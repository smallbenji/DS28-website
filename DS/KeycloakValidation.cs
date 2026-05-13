using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DS;

public class KeycloakValidation
{
    public static async Task KeycloakValidator(CookieValidatePrincipalContext context, DSSettings dsSettings)
    {
        var lastSync = context.Principal.FindFirst("last_group_sync")?.Value;
            if (string.IsNullOrEmpty(lastSync) || DateTime.UtcNow.Ticks - long.Parse(lastSync) > TimeSpan.FromMinutes(1).Ticks)
            {
                var accessToken = context.Properties.GetTokenValue("access_token");
                var expiresAtClaim = context.Properties.GetTokenValue("expires_at");
                var refreshToken = context.Properties.GetTokenValue("refresh_token");

                if (string.IsNullOrEmpty(expiresAtClaim) || string.IsNullOrEmpty(accessToken)) return;

                var expiresAt = DateTimeOffset.Parse(expiresAtClaim);

                // 1. If the token is expired (or about to expire in 30s), refresh it
                if (expiresAt < DateTimeOffset.UtcNow.AddSeconds(30))
                {
                    var tokenEndpoint = $"{dsSettings.SSO_URL}/realms/{dsSettings.Realm}/protocol/openid-connect/token";

                    using var client1 = new HttpClient();
                    var tokenResponse = await client1.PostAsync(tokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "client_id", dsSettings.ClientID },
                        { "client_secret", dsSettings.ClientSecret },
                        { "refresh_token", refreshToken }
                    }));

                    if (tokenResponse.IsSuccessStatusCode)
                    {
                        var json = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
                        accessToken = json.GetProperty("access_token").GetString();
                        var newRefreshToken = json.GetProperty("refresh_token").GetString();
                        var expiresIn = json.GetProperty("expires_in").GetInt32();

                        // Store the new tokens back into the authentication properties
                        context.Properties.UpdateTokenValue("access_token", accessToken);
                        context.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                        context.Properties.UpdateTokenValue("expires_at", DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o"));

                        context.ShouldRenew = true; // Tell the middleware to update the cookie
                    }
                    else
                    {
                        // If refresh fails (e.g. session revoked in Keycloak), force logout
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync();
                        return;
                    }
                }

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync($"{dsSettings.SSO_URL}/realms/{dsSettings.Realm}/protocol/openid-connect/userinfo");

                if (response.IsSuccessStatusCode)
                {
                    var userInfo = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var identity = (ClaimsIdentity)context.Principal.Identity;

                    var existingRoles = identity.FindAll("groups").ToList();
                    foreach (var claim in existingRoles) identity.RemoveClaim(claim);

                    if (userInfo.TryGetProperty("groups", out var groups))
                    {
                        foreach (var group in groups.EnumerateArray())
                        {
                            identity.AddClaim(new Claim("groups", group.GetString()));
                        }
                    }

                    var oldSyncClaim = identity.FindFirst("last_group_sync");
                    if (oldSyncClaim != null) identity.RemoveClaim(oldSyncClaim);
                    identity.AddClaim(new Claim("last_group_sync", DateTime.UtcNow.Ticks.ToString()));

                    context.ReplacePrincipal(context.Principal);
                    context.ShouldRenew = true;
                }
            }
    }
}