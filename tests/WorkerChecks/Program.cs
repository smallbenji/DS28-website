using DbUp;
using DS.Domain.Models;
using DS.Infrastructure.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Npgsql;
using System.Diagnostics;
using System.Net;
using System.Collections.Concurrent;

var connectionString = Environment.GetEnvironmentVariable("WORKER_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_worker_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_worker_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var workerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../DS.Worker/bin/Debug/net10.0/DS.Worker.dll"));
var storagePath = Path.Combine(Path.GetTempPath(), $"ds28-worker-check-{Guid.NewGuid():N}");
var start = new ProcessStartInfo("dotnet")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};
start.ArgumentList.Add(workerPath);
start.Environment["DS__ConnectionString"] = connectionString;
start.Environment["DS__FileStoragePath"] = storagePath;
start.Environment["DOTNET_ENVIRONMENT"] = "Production";
var logs = new ConcurrentQueue<string>();
using var worker = new Process { StartInfo = start };
worker.OutputDataReceived += (_, args) => { if (args.Data != null) logs.Enqueue(args.Data); };
worker.ErrorDataReceived += (_, args) => { if (args.Data != null) logs.Enqueue(args.Data); };
worker.Start();
worker.BeginOutputReadLine();
worker.BeginErrorReadLine();
try
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    while (!logs.Any(line => line.Contains("Venter på databasen")))
    {
        Check(!worker.HasExited, "worker stays alive while migrations are pending");
        await Task.Delay(200, deadline.Token);
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync(deadline.Token);
    await using (var command = new NpgsqlCommand("SELECT to_regclass('hangfire.server') IS NULL", connection))
    {
        Check((bool)await command.ExecuteScalarAsync(deadline.Token), "worker does not create Hangfire schema before Website migrations");
    }

    var migration = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
        .JournalToPostgresqlTable("ds28", "schemaversions")
        .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
        .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
    Check(migration.Successful, $"Website-style migrations complete: {migration.Error?.Message}");

    string serverId = null;
    while (serverId == null)
    {
        if (worker.HasExited) throw new Exception(string.Join(Environment.NewLine, logs));
        await using var command = new NpgsqlCommand("SELECT id FROM hangfire.server LIMIT 1", connection);
        serverId = (string)await command.ExecuteScalarAsync(deadline.Token);
        if (serverId == null) await Task.Delay(200, deadline.Token);
    }
    Check(serverId.StartsWith(Dns.GetHostName() + ":", StringComparison.OrdinalIgnoreCase), "Hangfire server uses host hostname as its name");

    GlobalConfiguration.Configuration.UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString),
        new PostgreSqlStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = false });
    var jobId = new BackgroundJobClient().Enqueue<OpenIddictJobs>(job => job.Prune());
    string state = null;
    while (state != "Succeeded")
    {
        if (worker.HasExited) throw new Exception(string.Join(Environment.NewLine, logs));
        await using var command = new NpgsqlCommand("SELECT statename FROM hangfire.job WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", long.Parse(jobId));
        state = (string)await command.ExecuteScalarAsync(deadline.Token);
        if (state == "Failed") throw new Exception(string.Join(Environment.NewLine, logs));
        if (state != "Succeeded") await Task.Delay(200, deadline.Token);
    }
    Check(state == "Succeeded", "worker executes OpenIddict pruning job with registered dependencies");
    await using (var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM hangfire.hash WHERE key = 'recurring-job:prune-openiddict')", connection))
    {
        Check((bool)await command.ExecuteScalarAsync(deadline.Token), "worker registers recurring pruning job");
    }
    Console.WriteLine("ALL WORKER CHECKS PASSED");
}
catch
{
    Console.WriteLine(string.Join(Environment.NewLine, logs));
    throw;
}
finally
{
    if (!worker.HasExited)
    {
        worker.Kill(entireProcessTree: true);
        await worker.WaitForExitAsync();
    }
    if (Directory.Exists(storagePath)) Directory.Delete(storagePath, recursive: true);
}
