using DS.Application.Services;
using DS.Application.Services.Groups;
using DS.Application.Services.Registration;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DbUp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("SCOUT_CAMP_DAY_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_scout_camp_day_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_scout_camp_day_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var migrations = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(typeof(Group).Assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(migrations.Successful, $"migrations: {migrations.Error?.Message}");

var services = new ServiceCollection();
services.AddLogging();
services.AddDsDataLayer(connectionString);
services.AddDsIdentityCore();
using var provider = services.BuildServiceProvider();

int first, swap, second, last;
using (var scope = provider.CreateScope())
{
    var campDays = new CampDayService(scope.ServiceProvider.GetRequiredService<DataDbContext>());
    first = (await campDays.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 22), Half = CampHalf.First })).Value.Id;
    swap = (await campDays.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 25), Half = CampHalf.Both })).Value.Id;
    second = (await campDays.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 27), Half = CampHalf.Second })).Value.Id;
    last = (await campDays.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 29), Half = CampHalf.Second })).Value.Id;

    var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
    var nordika = new Group { Id = 9001, Name = "Nordika", District = District.DANEHOF, Scouts = [], Patrols = [] };
    var other = new Group { Id = 9002, Name = "Anden gruppe", District = District.FIONIA, Scouts = [], Patrols = [] };
    db.Groups.AddRange(nordika, other);
    await db.SaveChangesAsync();

    var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    Check((await manager.CreateAsync(new User { Id = "leader", UserName = "leader", FirstName = "Leder", LastName = "Test", Email = "leader@example.test", Group = nordika })).Succeeded, "opret gruppebruger");
    Check((await manager.CreateAsync(new User { Id = "outsider", UserName = "outsider", FirstName = "Ude", LastName = "Test", Email = "outsider@example.test", Group = other })).Succeeded, "opret bruger i anden gruppe");
}

GroupService NewGroupService(IServiceScope scope)
{
    return new GroupService(scope.ServiceProvider.GetRequiredService<DataDbContext>(), scope.ServiceProvider.GetRequiredService<UserManager<User>>());
}

CreateScoutDto NewScout(string name, List<int> campDayIds = null)
{
    return new CreateScoutDto { Name = name, Birthday = new DateTime(2015, 3, 1), Gender = Gender.Female, CampDayIds = campDayIds ?? [] };
}

int anna, bo, carl, outsiderScout;
using (var scope = provider.CreateScope())
{
    var service = NewGroupService(scope);
    var created = await service.CreateScout("leader", NewScout("Anna"));
    Check(created.Outcome == ServiceOutcome.Success && created.Value.CampDayIds.Count == 0, "ny spejder har ingen dage som standard");
    anna = created.Value.Id;

    var withDays = await service.CreateScout("leader", NewScout("Bo", [first, swap, swap]));
    Check(withDays.Outcome == ServiceOutcome.Success && withDays.Value.CampDayIds.SequenceEqual(new[] { first, swap }.Order()), "spejder kan oprettes med dage, og dubletter ignoreres");
    bo = withDays.Value.Id;

    carl = (await service.CreateScout("leader", NewScout("Carl"))).Value.Id;
    outsiderScout = (await service.CreateScout("outsider", NewScout("Ude"))).Value.Id;

    Check((await service.CreateScout("leader", NewScout("Ukendt dag", [9999]))).Outcome == ServiceOutcome.Invalid, "oprettelse med ukendt lejrdag afvises");
}

using (var scope = provider.CreateScope())
{
    var service = NewGroupService(scope);
    var set = await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna], CampDayIds = [swap, second, last] });
    Check(set.Outcome == ServiceOutcome.Success, "dage kan sættes for én spejder");

    var replaced = await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [bo], CampDayIds = [second] });
    Check(replaced.Outcome == ServiceOutcome.Success, "dage kan erstattes");

    Check((await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna], CampDayIds = [] })).Outcome == ServiceOutcome.Invalid, "tom liste af dage afvises");
    Check((await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [], CampDayIds = [first] })).Outcome == ServiceOutcome.Invalid, "tom liste af spejdere afvises");
    Check((await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna], CampDayIds = [9999] })).Outcome == ServiceOutcome.Invalid, "ukendt lejrdag afvises");
    Check((await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna, outsiderScout], CampDayIds = [first] })).Outcome == ServiceOutcome.NotFound, "spejder fra en anden gruppe afvises");
}

