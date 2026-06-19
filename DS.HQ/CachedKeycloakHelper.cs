using System.Runtime.CompilerServices;
using DS.HQ.Controllers;
using Microsoft.Extensions.Caching.Memory;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Groups;

namespace DS.HQ
{
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
            return UpdateUser(user);
        }

        public Task RefreshUsers()
        {
            return keycloakHelper.RefreshUsers();
        }
    }
}