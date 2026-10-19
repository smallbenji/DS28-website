using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DS.Domain;
using DS.Domain.Data;
using DS.Domain.Models;
using DS.Website.Exports;
using DS.Website.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace DS.Website.Extensions;

public static class ServiceExtensions
{
    public static WebApplicationBuilder ConfigureDS(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseStaticWebAssets();
        builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));

        builder.AddMVC();

        builder.AddProtectionKeys();

        var dssettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

        builder.Services.AddDbContext<DataDbContext>(options =>
        {
            options.UseNpgsql(dssettings?.ConnectionString ?? "")
                .UseSnakeCaseNamingConvention();

            options.UseOpenIddict();
        });

        builder.Services.AddDSAuth(builder);

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        builder.Services.AddDSServices(builder);

        return builder;
    }

    public static IServiceCollection AddDSServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var dsSettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

        services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();
        services.AddTransient<RoleSeeder>();
        services.AddTransient<CampSettings>();
        services.AddTransient<EmailService>();
        services.AddTransient<MailJobs>();
        services.AddTransient<OpenIddictJobs>();
        services.AddTransient<FileProcessingJobs>();
        services.AddTransient<IFileStorage, LocalFileStorage>();
        services.AddTransient<IGoogleAuthService, GoogleAuthService>();
        services.AddTransient<IGoogleDriveService, GoogleDriveService>();
        services.AddHangfire(config => config.UsePostgreSqlStorage(
            storage => storage.UseNpgsqlConnection(dsSettings.ConnectionString),
            new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = false
            }));

        services.AddHangfireServer(options => options.WorkerCount = 5);
        services.AddHostedService<RecurringJobRegistrar>();

        services.AddExporters();

        return services;
    }

    public static IServiceCollection AddExporters(this IServiceCollection services)
    {
        services.AddScoped<DataExport, GroupPreSignupExport>();
        services.AddScoped<DataExport, ParticipantExport>();

        return services;
    }

    public static IServiceCollection AddDSAuth(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
            options.Tokens.AuthenticatorIssuer = "DS HQ";

            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        .AddEntityFrameworkStores<DataDbContext>()
        .AddUserManager<NotificationUserManager>()
        .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<DataDbContext>();
            })
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserInfoEndpointUris("/connect/userinfo");

                options.AllowAuthorizationCodeFlow();

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
                    options.AddDevelopmentSigningCertificate();
                    options.AddDevelopmentEncryptionCertificate();
                }

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/AccessDenied";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            if (builder.Environment.IsDevelopment()) options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        });

        services.AddScoped<IClaimsTransformation, ClaimsTransformer>();

        services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.AuthenticatorTimeout = TimeSpan.FromMinutes(2);

            var serverOrigin = builder.Configuration.GetValue<string>("ORIGIN", null);
            if (serverOrigin == null && builder.Environment.IsProduction())
            {
                throw new Exception("MISSING SERVER ORIGIN ENV");
            }

            options.ServerDomain = serverOrigin;
        });

        return services;
    }

    public static WebApplicationBuilder AddMVC(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.NumberHandling =
                    JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        return builder;
    }

    public static WebApplicationBuilder AddProtectionKeys(this WebApplicationBuilder builder)
    {
        var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
        var dataProtection = builder.Services.AddDataProtection()
            .SetApplicationName("ds28-website");

        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        }

        return builder;
    }
}
