using DS.Application.Services;
using DS.Application.Services.AccessTokens;
using DS.Backend.Authentication;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DbUp;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;

var connectionString = Environment.GetEnvironmentVariable("ACCESS_TOKEN_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_access_token_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_access_token_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var migrations = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(migrations.Successful, $"migrations: {migrations.Error?.Message}");

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await new NpgsqlCommand("""
        INSERT INTO ds28.asp_net_users
            (id, user_name, email_confirmed, phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
        VALUES
            ('token-user', 'token-user', false, false, false, false, 0),
            ('other-user', 'other-user', false, false, false, false, 0),
            ('locked-user', 'locked-user', false, false, false, true, 0);
        UPDATE ds28.asp_net_users SET lockout_end = NOW() + INTERVAL '1 day' WHERE id = 'locked-user';
        """, connection).ExecuteNonQueryAsync();
}

var services = new ServiceCollection();
services.AddLogging();
services.AddDsDataLayer(connectionString);
services.AddDsIdentityCore();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var service = new AccessTokenService(db);

var created = await service.CreateToken("token-user", new AccessTokenCreateDto { Name = "MCP", ExpiresInDays = 30 });
Check(created.Outcome == ServiceOutcome.Success, "token kan oprettes");
var rawToken = created.Value.Token;
Check(rawToken.StartsWith("ds28_") && rawToken.Length > 40, "token har prefix og tilstrækkelig længde");

var stored = await db.AccessTokens.AsNoTracking().SingleAsync(token => token.Id == created.Value.Id);
Check(stored.TokenHash == AccessTokenService.Hash(rawToken), "kun hash gemmes");
Check(stored.TokenHash.Length == 64 && !stored.TokenHash.Contains(rawToken), "rå token findes ikke i databasen");
Check(stored.ExpiresAt > DateTime.UtcNow.AddDays(29), "udløb gemmes");
Check(stored.Name == "MCP", "navn gemmes");

var identity = await service.Authenticate(rawToken);
Check(identity?.UserId == "token-user" && identity.UserName == "token-user", "gyldig token giver brugeren");
Check(await service.Authenticate(rawToken + "x") == null, "forkert token afvises");
Check(await service.Authenticate("") == null, "tomt token afvises");
Check(await service.RevokeToken("other-user", created.Value.Id) is { Outcome: ServiceOutcome.NotFound }, "man kan ikke tilbagekalde andres token");
Check((await service.RevokeToken("token-user", created.Value.Id)).Outcome == ServiceOutcome.Success, "eget token kan tilbagekaldes");
Check(await service.Authenticate(rawToken) == null, "tilbagekaldt token afvises");
Check(await service.GetTokens("token-user") is { Outcome: ServiceOutcome.Success } list && list.Value.Count == 0, "tilbagekaldte tokens vises ikke");

var expired = await service.CreateToken("token-user", new AccessTokenCreateDto { Name = "Udløbet", ExpiresInDays = 1 });
await db.Database.ExecuteSqlAsync($"UPDATE ds28.user_access_token SET expires_at = NOW() - INTERVAL '1 minute' WHERE id = {expired.Value.Id}");
Check(await service.Authenticate(expired.Value.Token) == null, "udløbet token afvises");

var locked = await service.CreateToken("locked-user", new AccessTokenCreateDto { Name = "Låst" });
Check(await service.Authenticate(locked.Value.Token) == null, "token for låst bruger afvises");

var valid = await service.CreateToken("token-user", new AccessTokenCreateDto { Name = "Aktiv" });
Check((await service.CreateToken("token-user", new AccessTokenCreateDto { Name = " " })).Outcome == ServiceOutcome.Invalid, "tomt navn afvises");
Check((await service.CreateToken("token-user", new AccessTokenCreateDto { Name = new string('a', 65) })).Outcome == ServiceOutcome.Invalid, "for langt navn afvises");
Check((await service.CreateToken("token-user", new AccessTokenCreateDto { Name = "x", ExpiresInDays = 0 })).Outcome == ServiceOutcome.Invalid, "ugyldigt udløb afvises");

