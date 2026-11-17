using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using DS.Application.Services.Email;
using DS.Application.Services.OpenIddict;
using DS.Backend;
using DS.Backend.Controllers;
using DS.Backend.Extensions;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.OpenIddict;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

static class OAuthRefreshChecks
{
    public static async Task Run(string connectionString, Action<bool, string> check)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDsDataLayer(connectionString);
        builder.Services.AddDSAuth(builder);
        builder.Services.AddOpenIddict().AddServer(options =>
        {
            options.Configure(configuration =>
            {
                configuration.SigningCredentials.Clear();
                configuration.EncryptionCredentials.Clear();
            });
            options.AddEphemeralSigningKey();
            options.AddEphemeralEncryptionKey();
        });
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<DS.Application.Services.AccessTokens.AccessTokenService>();
        builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddSingleton<IMailQueue, RecordingMailQueue>();
        builder.Services.Configure<DSSettings>(options => options.NotificationsEnabled = false);
        builder.Services.AddScoped<IOpenIddictClientService, OpenIddictClientService>();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AuthorizationController).Assembly);
        builder.Services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);
        builder.Services.Configure<OpenIddictServerOptions>(options =>
        {
            options.AccessTokenLifetime = TimeSpan.FromSeconds(1);
            options.RefreshTokenReuseLeeway = TimeSpan.Zero;
        });
        await using var app = builder.Build();
        string userId;
        string secret;
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            await roles.CreateAsync(new Role { Name = nameof(AppGroups.SysAdmin) });
            var user = new User { UserName = "refresh@example.test", Email = "refresh@example.test", EmailConfirmed = true };
            check((await users.CreateAsync(user)).Succeeded, "create OAuth user");
            check((await users.AddToRoleAsync(user, nameof(AppGroups.SysAdmin))).Succeeded, "grant OAuth user access");
            userId = user.Id;
            var result = await scope.ServiceProvider.GetRequiredService<IOpenIddictClientService>().CreateClient(new OpenIddictClientUpdateDto
            {
                ClientId = "refresh-check", DisplayName = "Refresh check", ClientType = "confidential",
                RedirectUris = ["https://client.example.test/callback"], Scopes = ["openid", "profile", "email"],
                RequiredRoles = [nameof(AppRoles.UsersView)], RequirePkce = true
            }, default);
            check(result.Value != null, "create OAuth client");
            secret = result.Value.ClientSecret;
        }
        app.UseRouting();
        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/connect/authorize")
            {
                var users = context.RequestServices.GetRequiredService<UserManager<User>>();
                context.User = await context.RequestServices.GetRequiredService<SignInManager<User>>()
                    .CreateUserPrincipalAsync(await users.FindByIdAsync(userId));
            }
            await next(context);
        });
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
        async Task<JsonObject> Exchange(params (string Key, string Value)[] parameters)
        {
            using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(parameters
                .Concat(new[] { ("client_id", "refresh-check"), ("client_secret", secret) })
                .Select(p => new KeyValuePair<string, string>(p.Item1, p.Item2))));
            return JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsObject();
        }
        async Task<JsonObject> Login(string scopes = "openid profile email offline_access")
        {
            var verifier = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            using var response = await client.GetAsync("/connect/authorize?client_id=refresh-check&response_type=code&redirect_uri=https%3A%2F%2Fclient.example.test%2Fcallback&scope=" + Uri.EscapeDataString(scopes) + "&code_challenge_method=S256&code_challenge=" + challenge);
            check(response.StatusCode == HttpStatusCode.Redirect, "authorization code with PKCE issued");
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(response.Headers.Location.Query);
            return await Exchange(("grant_type", "authorization_code"), ("code", query["code"].ToString()), ("redirect_uri", "https://client.example.test/callback"), ("code_verifier", verifier));
        }
        var metadata = JsonNode.Parse(await client.GetStringAsync("/.well-known/openid-configuration"));
        check(metadata["grant_types_supported"].AsArray().Any(v => v.GetValue<string>() == "refresh_token"), "discovery advertises refresh grant");
        check(metadata["scopes_supported"].AsArray().Any(v => v.GetValue<string>() == "offline_access"), "discovery advertises offline access");
        var initial = await Login();
        check(initial["refresh_token"] != null && initial["expires_in"].GetValue<int>() is >= 0 and <= 1, "code exchange issues refresh and short-lived access token");
        await Task.Delay(1500);
        var refreshed = await Exchange(("grant_type", "refresh_token"), ("refresh_token", initial["refresh_token"].GetValue<string>()));
        check(refreshed["access_token"] != null && refreshed["refresh_token"] != null, "refresh after access expiry succeeds without scope");
        check(refreshed["scope"].GetValue<string>().Split(' ').Contains("profile"), "omitted scope preserves granted scopes");
        var twice = await Exchange(("grant_type", "refresh_token"), ("refresh_token", refreshed["refresh_token"].GetValue<string>()));
        check(twice["refresh_token"] != null && twice["refresh_token"].GetValue<string>() != refreshed["refresh_token"].GetValue<string>(), "successive refresh rotates token");
        var replay = await Exchange(("grant_type", "refresh_token"), ("refresh_token", initial["refresh_token"].GetValue<string>()));
        check(replay["error"]?.GetValue<string>() == "invalid_grant", "redeemed refresh token rejected");
        var restricted = await Login();
        var narrowed = await Exchange(("grant_type", "refresh_token"), ("refresh_token", restricted["refresh_token"].GetValue<string>()), ("scope", "openid offline_access"));
        check(narrowed["access_token"] != null, "refresh accepts narrower scopes");
        narrowed = await Exchange(("grant_type", "refresh_token"), ("refresh_token", narrowed["refresh_token"].GetValue<string>()));
        check(narrowed["scope"]?.GetValue<string>().Split(' ').ToHashSet().SetEquals(new[] { "openid", "offline_access" }) == true, "narrowed scopes persist on successive refresh");
        var expanded = await Exchange(("grant_type", "refresh_token"), ("refresh_token", narrowed["refresh_token"].GetValue<string>()), ("scope", "openid profile offline_access"));
        check(expanded["error"] != null, "refresh cannot restore removed scope");
        var noOffline = await Login("openid profile");
        check(noOffline["access_token"] != null && noOffline["refresh_token"] == null, "no refresh token without offline access");
        var invalid = await Exchange(("grant_type", "refresh_token"), ("refresh_token", "invalid"));
        check(invalid["error"] != null, "invalid refresh rejected");
        var revoked = await Login();
        using (var scope = app.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
            await foreach (var token in tokens.FindBySubjectAsync(userId)) await tokens.TryRevokeAsync(token);
        }
        var revokedResult = await Exchange(("grant_type", "refresh_token"), ("refresh_token", revoked["refresh_token"].GetValue<string>()));
        check(revokedResult["error"] != null, "revoked refresh rejected");
        var changedStamp = await Login();
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            await users.UpdateSecurityStampAsync(await users.FindByIdAsync(userId));
        }
        var stampResult = await Exchange(("grant_type", "refresh_token"), ("refresh_token", changedStamp["refresh_token"].GetValue<string>()));
        check(stampResult["error"] != null, "refresh rejected after security stamp change");
        var locked = await Login();
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByIdAsync(userId);
            await users.SetLockoutEnabledAsync(user, true);
            await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        }
        var lockResult = await Exchange(("grant_type", "refresh_token"), ("refresh_token", locked["refresh_token"].GetValue<string>()));
        check(lockResult["error"] != null, "locked user cannot refresh");
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            await users.SetLockoutEndDateAsync(await users.FindByIdAsync(userId), null);
        }
        var lostAccess = await Login();
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            await users.RemoveFromRoleAsync(await users.FindByIdAsync(userId), nameof(AppGroups.SysAdmin));
        }
        var denied = await Exchange(("grant_type", "refresh_token"), ("refresh_token", lostAccess["refresh_token"].GetValue<string>()));
        check(denied["error"] != null, "refresh denied after loss of client role");
        await app.StopAsync();
    }
}
