using DS.Application.FileStorage;
using DS.Application.Jobs;
using DS.Application.Seeders;
using DS.Application.Services;
using DS.Website.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ConfigureApplication
{
    public static IServiceCollection ConfigureDsApplication(this IServiceCollection services)
    {
        services.AddTransient<ActivityService>();
        services.AddTransient<ActivityTeamService>();
        services.AddTransient<RoleSeeder>();
        services.AddTransient<IFileStorage, LocalFileStorage>();

        services.ConfigureJobs();

        return services;
    }

    public static IServiceCollection ConfigureJobs(this IServiceCollection services)
    {
        services.AddTransient<MailJobs>();
        services.AddTransient<OpenIddictJobs>();
        services.AddTransient<FileProcessingJobs>();

        services.AddHostedService<RecurringJobRegistrar>();

        return services;
    }
}