using DS.Domain;
using DS.Domain.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DS.Worker
{
    public class MigrationReadiness(IOptions<DSSettings> settings, ILogger<MigrationReadiness> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var expectedScripts = typeof(Group).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("DS.Migrations.", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
            if (expectedScripts.Count == 0) throw new InvalidOperationException("Ingen databasemigrationer blev fundet.");

            while (true)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(settings.Value.ConnectionString);
                    await connection.OpenAsync(cancellationToken);
                    await using var command = new NpgsqlCommand("SELECT scriptname FROM ds28.schemaversions", connection);
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    var appliedScripts = new HashSet<string>(StringComparer.Ordinal);
                    while (await reader.ReadAsync(cancellationToken)) appliedScripts.Add(reader.GetString(0));

                    if (expectedScripts.IsSubsetOf(appliedScripts))
                    {
                        logger.LogInformation("Databasemigrationerne er klar; Hangfire-worker starter.");
                        return;
                    }

                    logger.LogInformation("Venter på at Website gennemfører databasemigrationerne.");
                }
                catch (NpgsqlException ex)
                {
                    logger.LogWarning("Venter på databasen og Websites migrationer: {Message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
