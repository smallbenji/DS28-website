using System.Globalization;
using DS.Domain;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DS.Infrastructure.Jobs
{
    public class HistoricRegistrationSyncJob(IOptions<DSSettings> options, ILogger<HistoricRegistrationSyncJob> logger)
    {
        [AutomaticRetry(Attempts = 0)]
        public async Task Run()
        {
            var settings = options.Value;
            var historic = settings.HistoricRegistration;
            var arguments = new List<string>
            {
                "--firebase-credentials", historic.FirebaseCredentials,
                "--year", historic.Year.ToString(CultureInfo.InvariantCulture),
                "--apply"
            };

            if (!string.IsNullOrWhiteSpace(historic.GroupMap))
            {
                arguments.Add("--group-map");
                arguments.Add(historic.GroupMap);
            }

            foreach (var document in Split(historic.ExcludeSignupDocuments))
            {
                arguments.Add("--exclude-signup-document");
                arguments.Add(document);
            }

            foreach (var groupNumber in Split(historic.ExcludeGroupNumbers))
            {
                arguments.Add("--exclude-group-number");
                arguments.Add(groupNumber);
            }

            logger.LogInformation("Starter historisk tilmeldingssynkronisering for {Year}.", historic.Year);
            var exitCode = await DS.RegistrationImporter.Program.RunAsync(arguments.ToArray(), connectionString: settings.ConnectionString);
            if (exitCode != 0) throw new InvalidOperationException($"Historisk tilmeldingssynkronisering for {historic.Year} fejlede. Se worker-loggen.");

            logger.LogInformation("Historisk tilmeldingssynkronisering for {Year} er gennemført.", historic.Year);
        }

        private static IEnumerable<string> Split(string value)
        {
            return value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        }
    }
}
