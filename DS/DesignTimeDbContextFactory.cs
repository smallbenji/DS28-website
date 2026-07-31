using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DS;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DataDbContext>
{
    public DataDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DS__ConnectionString")
            ?? "Host=localhost;Database=postgres;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<DataDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DataDbContext(optionsBuilder.Options);
    }
}
