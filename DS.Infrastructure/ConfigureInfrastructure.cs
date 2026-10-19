using DS.Application.Services.Email;
using DS.Application.Services.Files;
using DS.Application.Services.OpenIddict;
using DS.Infrastructure.OpenIddict;
using DS.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace DS.Infrastructure;

public static class ConfigureInfrastructure {
    public static IServiceCollection ConfigureDsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOpenIddictClientService, OpenIddictClientService>();
        services.ConfigureJobs();

        return services;
    }

    public static IServiceCollection ConfigureJobs(this IServiceCollection services)
    {
        services.AddTransient<IMailQueue, HangfireMailQueue>();
        services.AddTransient<IFileProcessingQueue, HangfireFileProcessingQueue>();
        services.AddTransient<MailJobs>();
        services.AddTransient<OpenIddictJobs>();
        services.AddTransient<FileProcessingJobs>();

        return services;
    }
}
