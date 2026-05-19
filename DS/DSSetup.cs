using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace DS
{
    public static class DSSetup
    {
        public static IServiceCollection AddDSAuth(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DSSettings>(configuration.GetSection("DS"));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    var dsSettings = configuration.GetSection("DS").Get<DSSettings>();

                    options.Events.OnValidatePrincipal = async context => await KeycloakValidation.KeycloakValidator(context, dsSettings);
                })
                .AddOpenIdConnect(options =>
                {
                    var dsSettings = configuration.GetSection("DS").Get<DSSettings>();

                    options.Authority = dsSettings.SSO_URL + "/realms/" + dsSettings.Realm;

                    options.ClientId = dsSettings.ClientID;
                    options.ClientSecret = dsSettings.ClientSecret;

                    options.ResponseType = "code";

                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.RequireHttpsMetadata = true;

                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("groupnumber");
                    // options.Scope.Add("groups");

                    options.ClaimActions.MapJsonKey("groups", "groups");
                    options.ClaimActions.MapUniqueJsonKey("group_number", "group_number");

                    options.MapInboundClaims = true;

                    options.TokenValidationParameters.NameClaimType = "name";
                    options.TokenValidationParameters.RoleClaimType = "groups";
                });

            services.AddAuthorization();

            services.AddDbContext<DataDbContext>(options =>
            {
                var dsSettings = configuration.GetSection("DS").Get<DSSettings>();

                options.UseNpgsql(dsSettings.ConnectionString);
            });

            return services;
        }
        public static WebApplication AddDSEndpoints(this WebApplication app)
        {
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            var env = app.Services.GetRequiredService<IWebHostEnvironment>();

            app.MapFallback(async context =>
            {
                var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

                if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    return;
                }

                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(filePath);
            });

            app.MapGet("/refresh-users", async context =>
            {
                KeycloakValidation.LastUpdate = DateTime.UtcNow.Ticks;
            });

            app.MapGet("/logout", async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            });

            return app;
        }
    }
}