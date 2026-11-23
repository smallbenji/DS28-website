using DS.Application.Services;
using DS.Application.Services.Registration;
using DS.Backend.Controllers.ApiControllers;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DbUp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Reflection;

var connectionString = Environment.GetEnvironmentVariable("CAMP_DAY_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_camp_day_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_camp_day_tests database.");
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

CampDayService NewService(IServiceScope scope)
{
    return new CampDayService(scope.ServiceProvider.GetRequiredService<DataDbContext>());
}

using (var scope = provider.CreateScope())
{
    var service = NewService(scope);
    var sunday = await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 23), Half = CampHalf.First });
    var saturday = await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 22), Half = CampHalf.First });
    var wednesday = await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 26), Half = CampHalf.Both });
    Check(sunday.Outcome == ServiceOutcome.Success && saturday.Outcome == ServiceOutcome.Success && wednesday.Outcome == ServiceOutcome.Success, "lejrdage kan oprettes");
    Check(wednesday.Value.Half == CampHalf.Both && wednesday.Value.Id > 0, "halvdel og id returneres");

    var days = await service.GetDays();
    Check(days.Select(day => day.Date).SequenceEqual([new DateOnly(2028, 7, 22), new DateOnly(2028, 7, 23), new DateOnly(2028, 7, 26)]), "lejrdage sorteres efter dato");

    var duplicate = await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 22), Half = CampHalf.Second });
    Check(duplicate.Outcome == ServiceOutcome.Conflict, "en dato kan ikke oprettes to gange");
    Check((await service.GetDays()).Count == 3, "afvist dublet gemmes ikke");

    var afterConflict = await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 29), Half = CampHalf.Second });
    Check(afterConflict.Outcome == ServiceOutcome.Success, "samme context kan gemme efter en konflikt");

    Check((await service.CreateDay(null)).Outcome == ServiceOutcome.Invalid, "manglende data afvises");
    Check((await service.CreateDay(new CampDayUpsertDto { Half = CampHalf.First })).Outcome == ServiceOutcome.Invalid, "manglende dato afvises");
    Check((await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 24) })).Outcome == ServiceOutcome.Invalid, "manglende halvdel afvises");
    Check((await service.CreateDay(new CampDayUpsertDto { Date = new DateOnly(2028, 7, 24), Half = (CampHalf)42 })).Outcome == ServiceOutcome.Invalid, "ukendt halvdel afvises");

    var moved = await service.UpdateDay(sunday.Value.Id, new CampDayUpsertDto { Date = new DateOnly(2028, 7, 24), Half = CampHalf.Both });
    Check(moved.Outcome == ServiceOutcome.Success && moved.Value.Date == new DateOnly(2028, 7, 24) && moved.Value.Half == CampHalf.Both, "dato og halvdel kan rettes");

    var clash = await service.UpdateDay(moved.Value.Id, new CampDayUpsertDto { Date = new DateOnly(2028, 7, 22), Half = CampHalf.First });
    Check(clash.Outcome == ServiceOutcome.Conflict, "rettelse til en eksisterende dato afvises");

    var afterClash = await service.UpdateDay(wednesday.Value.Id, new CampDayUpsertDto { Date = new DateOnly(2028, 7, 26), Half = CampHalf.Second });
    Check(afterClash.Outcome == ServiceOutcome.Success, "samme context kan gemme efter en afvist rettelse");

    Check((await service.UpdateDay(9999, new CampDayUpsertDto { Date = new DateOnly(2028, 7, 30), Half = CampHalf.First })).Outcome == ServiceOutcome.NotFound, "ukendt lejrdag giver NotFound ved rettelse");
    Check((await service.DeleteDay(9999)).Outcome == ServiceOutcome.NotFound, "ukendt lejrdag giver NotFound ved sletning");

    Check((await service.DeleteDay(saturday.Value.Id)).Outcome == ServiceOutcome.Success, "lejrdag kan slettes");
}

using (var scope = provider.CreateScope())
{
    var days = await NewService(scope).GetDays();
    Check(days.Count == 3 && days.All(day => day.Date != new DateOnly(2028, 7, 22)), "sletning er gemt");
    Check(days.Single(day => day.Date == new DateOnly(2028, 7, 24)).Half == CampHalf.Both, "rettelse er gemt");
    Check(days.Single(day => day.Date == new DateOnly(2028, 7, 26)).Half == CampHalf.Second, "rettelse efter konflikt er gemt");
}

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    try
    {
        await new NpgsqlCommand("INSERT INTO ds28.camp_day (date, half) VALUES ('2028-08-01', 'Middle')", connection).ExecuteNonQueryAsync();
        Check(false, "databasen afviser ukendt halvdel");
    }
    catch (PostgresException exception) when (exception.SqlState == "23514")
    {
        Check(true, "databasen afviser ukendt halvdel");
    }
}

var controllerType = typeof(CampDayApiController);
Check(controllerType.GetCustomAttribute<AuthorizeAttribute>()?.Roles == null, "læsning kræver kun login");
foreach (var name in new[] { nameof(CampDayApiController.CreateDay), nameof(CampDayApiController.UpdateDay), nameof(CampDayApiController.DeleteDay) })
{
    var roles = controllerType.GetMethod(name)?.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
    Check(roles == nameof(AppRoles.PreSignupManage), $"{name} kræver PreSignupManage");
}
Check(controllerType.GetMethod(nameof(CampDayApiController.GetDays))?.GetCustomAttribute<AllowAnonymousAttribute>() == null, "læsning er ikke anonym");
