using DS;
using DS.Aktibasen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDSAuth(builder.Configuration);

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<IKeycloakActivityHelper, KeycloakActivityHelper>();
builder.Services.AddScoped<TeamPermissions>();

var app = builder.Build();

app.AddDSEndpoints();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();
