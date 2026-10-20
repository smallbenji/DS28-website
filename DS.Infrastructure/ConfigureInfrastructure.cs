using DS.Application.Data;
using DS.Application.Services.Email;
using DS.Application.Services.Files;
using DS.Application.Services.OpenIddict;
using DS.Domain.Models;
using DS.Infrastructure.Data;
using DS.Infrastructure.Identity;
using DS.Infrastructure.Jobs;
using DS.Infrastructure.OpenIddict;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public static IServiceCollection AddDsDataLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DataDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .UseOpenIddict());

        services.AddScoped<IDataDbContext>(provider => provider.GetRequiredService<DataDbContext>());

        services.AddOpenIddict().AddCore(options => options
            .UseEntityFrameworkCore()
            .UseDbContext<DataDbContext>());

        return services;
    }

    public static IdentityBuilder AddDsIdentityCore(this IServiceCollection services)
    {
        services.ConfigureDsIdentityOptions();

        return services.AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<DataDbContext>();
    }

    public static IServiceCollection ConfigureDsIdentityOptions(this IServiceCollection services)
    {
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
            options.Tokens.AuthenticatorIssuer = "DS HQ";
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        });

        return services;
    }

    public static IServiceCollection AddDsHangfireStorage(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(
            storage => storage.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = false
            }));

        return services;
    }
}
