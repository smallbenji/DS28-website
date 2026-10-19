using System.Net;
using DS.Application.Data;
using DS.Application.FileStorage;
using DS.Domain;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DS.Worker;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var settings = builder.Configuration.GetSection("DS").Get<DSSettings>();
if (string.IsNullOrWhiteSpace(settings?.ConnectionString)) throw new InvalidOperationException("DS:ConnectionString er ikke konfigureret.");

builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));
builder.Services.Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
builder.Services.AddDbContext<DataDbContext>(options => options
    .UseNpgsql(settings.ConnectionString)
    .UseSnakeCaseNamingConvention()
    .UseOpenIddict());
builder.Services.AddScoped<IDataDbContext>(provider => provider.GetRequiredService<DataDbContext>());
builder.Services.AddTransient<IFileStorage, LocalFileStorage>();
builder.Services.AddOpenIddict().AddCore(options => options.UseEntityFrameworkCore().UseDbContext<DataDbContext>());
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(
    storage => storage.UseNpgsqlConnection(settings.ConnectionString),
    new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        PrepareSchemaIfNecessary = false
    }));

builder.Services.AddHostedService<MigrationReadiness>();
builder.Services.ConfigureJobs();
builder.Services.AddHostedService<RecurringJobRegistrar>();
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = Dns.GetHostName();
    options.WorkerCount = 5;
});

await builder.Build().RunAsync();
