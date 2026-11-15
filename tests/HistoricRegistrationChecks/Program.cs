using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DS.RegistrationImporter;
using Npgsql;

var temporaryDirectory = Path.Combine(Path.GetTempPath(), "ds28-historic-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryDirectory);
var credentialsPath = Path.Combine(temporaryDirectory, "credentials.json");
await File.WriteAllTextAsync(credentialsPath, """{"projectId":"demo-project","apiKey":"test-key"}""");
var connectionString = Environment.GetEnvironmentVariable("HISTORIC_CHECK_CONNECTION");
var originalConnection = Environment.GetEnvironmentVariable("DS__ConnectionString");
var passed = 0;

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    passed++;
    Console.WriteLine($"PASS {message}");
}

async Task ExpectValidation(Func<Task> action, string message)
{
    try
    {
        await action();
    }
    catch (ImportValidationException)
    {
        Check(true, message);
        return;
    }

    throw new Exception(message);
}

var fields = new[] { "FamilieSpejder", "Baever", "Ulve", "Junior", "Trop", "Senior", "Klan", "Leder" };
JsonObject Pre(string uid, int size)
{
    var data = new JsonObject { ["_Gruppe"] = new JsonObject { ["GruppeUID"] = uid }, ["venskabsgruppe"] = false };
    foreach (var field in fields) data[field] = field == "Junior" ? size : 0;
    return data;
}

JsonObject Signup(string email, int size)
{
    var days = new JsonObject();
    foreach (var day in new[] { "LørdagStart", "Søndag", "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag", "LørdagSlut" }) days[day] = true;
    return new JsonObject
    {
        ["email"] = email,
        ["Junior"] = new JsonArray(new JsonObject { ["key"] = 123, ["antal"] = size, ["dage"] = days }),
        ["Ledere"] = new JsonObject { ["Juniorer"] = 2 }
    };
}

var groups = new List<SourceDocument>
{
    new("group", new JsonObject { ["GruppeUID"] = "uid", ["GruppeNummer"] = "123", ["GruppeNavn"] = "Demo", ["email"] = "demo@example.invalid" }),
    new("test", new JsonObject { ["GruppeUID"] = "test", ["GruppeNummer"] = "999", ["GruppeNavn"] = "Test", ["email"] = "test@example.invalid" })
};
var presignups = new List<SourceDocument> { new("pre", Pre("uid", 5)), new("pre-test", Pre("test", 1)) };
var signups = new List<SourceDocument>
{
    new("first", Signup("demo@example.invalid", 3)), new("second", Signup("demo@example.invalid", 4)),
    new("test", Signup("test@example.invalid", 1)), new("empty", new JsonObject())
};
var collections = new Dictionary<string, List<SourceDocument>> { ["Grupper"] = groups, ["Forhaand"] = presignups, ["Patruljer"] = signups };

async Task<(int Code, string Output)> Run(bool failSource, params string[] arguments)
{
    using var client = new HttpClient(new FirestoreHandler(collections) { Fail = failSource });
    var output = new StringWriter();
    var previousOut = Console.Out;
    var previousError = Console.Error;
    try
    {
        Console.SetOut(output);
        Console.SetError(output);
        var args = new[] { "--firebase-credentials", credentialsPath, "--year", "2024" }.Concat(arguments).ToArray();
        var code = await DS.RegistrationImporter.Program.RunAsync(args, client);
        return (code, output.ToString());
    }
    finally
    {
        Console.SetOut(previousOut);
        Console.SetError(previousError);
    }
}

async Task<object?> Sql(string sql)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    return await command.ExecuteScalarAsync();
}

