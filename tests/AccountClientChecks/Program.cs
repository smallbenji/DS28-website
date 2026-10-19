using DbUp;
using DS.Application.Data;
using DS.Application.Services.Accounts;
using DS.Application.Services.Email;
using DS.Application.Services.OpenIddict;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure.Data;
using DS.Infrastructure.Identity;
using DS.Infrastructure.OpenIddict;
using DS.Website.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
services.AddDbContext<DataDbContext>(options => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention().UseOpenIddict());
services.AddScoped<IDataDbContext>(provider => provider.GetRequiredService<DataDbContext>());
services.AddIdentityCore<User>(options =>
{
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
}).AddRoles<Role>().AddEntityFrameworkStores<DataDbContext>().AddUserManager<NotificationUserManager>().AddDefaultTokenProviders();
services.AddOpenIddict().AddCore(options => options.UseEntityFrameworkCore().UseDbContext<DataDbContext>());
services.Configure<DSSettings>(options =>
{
    options.NotificationsEnabled = true;
    options.PublicBaseUrl = "https://ds28.example.test";
});
services.AddSingleton<RecordingMailQueue>();
services.AddSingleton<IMailQueue>(provider => provider.GetRequiredService<RecordingMailQueue>());
services.AddScoped<EmailService>();
services.AddScoped<AccountService>();
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
Check((await clients.CreateClient(invalidData, default)).Outcome == OpenIddictClientOutcome.Invalid, "invalid client input rejected");
var created = await clients.CreateClient(data, default);
var saved = (OpenIddictClientSavedDto)created.Value;
var application = await applications.FindByIdAsync(saved.Client.Id);
Check(created.Outcome == OpenIddictClientOutcome.Success && !string.IsNullOrEmpty(saved.ClientSecret) && await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "confidential client receives valid secret");
Check((await clients.CreateClient(data, default)).Outcome == OpenIddictClientOutcome.Conflict, "duplicate client ID rejected");
var descriptor = new OpenIddictApplicationDescriptor();
await applications.PopulateAsync(descriptor, application);
descriptor.Properties["custom"] = JsonSerializer.SerializeToElement("keep");
descriptor.Permissions.Add("scp:custom");
descriptor.PostLogoutRedirectUris.Add(new Uri("https://client.example.test/logout"));
await applications.UpdateAsync(application, descriptor);
data.DisplayName = "Updated client";
var updated = (OpenIddictClientSavedDto)(await clients.UpdateClient(saved.Client.Id, data, default)).Value;
var preserved = new OpenIddictApplicationDescriptor();
await applications.PopulateAsync(preserved, application);
Check(updated.ClientSecret == null && await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "editing preserves secret without returning it");
Check(preserved.Properties["custom"].GetString() == "keep" && preserved.Permissions.Contains("scp:custom") && preserved.PostLogoutRedirectUris.Count == 1, "editing preserves unrelated descriptor settings");
var rotated = await clients.RotateSecret(saved.Client.Id, default);
var rotatedSecret = (string)rotated.Value.GetType().GetProperty("clientSecret").GetValue(rotated.Value);
Check(await applications.ValidateClientSecretAsync(application, rotatedSecret) && !await applications.ValidateClientSecretAsync(application, saved.ClientSecret), "rotation replaces old secret");
data.ClientType = "public";
Check((await clients.UpdateClient(saved.Client.Id, data, default)).Outcome == OpenIddictClientOutcome.Success && (await clients.RotateSecret(saved.Client.Id, default)).Outcome == OpenIddictClientOutcome.Invalid, "public clients cannot rotate secrets");
data.RequirePkce = false;
Check((await clients.UpdateClient(saved.Client.Id, data, default)).Outcome == OpenIddictClientOutcome.Invalid, "public clients still require PKCE");
Check((await clients.DeleteClient(saved.Client.Id, default)).Outcome == OpenIddictClientOutcome.Success && await applications.FindByClientIdAsync(data.ClientId) == null, "delete removes client");
Check((await clients.RotateSecret("missing", default)).Outcome == OpenIddictClientOutcome.NotFound, "missing client retains not-found result");
Console.WriteLine("ALL ACCOUNT AND CLIENT CHECKS PASSED");

class RecordingMailQueue : IMailQueue
{
    public List<(string Email, string Body)> Messages { get; } = new();

    public void Enqueue(string toEmail, string toName, string subject, string body)
    {
        Messages.Add((toEmail, body));
    }
}
