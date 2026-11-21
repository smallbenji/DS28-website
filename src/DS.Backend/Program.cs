using DS.Domain.Models;
using DS.Infrastructure.Data;
using DS;
using DS.Backend;
using DS.Backend.Services;
using Microsoft.AspNetCore.HttpOverrides;
using DbUp;
using Hangfire;
using DS.Backend.Extensions;
using DS.Backend.Mcp;
using DS.Domain;
using DS.Application.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDS();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<BoardTools>();

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

app.MapHub<BoardHub>("/api/v1/boards/hub");

app.MapMcp("/mcp").RequireAuthorization(McpAuthenticationHandler.PolicyName);

app.MapGet(McpAuthenticationHandler.MetadataPath, (HttpRequest request) =>
{
    var origin = $"{request.Scheme}://{request.Host}";

    return Results.Json(new
    {
        resource = $"{origin}/mcp",
        authorization_servers = new[] { $"{origin}/" },
        scopes_supported = new[] { "openid", "profile", "email", "roles", "offline_access" },
        bearer_methods_supported = new[] { "header" },
    });
});

var env = app.Services.GetRequiredService<IWebHostEnvironment>();

app.MapGet("/beta/{**path}", (string path, HttpRequest request) =>
    Results.Redirect($"/{path}{request.QueryString}", permanent: true));

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
    .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
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
