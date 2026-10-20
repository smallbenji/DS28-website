using Microsoft.AspNetCore.Hosting;
using DS.Application.Services.Users;
using DS.Application.Services.Organization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using DbUp;
using DS.Application.Data;
using DS.Application.Services.Accounts;
using DS.Application.Services.Email;
using DS.Application.Services.OpenIddict;
using DS.Application.Services;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DS.Infrastructure.Identity;
using DS.Infrastructure.OpenIddict;
using DS.Website.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenIddict.Abstractions;
using System.Text.Json;

var connectionString = Environment.GetEnvironmentVariable("ACCOUNT_CLIENT_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_account_client_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_account_client_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var migration = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(migration.Successful, $"migrations: {migration.Error?.Message}");

var services = new ServiceCollection();
services.AddLogging();
services.AddDataProtection();
services.AddDsDataLayer(connectionString);
services.AddDsIdentityCore().AddUserManager<NotificationUserManager>().AddDefaultTokenProviders();
services.Configure<DSSettings>(options =>
{
    options.NotificationsEnabled = true;
    options.PublicBaseUrl = "https://ds28.example.test";
});
services.AddSingleton<RecordingMailQueue>();
services.AddSingleton<IMailQueue>(provider => provider.GetRequiredService<RecordingMailQueue>());
services.AddScoped<EmailService>();
services.AddScoped<AccountService>();
services.AddScoped<UserService>();
services.AddSingleton<IUserAccessCache, TestUserAccessCache>();
services.AddScoped<IOpenIddictClientService, OpenIddictClientService>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var mail = provider.GetRequiredService<RecordingMailQueue>();
Check(manager is NotificationUserManager, "Identity resolves Infrastructure notification manager");
var observer = new User { UserName = "observer@example.test", Email = "observer@example.test" };
Check((await manager.CreateAsync(observer)).Succeeded, "create notification subscriber");
db.NotificationPreferences.Add(new UserNotificationPreference { UserId = observer.Id, NotificationType = NotificationType.NewUser });
await db.SaveChangesAsync();
var registration = await accounts.Register(new RegisterDto { FirstName = "Test", LastName = "User", Email = "new@example.test", Password = "test" });
Check(registration.Succeeded, "public registration creates user through Identity");
Check(mail.Messages.Count == 1 && mail.Messages[0].Email == observer.Email, "registration queues new-user notification through moved manager");
var user = await manager.FindByEmailAsync("new@example.test");
Check(user.EmailConfirmed && await manager.CheckPasswordAsync(user, "test"), "registration preserves confirmation and password");
await accounts.ForgotPassword("unknown@example.test");
Check(mail.Messages.Count == 1, "unknown email queues no reset mail");
await accounts.ForgotPassword(user.Email);
Check(mail.Messages.Count == 2 && mail.Messages[1].Email == user.Email && mail.Messages[1].Body.Contains("https://ds28.example.test/reset-password/"), "known email queues reset link");
var resetController = new PasswordResetApiController(accounts);
Check(await resetController.ResetPassword(new ResetPasswordDto { UserId = "missing", Token = "invalid", NewPassword = "next" }) is BadRequestObjectResult { Value: "Brugeren findes ikke." }, "missing user retains reset error");
Check((await accounts.ResetPassword(new ResetPasswordDto { UserId = user.Id, Token = "invalid", NewPassword = "next" })).Result.Succeeded == false, "invalid reset token is rejected");
var token = await manager.GeneratePasswordResetTokenAsync(user);
Check((await accounts.ResetPassword(new ResetPasswordDto { UserId = user.Id, Token = token, NewPassword = "next" })).Result.Succeeded && await manager.CheckPasswordAsync(user, "next"), "valid token resets password");

var webBuilder = WebApplication.CreateBuilder();
webBuilder.Logging.ClearProviders();
webBuilder.WebHost.UseUrls("http://127.0.0.1:0");
webBuilder.Services.AddAuthentication().AddCookie();
webBuilder.Services.AddAuthorization();
webBuilder.Services.AddAntiforgery();
webBuilder.Services.AddControllersWithViews().AddApplicationPart(typeof(UserApiController).Assembly);
webBuilder.Services.AddSingleton(scope.ServiceProvider.GetRequiredService<UserService>());
webBuilder.Services.AddSingleton(manager);
await using (var web = webBuilder.Build())
{
    web.UseRouting();
    web.UseAuthentication();
    web.Use(async (context, next) =>
    {
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, observer.Id),
            new Claim(ClaimTypes.Role, nameof(AppRoles.UsersView)),
            new Claim(ClaimTypes.Role, nameof(AppRoles.UsersResetPassword))
        }, "test"));
        await next(context);
    });
    web.UseAuthorization();
    web.MapControllers();
    await web.StartAsync();

    using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(web.Urls.Single()) };
    var resetPath = $"/api/v1/user/{user.Id}/reset-password-link";
    using var withoutToken = await client.PostAsync(resetPath, null);
    Check(withoutToken.StatusCode == HttpStatusCode.BadRequest, "reset-link endpoint rejects missing CSRF token");
    using var tokenResponse = await client.GetAsync("/api/v1/user/reset-password-link/antiforgery");
    Check(tokenResponse.IsSuccessStatusCode, "authorized user can obtain CSRF token");
    using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
    var csrfToken = tokenJson.RootElement.GetProperty("token").GetString();
    using var invalidRequest = new HttpRequestMessage(HttpMethod.Post, resetPath);
    invalidRequest.Headers.Add("RequestVerificationToken", "invalid");
    using var invalidResponse = await client.SendAsync(invalidRequest);
    Check(invalidResponse.StatusCode == HttpStatusCode.BadRequest, "reset-link endpoint rejects invalid CSRF token");
    using var validRequest = new HttpRequestMessage(HttpMethod.Post, resetPath);
    validRequest.Headers.Add("RequestVerificationToken", csrfToken);
    using var validResponse = await client.SendAsync(validRequest);
    Check(validResponse.IsSuccessStatusCode, "reset-link endpoint accepts matching CSRF token and cookie");
    using var linkJson = JsonDocument.Parse(await validResponse.Content.ReadAsStringAsync());
    Check(linkJson.RootElement.GetProperty("link").GetString().Contains($"/reset-password/{user.Id}?token="), "protected endpoint still returns reset link");
    await web.StopAsync();
}

