using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DS;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DataDbContext>
{
    public DataDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DS__ConnectionString")
            ?? "Host=localhost;Database=postgres;Username=postgres;Password=postgres";

        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        var serviceProvider = services.BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<DataDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseOpenIddict();
        optionsBuilder.UseApplicationServiceProvider(serviceProvider);

        return new DataDbContext(optionsBuilder.Options);
    }
}
