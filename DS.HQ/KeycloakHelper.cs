using DS.HQ.Controllers;
using Microsoft.Extensions.Options;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Auth;
using NETCore.Keycloak.Client.Models.Users;
using Newtonsoft.Json.Linq;

namespace DS.HQ
{
    public class KeycloakHelper
    {
        private readonly IOptions<DSSettings> options;

        private KeycloakClient client;

        public KeycloakHelper(IOptions<DSSettings> options)
        {
            this.options = options;

            client = new KeycloakClient(options.Value.SSO_URL);
        }

        public KeycloakClient GetClient()
        {
            return client;
        }

        public async Task<string> GetToken()
        {
            return (await client.Auth.GetClientCredentialsTokenAsync(options.Value.Realm, new KcClientCredentials
            {
                ClientId = options.Value.ClientID,
                Secret = options.Value.ClientSecret
            })).Response.AccessToken;
        }

        public async Task<List<DSUser>> GetUsers()
        {
            var token = await GetToken();

            var result = await client.Users.ListUserAsync(options.Value.Realm, token, new KcUserFilter { Max = 500 });

            var retval = new List<DSUser>();

            foreach (var obj in result.Response)
            {
                var usr = new DSUser()
                {
                    User = obj,
                    Roles = (await client.Users.UserGroupsAsync(options.Value.Realm, token, obj.Id)).Response.ToList()
                };

                if (obj.Attributes != null && obj.Attributes.TryGetValue("groupnumber", out var gtoken))
                {
                    usr.GroupNumber = ((JArray)gtoken).ToObject<List<string>>().FirstOrDefault();
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
                User = (await client.Users.GetAsync(options.Value.Realm, token, id)).Response,
                Roles = (await client.Users.UserGroupsAsync(options.Value.Realm, token, id)).Response.ToList()
            };

            if (usr.User.Attributes != null && usr.User.Attributes.TryGetValue("groupnumber", out var gtoken))
            {
                usr.GroupNumber = ((JArray)gtoken).ToObject<List<string>>().FirstOrDefault();
            }

            return usr;
        }
    }
}