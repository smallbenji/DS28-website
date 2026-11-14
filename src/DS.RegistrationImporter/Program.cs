using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace DS.RegistrationImporter
{
    internal static class Program
    {
        private static Task<int> Main(string[] args)
        {
            return RunAsync(args);
        }

        internal static async Task<int> RunAsync(string[] args, HttpClient? firebaseClient = null)
        {
            try
            {
                if (args.Contains("--help"))
                {
                    Console.WriteLine("Importer --firebase-credentials file.json --year 2024 [--group-map file.json] [--exclude-signup-document id] [--exclude-group-number number] [--apply]");
                    Console.WriteLine("Prøvekørsel er standard. --apply erstatter årets historik transaktionelt. Forbindelse: DS__ConnectionString.");
                    return 0;
                }

                string? credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                var year = 0;
                string? mappingPath = null;
                var exclusions = new HashSet<string>();
                var excludedGroups = new HashSet<string>();
                var apply = false;
                for (var index = 0; index < args.Length; index++)
                {
                    if (args[index] == "--apply")
                    {
                        apply = true;
                        continue;
                    }

                    if (index + 1 >= args.Length) throw new ImportValidationException("Argument mangler værdi.");
                    switch (args[index])
                    {
                        case "--firebase-credentials": credentialsPath = args[++index]; break;
                        case "--year": year = int.Parse(args[++index]); break;
                        case "--group-map": mappingPath = args[++index]; break;
                        case "--exclude-signup-document": exclusions.Add(args[++index]); break;
                        case "--exclude-group-number": excludedGroups.Add(args[++index]); break;
                        default: throw new ImportValidationException("Ukendt argument. Brug --help.");
                    }
                }

                if (string.IsNullOrWhiteSpace(credentialsPath)) throw new ImportValidationException("Angiv --firebase-credentials eller GOOGLE_APPLICATION_CREDENTIALS.");
                if (year < 1900 || year > 9999) throw new ImportValidationException("Angiv et gyldigt lejrår med --year.");
                var credentials = await FirebaseCredentials.LoadAsync(credentialsPath);
                using var ownedClient = firebaseClient == null ? new HttpClient { Timeout = TimeSpan.FromSeconds(30) } : null;
                var source = new FirebaseSource(credentials, firebaseClient ?? ownedClient!);
                var registrations = await source.ReadAsync();
                var presignups = registrations.Presignups;
                var allSignups = registrations.Signups;
                var canonical = JsonSerializer.Serialize(new { credentials.ProjectId, CampYear = year, registrations });
                var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
                Console.WriteLine($"Firebase-projekt: {credentials.ProjectId}. Kildekontrolsum: {hash}.");
                if (presignups.Count == 0 || allSignups.Count == 0) throw new ImportValidationException("Udtrækket skal indeholde begge tilmeldingstyper.");
                if (exclusions.Any(id => allSignups.All(row => row.SourceDocumentId != id))) throw new ImportValidationException("Et udeladt dokument findes ikke i udtrækket.");
                if (excludedGroups.Any(number => presignups.Concat(allSignups).All(row => row.GroupNumber != number))) throw new ImportValidationException("Et udeladt gruppenummer findes ikke i udtrækket.");
                presignups = presignups.Where(row => !excludedGroups.Contains(row.GroupNumber)).ToList();
                var signups = allSignups.Where(row => !exclusions.Contains(row.SourceDocumentId) && !excludedGroups.Contains(row.GroupNumber)).ToList();
                if (presignups.Count == 0) throw new ImportValidationException("Alle forhåndstilmeldinger er udeladt.");
                if (signups.Count == 0) throw new ImportValidationException("Alle endelige tilmeldinger er udeladt.");
                var rows = presignups.Concat(signups).ToList();
                var manualMapping = mappingPath == null ? new Dictionary<string, int>() : JsonSerializer.Deserialize<Dictionary<string, int>>(await File.ReadAllTextAsync(mappingPath))!;
                var connectionString = Environment.GetEnvironmentVariable("DS__ConnectionString");
                if (string.IsNullOrWhiteSpace(connectionString)) throw new ImportValidationException("DS__ConnectionString mangler.");
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
                await Execute(connection, "SELECT pg_advisory_xact_lock(280024, @year)", year);
                var groups = new List<(int Id, string Name)>();
                await using (var command = new NpgsqlCommand("SELECT id, name FROM ds28.scout_group WHERE deleted_at IS NULL", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) groups.Add((reader.GetInt32(0), reader.GetString(1)));
                }

                var groupIds = new Dictionary<string, int>();
                var errors = new List<string>();
                foreach (var sourceGroup in rows.GroupBy(row => row.GroupNumber))
                {
                    var names = sourceGroup.Select(row => row.GroupName).Distinct().ToList();
                    var matches = manualMapping.TryGetValue(sourceGroup.Key, out var id)
                        ? groups.Where(group => group.Id == id).ToList()
                        : groups.Where(group => names.Count == 1 && group.Name == names[0]).ToList();
                    if (matches.Count != 1)
                    {
                        errors.Add($"Gruppe {sourceGroup.Key} ({string.Join(" / ", names)}): intet entydigt match. Brug --group-map.");
                        continue;
                    }

                    groupIds.Add(sourceGroup.Key, matches[0].Id);
                    Console.WriteLine($"Gruppe {sourceGroup.Key}: {names[0]} -> {matches[0].Id} ({matches[0].Name})");
                }

                if (groupIds.Values.Distinct().Count() != groupIds.Count) errors.Add("Flere historiske grupper er koblet til samme DS28-gruppe.");
                CheckDuplicates(presignups, "forhåndstilmelding", errors);
                CheckDuplicates(signups, "endelig tilmelding", errors);
                Console.WriteLine($"Lejr {year}: {presignups.Sum(row => (long)row.ParticipantCount)} forhåndstilmeldte + {presignups.Sum(row => (long)row.GuestCount)} gæster.");
                Console.WriteLine($"Endeligt: {signups.Where(row => row.DayIndex == 0).Sum(row => (long)row.ParticipantCount)} inklusive ledere; {signups.Where(row => row.DayIndex == 0 && row.Category == "leader").Sum(row => (long)row.ParticipantCount)} ledere.");
                Console.WriteLine($"Udeladte gruppenumre: {string.Join(", ", excludedGroups)}.");
                Console.WriteLine($"Udeladte kildedokumenter: {string.Join(", ", exclusions)}. Tomme dokumenter uden gruppekobling: {registrations.SkippedDocumentIds.Count}.");
                foreach (var error in errors) Console.Error.WriteLine(error);
                if (errors.Count > 0) return 1;
                if (!apply)
                {
                    Console.WriteLine("Prøvekørsel gennemført. Ingen data gemt. Kør med --apply for at importere.");
                    return 0;
                }

                foreach (var table in new[] { "historic_presignup", "historic_signup" })
                {
                    await Execute(connection, $"DELETE FROM ds28.{table} WHERE camp_year = @year", year);
                }

                foreach (var row in presignups) await Insert(connection, row, false, year, groupIds[row.GroupNumber], hash);
                foreach (var row in signups) await Insert(connection, row, true, year, groupIds[row.GroupNumber], hash);
                await transaction.CommitAsync();
                Console.WriteLine($"Import gemt: {presignups.Count} forhåndsrækker og {signups.Count} endelige rækker. Årets tidligere historik er erstattet.");
                return 0;
            }
            catch (PostgresException error)
            {
                Console.Error.WriteLine($"Import afbrudt. Databasefejl {error.SqlState}; kontrollér migrationer og datakrav. Ingen delvis import er gemt.");
                return 1;
            }
            catch (Exception error) when (error is InvalidOperationException or JsonException or IOException or NpgsqlException
                or ArgumentException or OverflowException or NullReferenceException or KeyNotFoundException or FormatException
                or HttpRequestException or TaskCanceledException or UnauthorizedAccessException or CryptographicException
                or Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
            {
                Console.Error.WriteLine("Import afbrudt. Kontrollér argumenter, Firebase-credentials og DS__ConnectionString. Ingen delvis import er gemt.");
                if (error is ImportValidationException) Console.Error.WriteLine(error.Message);
                return 1;
            }
        }

        private static void CheckDuplicates(List<ImportRow> rows, string label, List<string> errors)
        {
            foreach (var duplicate in rows.Where(row => row.DayIndex == 0).GroupBy(row => (row.GroupNumber, row.Category)).Where(group => group.Count() > 1 && group.Key.Category == "family"))
            {
                var documents = duplicate.Select(row => row.SourceDocumentId).Distinct().ToList();
                errors.Add($"Dubleret {label} for {duplicate.First().GroupName}: " + string.Join("; ", documents.Select(id => $"{id}: {rows.Where(row => row.SourceDocumentId == id && row.DayIndex == 0).Sum(row => (long)row.ParticipantCount)} deltagere")));
            }

            if (rows.GroupBy(row => (row.GroupNumber, row.Category, row.DayIndex)).Any(group => group.Count() > 1)) errors.Add($"Dubletter i {label}. Afklar kilden og brug eventuelt --exclude-signup-document.");
        }

        private static async Task Execute(NpgsqlConnection connection, string sql, int year)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("year", year);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task Insert(NpgsqlConnection connection, ImportRow row, bool signup, int year, int groupId, string hash)
        {
            var table = signup ? "historic_signup" : "historic_presignup";
            var extraColumn = signup ? "day_index" : "guest_count";
            await using var command = new NpgsqlCommand($"INSERT INTO ds28.{table} (group_id, camp_year, category, participant_count, {extraColumn}, source_document_id, source_sha256, imported_at) VALUES (@group, @year, @category, @count, @extra, @source, @hash, CURRENT_TIMESTAMP)", connection);
            command.Parameters.AddWithValue("group", groupId);
            command.Parameters.AddWithValue("year", year);
            command.Parameters.AddWithValue("category", row.Category);
            command.Parameters.AddWithValue("count", row.ParticipantCount);
            command.Parameters.AddWithValue("extra", signup ? row.DayIndex : row.GuestCount);
            command.Parameters.AddWithValue("source", row.SourceDocumentId);
            command.Parameters.AddWithValue("hash", hash);
            await command.ExecuteNonQueryAsync();
        }
    }
}
