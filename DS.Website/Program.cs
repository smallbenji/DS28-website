using System.Text.Json.Serialization;
using DS;
using DS.Website;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;

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

builder.Services.AddDbContext<DataDbContext>(options =>
{
    var dssettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

    options.UseNpgsql(dssettings?.ConnectionString ?? "");

    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<User, Role>(options => {
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = true;
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

        // WordPress sender 'scope' med i token-anmodningen ved authorization code flow.
        // OpenIddict afviser dette med ID2074, da scopes allerede er bundet til
        // authorization koden. Vi fjerner derfor valideringen, så parameteren ignoreres.
        options.RemoveEventHandler(OpenIddictServerHandlers.Exchange.ValidateScopeParameter.Descriptor);
	options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Roles // (Hvis du også vil sende roller med over)
        );

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

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

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/AccessDenied";

    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    }
});

// Cacher brugerroller, så ClaimsTransformer ikke rammer databasen på hver request.
builder.Services.AddMemoryCache();

// Roller/approller hentes frisk fra databasen på hver request,
// så rolleændringer slår igennem uden at brugeren skal logge ind igen.
builder.Services.AddScoped<IClaimsTransformation, ClaimsTransformer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<DataDbContext>();
        context.Database.Migrate();

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
        logger.LogError(ex, "Der opstod en fejl under migrering eller seeding af databasen.");
    }

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    if (await manager.FindByClientIdAsync("wordpress-app1") is null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "wordpress-app1",
            ClientSecret = "isoisoisoisoisoisoisoiso",
            DisplayName = "WordPress Hjemmeside",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles
            },
            RedirectUris = { new Uri("https://www.distriktssommerlejr.dk/wp-admin/admin-ajax.php?action=openid-connect-authorize") }
        });
    }
}

app.Run();
