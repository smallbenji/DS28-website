using DS;
using DS.HQ;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        var dsSettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

        options.Authority = dsSettings.SSO_URL + "/realms/" + dsSettings.Realm;

        options.ClientId = dsSettings.ClientID;
        options.ClientSecret = dsSettings.ClientSecret;

        options.ResponseType = "code";

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.RequireHttpsMetadata = true;

        options.Scope.Add("groupnumber");
        // options.Scope.Add("groups");

        options.ClaimActions.MapJsonKey("groups", "groups");
        options.ClaimActions.MapUniqueJsonKey("group_number", "group_number");

        options.MapInboundClaims = true;

        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "groups";
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<KeycloakHelper>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.MapGet("/logout", async context =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});

app.Run();