var clients = scope.ServiceProvider.GetRequiredService<IOpenIddictClientService>();
var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
var data = new OpenIddictClientUpdateDto
{
    ClientId = "test-client",
    DisplayName = "Test client",
    ClientType = "confidential",
    RedirectUris = ["https://client.example.test/callback"],
    Scopes = ["openid", "profile"],
    RequiredRoles = [nameof(AppRoles.UsersView)],
    RequirePkce = true
};
var invalidData = new OpenIddictClientUpdateDto();
Check((await clients.CreateClient(invalidData, default)).Outcome == ServiceOutcome.Invalid, "invalid client input rejected");
var created = await clients.CreateClient(data, default);
var saved = created.Value;
var application = await applications.FindByIdAsync(saved.Client.Id);
Check(created.Outcome == ServiceOutcome.Success && !string.IsNullOrEmpty(saved.ClientSecret) && await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "confidential client receives valid secret");
Check((await clients.CreateClient(data, default)).Outcome == ServiceOutcome.Conflict, "duplicate client ID rejected");
var descriptor = new OpenIddictApplicationDescriptor();
await applications.PopulateAsync(descriptor, application);
descriptor.Properties["custom"] = JsonSerializer.SerializeToElement("keep");
descriptor.Permissions.Add("scp:custom");
descriptor.PostLogoutRedirectUris.Add(new Uri("https://client.example.test/logout"));
await applications.UpdateAsync(application, descriptor);
data.DisplayName = "Updated client";
var updated = (await clients.UpdateClient(saved.Client.Id, data, default)).Value;
var preserved = new OpenIddictApplicationDescriptor();
await applications.PopulateAsync(preserved, application);
Check(updated.ClientSecret == null && await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "editing preserves secret without returning it");
Check(preserved.Properties["custom"].GetString() == "keep" && preserved.Permissions.Contains("scp:custom") && preserved.PostLogoutRedirectUris.Count == 1, "editing preserves unrelated descriptor settings");
var rotatedSecret = (await clients.RotateSecret(saved.Client.Id, default)).Value.ClientSecret;
Check(await applications.ValidateClientSecretAsync(application, rotatedSecret) && !await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "rotation replaces old secret");
data.ClientType = "public";
Check((await clients.UpdateClient(saved.Client.Id, data, default)).Outcome == ServiceOutcome.Success && (await clients.RotateSecret(saved.Client.Id, default)).Outcome == ServiceOutcome.Invalid, "public clients cannot rotate secrets");
data.RequirePkce = false;
Check((await clients.UpdateClient(saved.Client.Id, data, default)).Outcome == ServiceOutcome.Invalid, "public clients still require PKCE");
Check((await clients.DeleteClient(saved.Client.Id, default)).Outcome == ServiceOutcome.Success && await applications.FindByClientIdAsync(data.ClientId) == null, "delete removes client");
Check((await clients.RotateSecret("missing", default)).Outcome == ServiceOutcome.NotFound, "missing client retains not-found result");
Console.WriteLine("ALL ACCOUNT AND CLIENT CHECKS PASSED");

class RecordingMailQueue : IMailQueue
{
    public List<(string Email, string Body)> Messages { get; } = new();

    public void Enqueue(string toEmail, string toName, string subject, string body)
    {
        Messages.Add((toEmail, body));
    }
}

class TestUserAccessCache : IUserAccessCache
{
    public void Evict(string userId)
    {
    }
}
