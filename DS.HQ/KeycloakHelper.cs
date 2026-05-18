using DS.HQ.Controllers;
using Microsoft.Extensions.Options;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Auth;
using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;
using Newtonsoft.Json.Linq;

namespace DS.HQ
{
    public class KeycloakHelper
    {
        private readonly IOptions<DSSettings> options;
        private readonly IOptions<HQSettings> hQOptions;
        private readonly DataDbContext dataDB;
        private KeycloakClient client;
        private string realm;

        public KeycloakHelper(IOptions<DSSettings> options, IOptions<HQSettings> HQOptions, DataDbContext DataDB)
        {
            this.options = options;
            hQOptions = HQOptions;
            dataDB = DataDB;
            realm = options.Value.Realm;

            client = new KeycloakClient(options.Value.SSO_URL);
        }

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

            var response = await client.Users.CreateAsync(realm, token, data.User);

            await RefreshUsers();
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
                    await httpClient.GetAsync(site+"/refresh-users");
                }
            }
        }
    }
}