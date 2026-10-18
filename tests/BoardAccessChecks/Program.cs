using DbUp;
using DS.Data;
using DS.DTOs;
using DS.Models;
using DS.Website;
using DS.Website.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Claims;

var connectionString = Environment.GetEnvironmentVariable("BOARD_ACCESS_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_board_access_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_board_access_tests database.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var assembly = typeof(DataDbContext).Assembly;
var initial = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(assembly, name => string.CompareOrdinal(name, "DS.Migrations.022_board_members.sql") < 0)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(initial.Successful, $"pre-membership migrations: {initial.Error?.Message}");

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await new NpgsqlCommand("""
        INSERT INTO ds28.asp_net_users
            (id, user_name, email_confirmed, phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
        VALUES ('owner', 'owner', false, false, false, false, 0);
        INSERT INTO ds28.board (key, title, required_role, created_by_user_id)
        VALUES ('legacy', 'Legacy', 'ActivityView', 'owner'), ('orphan', 'Orphan', NULL, NULL);
        """, connection).ExecuteNonQueryAsync();
}

var upgrade = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions").WithScriptsEmbeddedInAssembly(assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(upgrade.Successful, $"membership migration: {upgrade.Error?.Message}");

var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<DataDbContext>(builder => builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention().UseOpenIddict());
services.AddIdentityCore<User>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
    .AddRoles<Role>().AddEntityFrameworkStores<DataDbContext>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
foreach (var id in new[] { "member", "friend", "outsider", "admin" })
{
    Check((await manager.CreateAsync(new User { Id = id, UserName = id, FirstName = id, LastName = "Test", Email = $"{id}@example.test" })).Succeeded, $"create {id}");
}

var controller = new BoardApiController(db, manager)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
};
void ActAs(string id, params string[] roles)
{
    controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id) }.Concat(roles.Select(role => new Claim(ClaimTypes.Role, role))), "test"));
}
async Task<List<BoardSummaryDto>> Boards()
{
    return (List<BoardSummaryDto>)((OkObjectResult)await controller.GetBoards()).Value;
}
async Task<BoardDetailDto> Board(int id)
{
    return (BoardDetailDto)((OkObjectResult)await controller.GetBoard(id)).Value;
}

ActAs("owner");
Check((await Boards()).Select(board => board.Key).SequenceEqual(new[] { "legacy" }), "existing creator retains access without global roles");
Check(await db.BoardMembers.CountAsync() == 1, "migration grants only creator membership");
var created = (OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Private board" });
var boardId = (int)created.Value;
Check(await db.BoardMembers.AnyAsync(member => member.BoardId == boardId && member.UserId == "owner"), "new board creator becomes a member");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, boardId) is OkResult, "ordinary member can invite");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, boardId) is OkResult, "duplicate invitation is idempotent");
Check(await db.BoardMembers.CountAsync(member => member.BoardId == boardId && member.UserId == "member") == 1, "duplicate invitation stores one membership");

ActAs("member");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "friend" }, boardId) is OkResult, "invited member can invite another user");
Check(await controller.UpdateBoard(new BoardUpdateDto { Title = "Edited by member" }, boardId) is OkResult, "invited member can edit settings");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "missing" }, boardId) is NotFoundResult, "cannot invite nonexistent user");
var columnId = (await Board(boardId)).Columns.First().Id;
var cardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Shared", ColumnId = columnId }, boardId)).Value;
var search = (List<BoardMemberDto>)((OkObjectResult)await controller.SearchAssignees("outsider", boardId)).Value;
Check(search.Single().Id == "outsider", "member can find existing ordinary users");

ActAs("outsider", nameof(AppGroups.EventAdmin), nameof(AppRoles.ActivityAdmin));
Check((await Boards()).Count == 0, "unrelated global administrator cannot list boards");
var denied = new Dictionary<string, IActionResult>
{
    ["read"] = await controller.GetBoard(boardId),
    ["columns"] = await controller.GetColumns(boardId),
    ["members"] = await controller.GetMembers(boardId),
    ["search"] = await controller.SearchAssignees("member", boardId),
    ["invite"] = await controller.AddMember(new BoardMemberCreateDto { UserId = "outsider" }, boardId),
    ["update"] = await controller.UpdateBoard(new BoardUpdateDto { Title = "Denied" }, boardId),
    ["delete"] = await controller.DeleteBoard(boardId),
    ["create column"] = await controller.CreateColumn(new BoardColumnCreateDto { Title = "Denied" }, boardId),
    ["update column"] = await controller.UpdateColumn(new BoardColumnUpdateDto { Title = "Denied" }, boardId, columnId),
    ["delete column"] = await controller.DeleteColumn(boardId, columnId),
    ["reorder"] = await controller.ReorderColumns([columnId], boardId),
    ["create card"] = await controller.CreateCard(new WorkItemCreateDto { Title = "Denied" }, boardId),
    ["update card"] = await controller.UpdateCard(new WorkItemUpdateDto { Title = "Denied" }, boardId, cardId),
    ["move card"] = await controller.MoveCard(new WorkItemMoveDto { ColumnId = columnId }, boardId, cardId),
    ["remove card"] = await controller.RemoveCard(boardId, cardId),
    ["placement"] = await controller.AddCardPlacement(new WorkItemPlacementCreateDto { ColumnId = columnId }, boardId, cardId),
};
foreach (var (operation, result) in denied) Check(result is ForbidResult, $"outsider denied: {operation}");

var hiddenId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Hidden" })).Value;
var hiddenColumn = (await Board(hiddenId)).Columns.First().Id;
Check(await controller.AddCardPlacement(new WorkItemPlacementCreateDto { ColumnId = hiddenColumn }, hiddenId, cardId) is ForbidResult, "cannot copy guessed card from inaccessible source");
foreach (var role in new[] { nameof(AppGroups.SysAdmin), nameof(AppGroups.CampAdmin) })
{
    ActAs("admin", role);
    Check((await Boards()).Count == 4, $"{role} sees all boards including orphan");
    Check((await Board(boardId)).Id == boardId, $"{role} can open without membership");
    Check(await controller.UpdateBoard(new BoardUpdateDto { Title = role }, boardId) is OkResult, $"{role} can edit without membership");
}
Check(await controller.AddCardPlacement(new WorkItemPlacementCreateDto { ColumnId = hiddenColumn }, hiddenId, cardId) is OkResult, "administrator can share card across boards");
Check((await Board(boardId)).Cards.Single().Placements.Count == 2, "administrator sees both shared placements");

ActAs("friend");
Check((await Board(boardId)).Cards.Single().Placements.Single().BoardId == boardId, "shared card hides inaccessible board metadata");
Check(await controller.GetBoard(hiddenId) is ForbidResult, "shared card does not grant hidden board access");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Friend edit" }, boardId, cardId) is OkResult, "invited friend can edit cards");
Check(await controller.DeleteBoard(boardId) is OkResult, "member can delete board");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "outsider" }, boardId) is ForbidResult, "deleted board rejects invitations");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(!(await Boards()).Any(board => board.Id == boardId), "deleted board is hidden even from administrators");
Console.WriteLine("ALL BOARD ACCESS CHECKS PASSED");
