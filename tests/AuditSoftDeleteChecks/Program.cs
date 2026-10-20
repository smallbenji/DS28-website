using DbUp;
using DS.Application.Data;
using DS.Application.Services;
using DS.Application.Services.Email;
using DS.Application.Services.Organization;
using DS.Domain;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("AUDIT_SOFTDELETE_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_audit_softdelete_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_audit_softdelete_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var migration = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(migration.Successful, $"migrations: {migration.Error?.Message}");

var services = new ServiceCollection();
services.AddLogging();
services.AddDsDataLayer(connectionString);
services.AddDsIdentityCore();
services.AddSingleton<IMailQueue>(new NoopMailQueue());
services.AddSingleton<IUserAccessCache>(new NoopAccessCache());
services.AddSingleton(Options.Create(new DSSettings()));
services.AddSingleton<EmailService>();
services.AddSingleton<OrgService>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var orgService = scope.ServiceProvider.GetRequiredService<OrgService>();
var orgAdmin = new OrgActor("admin", true, true);

await db.Database.ExecuteSqlRawAsync("INSERT INTO ds28.scout_group (id, name, district) VALUES (1, 'Gruppe', 'FIONIA')");
await db.Database.ExecuteSqlRawAsync("INSERT INTO ds28.scout (name, birthday, gender, group_id) VALUES ('Spejder', DATE '2015-01-01', 'MALE', 1)");

var scout = await db.Scouts.SingleAsync(s => s.Name == "Spejder");
Check(scout.CreatedAt != default && scout.UpdatedAt != default, "insert sets audit timestamps");

var createdStamp = scout.CreatedAt;
var modifiedStamp = scout.UpdatedAt;
await Task.Delay(10);
scout.Name = "Spejder2";
await db.SaveChangesAsync();
var reloaded = await db.Scouts.SingleAsync(s => s.Id == scout.Id);
Check(reloaded.CreatedAt == createdStamp, "update preserves CreatedAt");
Check(reloaded.UpdatedAt > modifiedStamp, "update advances UpdatedAt");

db.Scouts.Remove(reloaded);
await db.SaveChangesAsync();

Check(!await db.Scouts.AnyAsync(s => s.Id == scout.Id), "soft-deleted scout is hidden by the query filter");
var withDeleted = await db.Scouts.IgnoreQueryFilters().SingleAsync(s => s.Id == scout.Id);
Check(withDeleted.DeletedAt != null, "soft delete sets DeletedAt instead of removing the row");

await db.Database.ExecuteSqlRawAsync("INSERT INTO ds28.scout (name, birthday, gender, group_id) VALUES ('SpejderTo', DATE '2015-01-01', 'FEMALE', 1)");

var group = await db.Groups.SingleAsync(g => g.Id == 1);
db.Groups.Remove(group);
await db.SaveChangesAsync();
Check(!await db.Groups.AnyAsync(g => g.Id == 1), "soft-deleted group is hidden by the query filter");
Check(await db.Groups.IgnoreQueryFilters().AnyAsync(g => g.Id == 1 && g.DeletedAt != null), "soft-deleted group row is retained");

Check(!await db.Scouts.AsNoTracking().Where(s => s.Group.DeletedAt == null).AnyAsync(s => s.Id == 2), "scout whose group is soft-deleted stops appearing in participant export");

var position = new OrgPosition { Title = "Stilling" };
db.OrgPositions.Add(position);
await db.SaveChangesAsync();
var positionId = position.Id;
db.OrgPositions.Remove(position);
await db.SaveChangesAsync();
Check(!await db.OrgPositions.AnyAsync(p => p.Id == positionId), "soft-deleted position is hidden by the query filter");
Check(await db.OrgPositions.IgnoreQueryFilters().AnyAsync(p => p.Id == positionId && p.DeletedAt != null), "soft-deleted position row is retained");

var owner = new OrgPosition { Title = "Ejer" };
db.OrgPositions.Add(owner);
await db.SaveChangesAsync();
var ownerId = owner.Id;

async Task<bool> TryCreatePa(int forPositionId)
{
    try
    {
        db.OrgPositions.Add(new OrgPosition { Title = "Personlig assistent", PaForPositionId = forPositionId });
        await db.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException)
    {
        db.ChangeTracker.Clear();
        return false;
    }
}

Check((await orgService.CreatePa(orgAdmin, ownerId)).Outcome == ServiceOutcome.Success, "position gets a personal assistant");
var assistant = await db.OrgPositions.SingleAsync(p => p.PaForPositionId == ownerId);
Check((await orgService.DeletePosition(orgAdmin, assistant.Id)).Outcome == ServiceOutcome.Success, "personal assistant can be deleted");
Check(await TryCreatePa(ownerId), "position can get a new personal assistant after the old one is deleted");

db.ChangeTracker.Clear();
Check((await orgService.DeletePosition(orgAdmin, ownerId)).Outcome == ServiceOutcome.Success, "position with a personal assistant can be deleted");
Check(!await db.OrgPositions.AnyAsync(p => p.PaForPositionId == ownerId), "deleting a position soft-deletes its personal assistant");
Check(await db.OrgPositions.IgnoreQueryFilters().CountAsync(p => p.PaForPositionId == ownerId) == 2, "both historical personal assistants are retained");

var probeMaterial = new Material { Name = "Probe", Price = 1 };
db.Materials.Add(probeMaterial);
await db.SaveChangesAsync();
var probeId = probeMaterial.Id;
db.Materials.Remove(probeMaterial);
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
Check(await db.Materials.FindAsync(probeId) == null, "FindAsync honors the soft-delete query filter");
Check(await db.Materials.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == probeId) != null, "IgnoreQueryFilters still finds the soft-deleted row");

var patrol = new Patrol { Name = "Patrulje", GroupId = 1 };
db.Patrols.Add(patrol);
await db.SaveChangesAsync();
var patrolId = patrol.Id;
db.Patrols.Remove(patrol);
await db.SaveChangesAsync();
Check(!await db.Patrols.AnyAsync(p => p.Id == patrolId), "patrol is still hard deleted");

Console.WriteLine("ALL AUDIT AND SOFT-DELETE CHECKS PASSED");

internal sealed class NoopMailQueue : IMailQueue
{
    public void Enqueue(string toEmail, string toName, string subject, string body) { }
}

internal sealed class NoopAccessCache : IUserAccessCache
{
    public void Evict(string userId) { }
}
