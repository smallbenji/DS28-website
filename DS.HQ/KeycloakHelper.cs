using DS.HQ.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Auth;
using NETCore.Keycloak.Client.Models.Common;
using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;
using Newtonsoft.Json.Linq;

namespace DS.HQ
{
    public interface IKeycloakHelper
    {
        Task AddUserToGroup(string userId, string groupId);
        Task CreateUser(DSUser data);
        Task DeleteUser(string id);
        KeycloakClient GetClient();
        Task<List<KcGroup>> GetGroups();
        Task<string> GetToken();
        Task<DSUser> GetUser(string id);
        Task<List<DSUser>> GetUsers();
        Task RefreshUsers();
        Task RemoveUserFromGroup(string userId, string groupId);
        Task ResetUserPassword(string userId, string newPassword);
        Task UpdateUser(DSUser user);
    }

    public class KeycloakHelper(IOptions<DSSettings> options, IOptions<HQSettings> hQOptions, DataDbContext dataDB) : IKeycloakHelper
    {
        private KeycloakClient client = new KeycloakClient(options.Value.SSO_URL);
        private string realm = options.Value.Realm;

        public KeycloakClient GetClient()
        {
            return client;
        }

        public async Task<string> GetToken()
        {
            return (await client.Auth.GetClientCredentialsTokenAsync(realm, new KcClientCredentials
            {
                ClientId = options.Value.ClientID,
                Secret = options.Value.ClientSecret
            })).Response.AccessToken;
        }

        public async Task<List<DSUser>> GetUsers()
        {
            var token = await GetToken();

            var result = await client.Users.ListUserAsync(realm, token, new KcUserFilter { Max = 500 });

            var retval = new List<DSUser>();

            foreach (var obj in result.Response)
            {
                var usr = new DSUser()
                {
                    User = obj,
                    Roles = (await client.Users.UserGroupsAsync(realm, token, obj.Id)).Response.ToList()
                };

                if (obj.Attributes != null && obj.Attributes.TryGetValue("groupnumber", out var gtoken))
                {
                    usr.GroupNumber = ((JArray)gtoken).ToObject<List<string>>().FirstOrDefault();

                    usr.Group = dataDB.Groups.FirstOrDefault(x => x.Id.ToString().Equals(usr.GroupNumber));
                }

                retval.Add(usr);
            }

            return retval;
        }

        public async Task<DSUser> GetUser(string id)
        {
            var token = await GetToken();

            var usr = new DSUser()
            {
                User = (await client.Users.GetAsync(realm, token, id)).Response,
                Roles = (await client.Users.UserGroupsAsync(realm, token, id)).Response.ToList()
            };

            if (usr.User.Attributes != null && usr.User.Attributes.TryGetValue("groupnumber", out var gtoken))
            {
                usr.GroupNumber = ((JArray)gtoken).ToObject<List<string>>().FirstOrDefault();

                usr.Group = dataDB.Groups.FirstOrDefault(x => x.Id.Equals(usr.GroupNumber));
            }

            return usr;
        }

        public async Task CreateUser(DSUser data)
        {
            var token = await GetToken();

            data.User.EmailVerified = true;
            data.User.Enabled = true;
            data.User.CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await client.Users.CreateAsync(realm, token, data.User);

            await RefreshUsers();
        }

        public async Task ResetUserPassword(string userId, string newPassword)
        {
            var token = await GetToken();

            var credential = new KcCredentials
            {
                Type = "password",
                Value = newPassword,
                Temporary = false
            };

            await client.Users.ResetPasswordAsync(realm, token, userId, credential);
        }

        public async Task DeleteUser(string id)
        {
            var token = await GetToken();

            await client.Users.DeleteAsync(realm, token, id);

            await RefreshUsers();
        }

        public async Task UpdateUser(DSUser user)
        {
            var token = await GetToken();

            if (user.User.Attributes == null)
                user.User.Attributes = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(user.GroupNumber))
            {
                user.User.Attributes["groupnumber"] = new List<string> { user.GroupNumber };
            }
            else
            {
                user.User.Attributes.Remove("groupnumber");
            }

            await client.Users.UpdateAsync(realm, token, user.User.Id, user.User);


            await RefreshUsers();
        }

        public async Task<List<KcGroup>> GetGroups()
        {
            var token = await GetToken();

            return (await client.Groups.ListAsync(realm, token, new KcGroupFilter { Max = 500 })).Response.ToList();
        }

        public async Task AddUserToGroup(string userId, string groupId)
        {
            var token = await GetToken();

            await client.Users.AddToGroupAsync(realm, token, userId, groupId);

            await RefreshUsers();
        }

        public async Task RemoveUserFromGroup(string userId, string groupId)
        {
            var token = await GetToken();

            await client.Users.DeleteFromGroupAsync(realm, token, userId, groupId);

            await RefreshUsers();
        }

        public async Task RefreshUsers()
        {
            KeycloakValidation.LastUpdate = DateTime.UtcNow.Ticks;

            if (hQOptions.Value.UserRefreshUrl != null)
            {
                var httpClient = new HttpClient();
                foreach (var site in hQOptions.Value.UserRefreshUrl)
                {
                    await httpClient.GetAsync(site + "/refresh-users");
                }
            }
        }
    }

    public class CachedKeycloakHelper(IKeycloakHelper keycloakHelper, IMemoryCache cache) : IKeycloakHelper
    {
        public KeycloakClient GetClient()
        {
            return keycloakHelper.GetClient();
        }

        public Task<string> GetToken()
        {
            return keycloakHelper.GetToken();
        }

        public Task AddUserToGroup(string userId, string groupId)
        {
            cache.Remove("users_all");
            cache.Remove($"users_{userId}");
            return keycloakHelper.AddUserToGroup(userId, groupId);
        }

        public Task CreateUser(DSUser data)
        {
            cache.Remove("users_all");
            return keycloakHelper.CreateUser(data);
        }

        public Task DeleteUser(string id)
        {
            cache.Remove("users_all");
            cache.Remove($"users_{id}");
            return keycloakHelper.DeleteUser(id);
        }

        public Task<List<KcGroup>> GetGroups()
        {
            return cache.GetOrCreateAsync("groups_all", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return keycloakHelper.GetGroups();
            });
        }

        public Task<DSUser> GetUser(string id)
        {
            return cache.GetOrCreateAsync($"users_{id}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return keycloakHelper.GetUser(id);
            });
        }

        public Task<List<DSUser>> GetUsers()
        {
            return cache.GetOrCreateAsync("users_all", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return keycloakHelper.GetUsers();
            });
        }

        public Task RemoveUserFromGroup(string userId, string groupId)
        {
            cache.Remove("users_all");
            cache.Remove($"users_{userId}");
            return keycloakHelper.RemoveUserFromGroup(userId, groupId);
        }

        public Task ResetUserPassword(string userId, string newPassword)
        {
            return keycloakHelper.ResetUserPassword(userId, newPassword);
        }

        public Task UpdateUser(DSUser user)
        {
            cache.Remove("users_all");
            cache.Remove($"users_{user.User.Id}");
            return keycloakHelper.UpdateUser(user);
        }

        public Task RefreshUsers()
        {
            return keycloakHelper.RefreshUsers();
        }
    }
}