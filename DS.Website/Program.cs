using DS.Models;
using DS.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using DS;
using DS.Website;
using DS.Website.Exports;
using DS.Website.Repositories;
using DS.Website.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using DbUp;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));

builder.Services.AddControllersWithViews()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.NumberHandling =
            JsonNumberHandling.AllowReadingFromString |
            JsonNumberHandling.WriteAsString;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("ds28-website")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();

var dssettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

builder.Services.AddDbContext<DataDbContext>(options =>
{

    options.UseNpgsql(dssettings?.ConnectionString ?? "")
        .UseSnakeCaseNamingConvention();

    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<User, Role>(options =>
    {
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = true;
        options.Tokens.AuthenticatorIssuer = "DS HQ";

        // Enable version 3 for passkey support
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<DataDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<DataDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetUserInfoEndpointUris("/connect/userinfo");

        options.AllowAuthorizationCodeFlow();
        // .RequireProofKeyForCodeExchange();

        options.RemoveEventHandler(OpenIddictServerHandlers.Exchange.ValidateScopeParameter.Descriptor);
	    options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Roles
        );

        string certPath = builder.Configuration["OpenIddict:CertificatePath"];
        string certPass = builder.Configuration["OpenIddict:CertificatePassword"];

        if (File.Exists(certPath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certPath,
                certPass,
                keyStorageFlags: X509KeyStorageFlags.MachineKeySet
            );

            options.AddSigningCertificate(certificate);
            options.AddEncryptionCertificate(certificate);
        }
        else
        {
            options.AddDevelopmentEncryptionCertificate();
            options.AddDevelopmentSigningCertificate();
        }

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    if (builder.Environment.IsDevelopment()) options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IClaimsTransformation, ClaimsTransformer>();

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.AuthenticatorTimeout = TimeSpan.FromMinutes(2);

    var serverOrigin = builder.Configuration.GetValue<string>("ORIGIN", null);
    if(serverOrigin == null && builder.Environment.IsProduction())
    {
        throw new Exception("MISSING SERVER ORIGIN ENV");
    }

    options.ServerDomain = serverOrigin;
});

builder.Services.AddTransient<ActivityRepository>();
builder.Services.AddTransient<CampSettings>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddScoped<DataExport, GroupPreSignupExport>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var env = app.Services.GetRequiredService<IWebHostEnvironment>();

app.MapFallback(async context =>
{
    var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(filePath);
}).RequireAuthorization();

var migrationResult = DeployChanges.To
    .PostgresqlDatabase(dssettings.ConnectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(typeof(DataDbContext).Assembly)
    .WithVariablesDisabled()
    .WithTransaction()
    .LogTo(new MigrationLog(app.Services.GetRequiredService<ILogger<MigrationLog>>()))
    .Build()
    .PerformUpgrade();

if (!migrationResult.Successful)
{
    throw new InvalidOperationException($"Databasemigreringen mislykkedes i {migrationResult.ErrorScript?.Name}: {migrationResult.Error.Message}", migrationResult.Error);
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var groupNames = Enum.GetNames<AppGroups>();

        foreach (var groupName in groupNames)
        {
            var groupExists = await roleManager.RoleExistsAsync(groupName);
            if (!groupExists)
            {
                var newRole = new Role { Name = groupName };
                var result = await roleManager.CreateAsync(newRole);

                if (result.Succeeded)
                {
                    logger.LogInformation("Seedede rollen/gruppen: {GroupName}", groupName);
                }
                else
                {
                    logger.LogError("Fejl under seeding af rollen {GroupName}: {Errors}",
                        groupName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Der opstod en fejl under seeding af databasen.");
    }
}

app.Run();
