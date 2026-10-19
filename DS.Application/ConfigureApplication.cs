using DS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ConfigureApplication
{
    public static IServiceCollection ConfigureDsApplication(this IServiceCollection service)
    {
        service.AddTransient<ActivityService>();
        service.AddTransient<ActivityTeamService>();

        return service;
    }
}