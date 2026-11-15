using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;

namespace DS.RegistrationImporter
{
    internal sealed class ImportValidationException(string message) : InvalidOperationException(message);

    internal sealed record ImportRow(string GroupNumber, string GroupName, string Category, int ParticipantCount, int GuestCount, int DayIndex, string SourceDocumentId);

    internal sealed record RegistrationData(List<ImportRow> Presignups, List<ImportRow> Signups, List<string> SkippedDocumentIds);

    internal sealed record FirebaseCredentials(string ProjectId, string? ApiKey, Func<Task<string>>? GetAccessToken)
    {
        internal static async Task<FirebaseCredentials> LoadAsync(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            var value = JsonNode.Parse(json)?.AsObject() ?? throw new ImportValidationException("Tom credentials-fil.");
            if (value["type"]?.GetValue<string>() == "service_account")
            {
                var project = Required(value, "project_id");
                ValidateProject(project);
                var initializer = new ServiceAccountCredential.Initializer(Required(value, "client_email"))
                {
                    ProjectId = project,
                    Scopes = ["https://www.googleapis.com/auth/datastore"]
                }.FromPrivateKey(Required(value, "private_key"));
                var credential = new ServiceAccountCredential(initializer);
                return new FirebaseCredentials(project, null, () => credential.GetAccessTokenForRequestAsync());
            }

            if (value["type"] != null) throw new ImportValidationException("Credentials skal være en service-account-fil eller Firebase-klientkonfiguration med projectId og apiKey.");
            var projectId = Required(value, "projectId");
            ValidateProject(projectId);
            return new FirebaseCredentials(projectId, Required(value, "apiKey"), null);
        }

        private static string Required(JsonObject value, string name)
        {
            var text = value[name]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(text)) throw new ImportValidationException($"Credentials mangler {name}.");
            return text;
        }

        private static void ValidateProject(string project)
        {
            if (!Regex.IsMatch(project, "^[a-z][a-z0-9-]{4,28}[a-z0-9]$")) throw new ImportValidationException("Ugyldigt Firebase-projekt-id.");
        }
    }

    internal sealed class FirebaseSource(FirebaseCredentials credentials, HttpClient client)
    {
        internal async Task<RegistrationData> ReadAsync()
        {
            var groups = await ReadCollectionAsync("Grupper");
            var presignups = await ReadCollectionAsync("Forhaand");
            var signups = await ReadCollectionAsync("Patruljer");
            return RegistrationNormalizer.Normalize(groups, presignups, signups);
        }

        private async Task<List<SourceDocument>> ReadCollectionAsync(string collection)
        {
            var documents = new List<SourceDocument>();
            var pageTokens = new HashSet<string>();
            string? pageToken = null;
            do
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{credentials.ProjectId}/databases/(default)/documents/{collection}?pageSize=300";
                if (credentials.ApiKey != null) url += $"&key={Uri.EscapeDataString(credentials.ApiKey)}";
                if (pageToken != null) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (credentials.GetAccessToken != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await credentials.GetAccessToken());
                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) throw new ImportValidationException($"Firestore afviste læsning af {collection}: HTTP {(int)response.StatusCode}. Ingen data er importeret.");
                await using var stream = await response.Content.ReadAsStreamAsync();
                var result = (await JsonNode.ParseAsync(stream))?.AsObject() ?? throw new ImportValidationException("Tomt Firestore-svar.");
                foreach (var document in result["documents"]?.AsArray() ?? [])
                {
                    var name = document!["name"]!.GetValue<string>();
                    var id = name.Split('/')[^1];
                    var fields = document["fields"]?.AsObject() ?? new JsonObject();
                    var decoded = new JsonObject();
                    foreach (var field in fields) decoded[field.Key] = Decode(field.Value!.AsObject());
                    documents.Add(new SourceDocument(id, decoded));
                }

                pageToken = result["nextPageToken"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(pageToken) && !pageTokens.Add(pageToken)) throw new ImportValidationException("Firestore gentog en side-token; importen er afbrudt.");
            } while (!string.IsNullOrEmpty(pageToken));

            if (documents.Select(document => document.Id).Distinct().Count() != documents.Count) throw new ImportValidationException($"Dublerede dokument-id'er i {collection}.");
            return documents.OrderBy(document => document.Id, StringComparer.Ordinal).ToList();
        }

        private static JsonNode? Decode(JsonObject value)
        {
            if (value["mapValue"] is JsonObject map)
            {
                var result = new JsonObject();
                foreach (var field in map["fields"]?.AsObject() ?? new JsonObject()) result[field.Key] = Decode(field.Value!.AsObject());
                return result;
            }

            if (value["arrayValue"] is JsonObject array) return new JsonArray((array["values"]?.AsArray() ?? []).Select(item => Decode(item!.AsObject())).ToArray());
            if (value.ContainsKey("integerValue")) return JsonValue.Create(long.Parse(value["integerValue"]!.GetValue<string>(), CultureInfo.InvariantCulture));
            foreach (var type in new[] { "stringValue", "booleanValue", "nullValue", "doubleValue", "timestampValue" })
            {
                if (value.ContainsKey(type)) return value[type]?.DeepClone();
            }

            throw new ImportValidationException("Ukendt Firestore-felttype.");
        }
    }

    internal sealed record SourceDocument(string Id, JsonObject Data);
}