using (var scope = provider.CreateScope())
{
    var group = (await NewGroupService(scope).GetGroup("leader")).Value;
    Check(group.Scouts.Single(scout => scout.Id == anna).CampDayIds.SequenceEqual(new[] { swap, second, last }.Order()), "dage er gemt og vises på gruppen");
    Check(group.Scouts.Single(scout => scout.Id == bo).CampDayIds.SequenceEqual([second]), "erstattede dage er gemt");
    Check(group.Scouts.Single(scout => scout.Id == carl).CampDayIds.Count == 0, "afviste ændringer gemmes ikke");
}

using (var scope = provider.CreateScope())
{
    var service = NewGroupService(scope);
    var fill = await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna, bo, carl], CampDayIds = [first, swap], OnlyScoutsWithoutDays = true });
    Check(fill.Outcome == ServiceOutcome.Success, "udfyldning af manglende dage lykkes");

    var group = (await service.GetGroup("leader")).Value;
    Check(group.Scouts.Single(scout => scout.Id == carl).CampDayIds.SequenceEqual(new[] { first, swap }.Order()), "spejder uden dage får dage ved udfyldning");
    Check(group.Scouts.Single(scout => scout.Id == anna).CampDayIds.SequenceEqual(new[] { swap, second, last }.Order()), "udfyldning rører ikke spejdere med dage");

    var bulk = await service.SetScoutCampDays("leader", new SetScoutCampDaysDto { ScoutIds = [anna, bo], CampDayIds = [first, swap, second, last] });
    Check(bulk.Outcome == ServiceOutcome.Success, "flere spejdere kan sættes samlet");
}

using (var scope = provider.CreateScope())
{
    var campDays = new CampDayService(scope.ServiceProvider.GetRequiredService<DataDbContext>());
    var blocked = await campDays.DeleteDay(last);
    Check(blocked.Outcome == ServiceOutcome.Conflict && blocked.Error is string message && message.StartsWith("2 spejdere"), "lejrdag med tilmeldte spejdere kan ikke slettes");

    var moved = await campDays.UpdateDay(last, new CampDayUpsertDto { Date = new DateOnly(2028, 7, 30), Half = CampHalf.Second });
    Check(moved.Outcome == ServiceOutcome.Success, "datoen på en lejrdag med tilmeldinger kan rettes");
}

using (var scope = provider.CreateScope())
{
    var service = NewGroupService(scope);
    Check((await service.DeleteScout("leader", anna)).Outcome == ServiceOutcome.Success, "spejder kan slettes");
    Check((await service.DeleteScout("leader", bo)).Outcome == ServiceOutcome.Success, "anden spejder kan slettes");
}

using (var scope = provider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
    Check(!await db.ScoutCampDays.AnyAsync(registration => registration.ScoutId == anna || registration.ScoutId == bo), "slettede spejderes dage fjernes");
    Check(await db.Scouts.IgnoreQueryFilters().AnyAsync(scout => scout.Id == anna && scout.DeletedAt != null), "spejderen er soft-slettet");

    var campDays = new CampDayService(db);
    Check((await campDays.DeleteDay(last)).Outcome == ServiceOutcome.Success, "lejrdag uden tilmeldinger kan slettes");
    Check((await campDays.DeleteDay(first)).Outcome == ServiceOutcome.Conflict, "lejrdag med en tilbageværende spejder kan stadig ikke slettes");
}

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    try
    {
        await new NpgsqlCommand($"INSERT INTO ds28.scout_camp_day (scout_id, camp_day_id) VALUES ({carl}, {first})", connection).ExecuteNonQueryAsync();
        Check(false, "databasen afviser samme dag to gange for en spejder");
    }
    catch (PostgresException exception) when (exception.SqlState == "23505")
    {
        Check(true, "databasen afviser samme dag to gange for en spejder");
    }

    try
    {
        await new NpgsqlCommand($"DELETE FROM ds28.camp_day WHERE id = {first}", connection).ExecuteNonQueryAsync();
        Check(false, "databasen afviser sletning af en lejrdag med tilmeldinger");
    }
    catch (PostgresException exception) when (exception.SqlState == "23503")
    {
        Check(true, "databasen afviser sletning af en lejrdag med tilmeldinger");
    }
}
