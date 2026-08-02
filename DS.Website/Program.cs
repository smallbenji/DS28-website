using DS;
using DS.Website;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();

builder.Services.AddDbContext<DataDbContext>(options =>
{
    var dssettings = builder.Configuration.GetSection("DS").Get<DSSettings>();

    options.UseNpgsql(dssettings?.ConnectionString ?? "");
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

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.LogoutPath = "/AccessDenied";
});

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

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
}

app.Run();
