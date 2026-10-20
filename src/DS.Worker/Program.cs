using System.Net;
using DS.Application.FileStorage;
using DS.Domain;
using DS.Infrastructure;
using DS.Worker;
using Hangfire;

var builder = Host.CreateApplicationBuilder(args);
var settings = builder.Configuration.GetSection("DS").Get<DSSettings>();
if (string.IsNullOrWhiteSpace(settings?.ConnectionString)) throw new InvalidOperationException("DS:ConnectionString er ikke konfigureret.");

builder.Services.Configure<DSSettings>(builder.Configuration.GetSection("DS"));
builder.Services.AddDsDataLayer(settings.ConnectionString);
builder.Services.ConfigureDsIdentityOptions();
builder.Services.AddTransient<IFileStorage, LocalFileStorage>();
builder.Services.AddDsHangfireStorage(settings.ConnectionString);

builder.Services.AddHostedService<MigrationReadiness>();
builder.Services.ConfigureJobs();
builder.Services.AddHostedService<RecurringJobRegistrar>();
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = Dns.GetHostName();
    options.WorkerCount = 5;
});

await builder.Build().RunAsync();