var handler = CreateHandler(service, $"Bearer {valid.Value.Token}");
var handled = await handler.AuthenticateAsync();
Check(handled.Succeeded, "handlers accepterer gyldigt token");
Check(handled.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) == "token-user", "handler sætter bruger-id");
Check(handled.Principal?.Identity?.AuthenticationType == AccessTokenAuthenticationHandler.SchemeName, "handler markerer token-auth");
Check((await CreateHandler(service, $"Bearer {rawToken}").AuthenticateAsync()).Failure != null, "handler afviser tilbagekaldt token");
Check((await CreateHandler(service, null).AuthenticateAsync()).None, "handler uden header giver intet resultat");

var antiforgery = new RecordingAntiforgery();
var filter = new CookieAntiforgeryFilter(antiforgery);
var cookie = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "token-user")], IdentityConstants.ApplicationScheme);
var bearer = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "token-user")], AccessTokenAuthenticationHandler.SchemeName);

var safe = await RunFilter("GET", cookie, nameof(DummyController.Save));
Check(safe.Result == null && antiforgery.Validations == 0, "GET kræver ikke token");
var cookiePost = await RunFilter("POST", cookie, nameof(DummyController.Save));
Check(cookiePost.Result is AntiforgeryValidationFailedResult && antiforgery.Validations == 1, "cookie-POST uden token afvises");
var cookieValid = await RunFilter("POST", cookie, nameof(DummyController.Save), reject: false);
Check(cookieValid.Result == null && antiforgery.Validations == 1, "cookie-POST med token valideres");
var bearerPost = await RunFilter("POST", bearer, nameof(DummyController.Save));
Check(bearerPost.Result == null && antiforgery.Validations == 0, "bearer-POST springer antiforgery over");
var ignored = await RunFilter("POST", cookie, nameof(DummyController.Public));
Check(ignored.Result == null && antiforgery.Validations == 0, "IgnoreAntiforgeryToken respekteres");

Console.WriteLine("OK");

async Task<AuthorizationFilterContext> RunFilter(string method, ClaimsIdentity identity, string actionName, bool reject = true)
{
    antiforgery.Validations = 0;
    antiforgery.Reject = reject;
    var httpContext = new DefaultHttpContext
    {
        Request = { Method = method },
        User = new ClaimsPrincipal(identity),
    };
    var descriptor = new ControllerActionDescriptor
    {
        MethodInfo = typeof(DummyController).GetMethod(actionName)!,
        ControllerTypeInfo = typeof(DummyController).GetTypeInfo(),
    };
    var context = new AuthorizationFilterContext(
        new ActionContext(httpContext, new RouteData(), descriptor, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()),
        []);
    await filter.OnAuthorizationAsync(context);
    return context;
}

static AccessTokenAuthenticationHandler CreateHandler(AccessTokenService service, string authorization)
{
    var httpContext = new DefaultHttpContext();
    if (authorization != null) httpContext.Request.Headers.Authorization = authorization;

    var handler = new AccessTokenAuthenticationHandler(
        new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
        NullLoggerFactory.Instance,
        UrlEncoder.Default,
        service);

    handler.InitializeAsync(new AuthenticationScheme(
        AccessTokenAuthenticationHandler.SchemeName,
        null,
        typeof(AccessTokenAuthenticationHandler)), httpContext);

    return handler;
}

sealed class DummyController
{
    public void Save() { }

    [IgnoreAntiforgeryToken]
    public void Public() { }
}

sealed class RecordingAntiforgery : IAntiforgery
{
    public int Validations { get; set; }
    public bool Reject { get; set; }

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => throw new NotSupportedException();
    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => throw new NotSupportedException();
    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(!Reject);
    public void SetCookieTokenAndHeader(HttpContext httpContext) { }

    public Task ValidateRequestAsync(HttpContext httpContext)
    {
        Validations++;
        return Reject
            ? Task.FromException(new AntiforgeryValidationException("ugyldigt token"))
            : Task.CompletedTask;
    }
}

sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;
    public T Get(string name) => value;
    public IDisposable OnChange(Action<T, string> listener) => null;
}
