using DS.Domain.Data;
using DS;
using DS.Website;
using DS.Website.Services;
using Microsoft.AspNetCore.HttpOverrides;
using DbUp;
using Hangfire;
using DS.Website.Extensions;
using DS.Domain;
using DS.Application.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDS();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

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

var dssettings = app.Configuration.GetSection("DS").Get<DSSettings>();

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
    await scope.ServiceProvider.GetRequiredService<RoleSeeder>().Seed();
}

app.Run();
