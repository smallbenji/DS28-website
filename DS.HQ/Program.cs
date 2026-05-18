using DS;
using DS.HQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<HQSettings>(builder.Configuration.GetSection("HQ"));

builder.Services.AddDSAuth(builder.Configuration);

builder.Services.AddControllersWithViews(options => {
    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddScoped<KeycloakHelper>();

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

// app.MapGet("/logout", async context =>
// {
//     await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
// });

app.AddDSEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try {
        var context = services.GetRequiredService<DataDbContext>();
        context.Database.Migrate();
    } catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