try
{
    Check(RegistrationNormalizer.Count(JsonValue.Create("")) == 0 && RegistrationNormalizer.Count(JsonValue.Create("12")) == 12, "Legacy blank and numeric counts");
    foreach (var value in new JsonNode?[] { null, JsonValue.Create(true), JsonValue.Create(-1), JsonValue.Create("1.2"), JsonValue.Create("invalid") })
    {
        await ExpectValidation(() => Task.FromResult(RegistrationNormalizer.Count(value)), "Invalid count rejected");
    }

    var credentials = await FirebaseCredentials.LoadAsync(credentialsPath);
    Check(credentials.ProjectId == "demo-project" && credentials.ApiKey == "test-key", "Firebase client configuration accepted");
    var inlineCredentials = await FirebaseCredentials.LoadAsync("""{"projectId":"demo-project","apiKey":"test-key"}""");
    Check(inlineCredentials.ProjectId == "demo-project" && inlineCredentials.ApiKey == "test-key", "Inline Firebase client configuration accepted");
    var servicePath = Path.Combine(temporaryDirectory, "service-account.json");
    using (var key = RSA.Create(2048))
    {
        await File.WriteAllTextAsync(servicePath, JsonSerializer.Serialize(new { type = "service_account", project_id = "demo-project", client_email = "test@demo-project.iam.gserviceaccount.com", private_key = key.ExportPkcs8PrivateKeyPem() }));
    }
    var serviceCredentials = await FirebaseCredentials.LoadAsync(servicePath);
    Check(serviceCredentials.ProjectId == "demo-project" && serviceCredentials.ApiKey == null && serviceCredentials.GetAccessToken != null, "Service account key accepted without API key");
    await File.WriteAllTextAsync(servicePath, """{"type":"authorized_user","refresh_token":"secret"}""");
    await ExpectValidation(() => FirebaseCredentials.LoadAsync(servicePath), "Unsupported credential type rejected");
    await File.WriteAllTextAsync(servicePath, """{"projectId":"../other","apiKey":"secret"}""");
    await ExpectValidation(() => FirebaseCredentials.LoadAsync(servicePath), "Invalid project id rejected");

    using (var client = new HttpClient(new FirestoreHandler(collections)))
    {
        var data = await new FirebaseSource(credentials, client).ReadAsync();
        Check(data.Presignups.Count == 16 && data.SkippedDocumentIds.SequenceEqual(["empty"]), "All Firestore pages read and empty orphan skipped");
        Check(data.Signups.Where(row => row.Category == "leader").All(row => row.DayIndex == 0), "Unknown leader attendance stays unknown");
        Check(data.Signups.Where(row => row.DayIndex == 0).Sum(row => row.ParticipantCount) == 14, "Patrols and separate leader counts preserved");
    }
    using (var client = new HttpClient(new FirestoreHandler(collections) { ExpectBearer = true }))
    {
        await new FirebaseSource(new FirebaseCredentials("demo-project", null, () => Task.FromResult("fake-token")), client).ReadAsync();
        Check(true, "Service account token sent in Authorization header");
    }
    using (var client = new HttpClient(new FirestoreHandler(collections) { RepeatPageToken = true }))
    {
        await ExpectValidation(() => new FirebaseSource(credentials, client).ReadAsync(), "Repeated pagination token rejected");
    }

    var badSignup = Signup("demo@example.invalid", 3);
    badSignup["Junior"]![0]!["dage"]!.AsObject().Remove("Mandag");
    await ExpectValidation(() => Task.FromResult(RegistrationNormalizer.Normalize(groups, presignups, [new("bad", badSignup)])), "Missing attendance day rejected");
    var orphan = Signup("", 3);
    await ExpectValidation(() => Task.FromResult(RegistrationNormalizer.Normalize(groups, presignups, [new("bad", orphan)])), "Nonempty orphan rejected");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine($"{passed} checks passed. Database checks skipped; set HISTORIC_CHECK_CONNECTION to an isolated ds28_historic_tests database.");
        return;
    }
    if (new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_historic_tests") throw new Exception("Database checks require a disposable database named ds28_historic_tests.");
    Environment.SetEnvironmentVariable("DS__ConnectionString", connectionString);
    await Sql("DROP SCHEMA IF EXISTS ds28 CASCADE; CREATE SCHEMA ds28; CREATE TABLE ds28.scout_group (id INTEGER PRIMARY KEY, name TEXT, deleted_at TIMESTAMPTZ); INSERT INTO ds28.scout_group (id, name) VALUES (1, 'Demo'), (2, 'Test');");
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    await Sql(await File.ReadAllTextAsync(Path.Combine(root, "src/DS.Domain/Migrations/027_historic_registration.sql")));

    var result = await Run(false, "--apply");
    Check(result.Code == 1 && result.Output.Contains("Dubleret"), "Duplicate submissions block apply");
    Check(Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup")) == 0, "Duplicate failure writes nothing");
    result = await Run(false, "--exclude-signup-document", "first");
    Check(result.Code == 0 && Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup")) == 0, "Dry run writes nothing");
    for (var iteration = 0; iteration < 2; iteration++)
    {
        result = await Run(false, "--exclude-signup-document", "first", "--apply");
        Check(result.Code == 0, "Repeat live import succeeds");
    }
    Check(Convert.ToInt64(await Sql("SELECT sum(participant_count) FROM ds28.historic_signup WHERE day_index = 0")) == 9, "Repeat import creates no duplicates");
    var before = Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup"));
    result = await Run(true, "--exclude-signup-document", "first", "--apply");
    Check(result.Code == 1 && !result.Output.Contains("test-key") && !result.Output.Contains("sensitive-response"), "Source denial hides credentials and response content");
    Check(Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup")) == before, "Source failure preserves history");
    await Sql("UPDATE ds28.scout_group SET name = 'changed' WHERE id = 1");
    result = await Run(false, "--exclude-signup-document", "first", "--apply");
    Check(result.Code == 1 && Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup")) == before, "Unknown group preserves history");
    await Sql("UPDATE ds28.scout_group SET name = 'Demo' WHERE id = 1");
    await Sql("CREATE FUNCTION ds28.reject_import() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'test'; END $$; CREATE TRIGGER reject_import BEFORE INSERT ON ds28.historic_signup FOR EACH ROW EXECUTE FUNCTION ds28.reject_import();");
    result = await Run(false, "--exclude-signup-document", "first", "--apply");
    Check(result.Code == 1 && Convert.ToInt64(await Sql("SELECT count(*) FROM ds28.historic_signup")) == before, "Write failure rolls back entire replacement");
    await Sql("DROP TRIGGER reject_import ON ds28.historic_signup; DROP FUNCTION ds28.reject_import();");
    result = await Run(false, "--exclude-signup-document", "first", "--exclude-group-number", "999", "--apply");
    Check(result.Code == 0 && Convert.ToInt64(await Sql("SELECT sum(participant_count) FROM ds28.historic_presignup")) == 5, "Explicit group exclusion affects both registration types");
    Console.WriteLine($"{passed} checks passed.");
}
finally
{
    Environment.SetEnvironmentVariable("DS__ConnectionString", originalConnection);
    Directory.Delete(temporaryDirectory, true);
}

internal sealed class FirestoreHandler(Dictionary<string, List<SourceDocument>> collections) : HttpMessageHandler
{
    internal bool Fail { get; init; }
    internal bool RepeatPageToken { get; init; }
    internal bool ExpectBearer { get; init; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || request.RequestUri!.Host != "firestore.googleapis.com") throw new Exception("Unexpected source request");
        if (ExpectBearer)
        {
            if (request.Headers.Authorization?.ToString() != "Bearer fake-token" || request.RequestUri.Query.Contains("key=")) throw new Exception("Invalid bearer authentication");
        }
        else if (!request.RequestUri.Query.Contains("key=test-key")) throw new Exception("Missing Firebase key");
        if (Fail) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("sensitive-response") });
        var collection = request.RequestUri.Segments[^1];
        var documents = collections[collection];
        var secondPage = request.RequestUri.Query.Contains("pageToken=");
        var selected = secondPage ? documents.Skip(1) : documents.Take(1);
        var array = new JsonArray();
        foreach (var document in selected)
        {
            var fields = new JsonObject();
            foreach (var field in document.Data) fields[field.Key] = Encode(field.Value);
            array.Add(new JsonObject { ["name"] = $"projects/demo-project/databases/(default)/documents/{collection}/{document.Id}", ["fields"] = fields });
        }
        var result = new JsonObject { ["documents"] = array };
        if (!secondPage || RepeatPageToken) result["nextPageToken"] = "page2";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(result.ToJsonString()) });
    }

    private static JsonObject Encode(JsonNode? value)
    {
        if (value == null) return new JsonObject { ["nullValue"] = null };
        if (value is JsonObject obj)
        {
            var fields = new JsonObject();
            foreach (var field in obj) fields[field.Key] = Encode(field.Value);
            return new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = fields } };
        }
        if (value is JsonArray array) return new JsonObject { ["arrayValue"] = new JsonObject { ["values"] = new JsonArray(array.Select(item => (JsonNode)Encode(item)).ToArray()) } };
        var scalar = value.AsValue();
        if (scalar.TryGetValue<bool>(out var boolean)) return new JsonObject { ["booleanValue"] = boolean };
        if (scalar.TryGetValue<int>(out var number)) return new JsonObject { ["integerValue"] = number.ToString() };
        return new JsonObject { ["stringValue"] = scalar.GetValue<string>() };
    }
}
