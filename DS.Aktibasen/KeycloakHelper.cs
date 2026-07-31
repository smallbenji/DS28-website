using Microsoft.Extensions.Options;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Auth;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.Aktibasen;

public interface IKeycloakActivityHelper
{
    Task<List<KcUser>> GetUsers();
}

public class KeycloakActivityHelper(IOptions<DSSettings> options) : IKeycloakActivityHelper
{
    private readonly KeycloakClient client = new(options.Value.SSO_URL);
    private readonly string realm = options.Value.Realm;

    public async Task<List<KcUser>> GetUsers()
    {
        var token = (await client.Auth.GetClientCredentialsTokenAsync(realm, new KcClientCredentials
        {
            ClientId = options.Value.ClientID,
            Secret = options.Value.ClientSecret
        })).Response.AccessToken;

        return (await client.Users.ListUserAsync(realm, token, new KcUserFilter { Max = 500 })).Response.ToList();
    }
}
