using DS.Domain;
using DS.Backend.Mcp;
using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol;
using DbUp;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DS.Application.Services.Boards;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Backend;
using DS.Backend.Controllers.ApiControllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Claims;

var connectionString = Environment.GetEnvironmentVariable("BOARD_ACCESS_TEST_CONNECTION");

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var boardColumns = new List<BoardColumnDto> { new() { Id = 4, Title = "I gang" }, new() { Id = 7, Title = "Færdig" } };
Check(DS.Backend.Mcp.BoardInputs.ResolveColumn(boardColumns, 7, null) == 7, "MCP finder kolonne på id");
Check(DS.Backend.Mcp.BoardInputs.ResolveColumn(boardColumns, null, " i GANG ") == 4, "MCP finder kolonne på titel uden hensyn til store bogstaver");
Check(DS.Backend.Mcp.BoardInputs.ToHtml("Hej\n\nVerden") == "<p>Hej</p><p>Verden</p>", "MCP laver afsnit af ren tekst");
Check(DS.Backend.Mcp.BoardInputs.ToHtml("<p>findes</p>") == "<p>findes</p>", "MCP lader HTML være");

var boardUsers = new List<BoardMemberDto>
{
    new() { Id = "u-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" },
    new() { Id = "u-2", FirstName = "Ada", LastName = "Lovelace Jr", Email = "ada.jr@example.com" },
};
Check(DS.Backend.Mcp.BoardInputs.MatchUser(boardUsers, " ada lovelace ").Id == "u-1", "MCP matcher ansvarlig på fuldt navn");
Check(DS.Backend.Mcp.BoardInputs.MatchUser(boardUsers, "ADA@EXAMPLE.COM").Id == "u-1", "MCP matcher ansvarlig på email");

var boardText = DS.Backend.Mcp.BoardText.Board(new BoardDetailDto
{
    Id = 1,
    Title = "Vejfest",
    Columns = boardColumns,
    Colors = [new BoardColorDto { Id = 3, Name = "Blå", Color = "#3366ff" }],
    Cards =
    [
        new WorkItemDto
        {
            Id = 42,
            Title = "Fejl",
            ColumnId = 4,
            Color = "#3366ff",
            DueDate = new DateOnly(2026, 11, 20),
            Assignees = [new UserSummaryDto { FirstName = "Ada", LastName = "Lovelace" }],
        },
    ],
});
Check(boardText.Contains("Blå: #3366ff"), "MCP-tekst viser tavlens farvepalette");
Check(boardText.Contains("DS-42 Fejl | kolonne I gang (id 4) | frist 2026-11-20 | farve #3366ff | ansvarlige: Ada Lovelace"), "MCP-tekst viser kortets felter");

var subjectOnly = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")]));
Check(DS.Backend.Mcp.McpAuthenticationHandler.WithUserId(subjectOnly).FindFirst(ClaimTypes.NameIdentifier)?.Value == "user-1", "MCP bruger tokenets sub som bruger-id");

var alreadyMapped = new ClaimsPrincipal(new ClaimsIdentity([
    new Claim(ClaimTypes.NameIdentifier, "user-2"),
    new Claim("sub", "user-1")
]));
Check(DS.Backend.Mcp.McpAuthenticationHandler.WithUserId(alreadyMapped).FindFirst(ClaimTypes.NameIdentifier)?.Value == "user-2", "MCP overskriver ikke et eksisterende bruger-id");

if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_board_access_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_board_access_tests database.");
}

var assembly = typeof(Group).Assembly;
Check(assembly.GetManifestResourceNames().Contains("DS.Migrations.000_legacy_v016.sql"), "migration resource names retain their original journal identity");
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
        INSERT INTO ds28.work_item (title, assignee_user_id) VALUES ('Existing uncolored card', 'owner');
        """, connection).ExecuteNonQueryAsync();
}

var fixedPaletteUpgrade = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(assembly, name => string.CompareOrdinal(name, "DS.Migrations.024_board_colors.sql") < 0)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(fixedPaletteUpgrade.Successful, $"fixed palette migration: {fixedPaletteUpgrade.Error?.Message}");
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await new NpgsqlCommand("INSERT INTO ds28.work_item (title, color) VALUES ('Existing colored card', '#dbeafe')", connection).ExecuteNonQueryAsync();
}

var upgrade = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions").WithScriptsEmbeddedInAssembly(assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(upgrade.Successful, $"membership migration: {upgrade.Error?.Message}");
var repeatedUpgrade = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build();
Check(!repeatedUpgrade.GetScriptsToExecute().Any(), "original migration journal prevents scripts from running again");

var services = new ServiceCollection();
services.AddLogging();
services.AddDsDataLayer(connectionString);
services.AddDsIdentityCore();
services.AddSingleton<IAuthenticationService, TestMcpAuthentication>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
Check((await db.WorkItems.SingleAsync(item => item.Title == "Existing uncolored card")).Color == null, "existing cards migrate without a color");
Check((await db.WorkItems.Include(item => item.Assignees).SingleAsync(item => item.Title == "Existing uncolored card")).Assignees.Single().UserId == "owner", "assignee migration preserves existing assignment without index name collision");
Check((await db.WorkItems.SingleAsync(item => item.Title == "Existing colored card")).Color == "#dbeafe", "board palette migration preserves existing card colors");
Check(await db.BoardColors.CountAsync() == 20, "existing boards receive ten default palette colors each");
foreach (var id in new[] { "member", "friend", "outsider", "admin" })
{
    Check((await manager.CreateAsync(new User { Id = id, UserName = id, FirstName = id, LastName = "Test", Email = $"{id}@example.test" })).Succeeded, $"create {id}");
}

var boardEvents = new RecordingBoardEvents();
var controller = new BoardApiController(new BoardService(db, manager, boardEvents), manager)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider } }
};
var mcp = new BoardTools(new BoardService(db, manager, boardEvents), new HttpContextAccessor { HttpContext = controller.HttpContext }, manager);
async Task RejectMcp(Func<Task<string>> action, string message)
{
    try
    {
        await action();
    }
    catch (McpException)
    {
        Check(true, message);
        return;
    }
    throw new Exception(message);
}
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
Check((await Board(boardId)).Colors.Count == 10, "new board receives ten default palette colors");
Check(await db.BoardMembers.AnyAsync(member => member.BoardId == boardId && member.UserId == "owner"), "new board creator becomes a member");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, boardId) is OkResult, "ordinary member can invite");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, boardId) is OkResult, "duplicate invitation is idempotent");
Check(await db.BoardMembers.CountAsync(member => member.BoardId == boardId && member.UserId == "member") == 1, "duplicate invitation stores one membership");

ActAs("member");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "friend" }, boardId) is OkResult, "invited member can invite another user");
Check(await controller.UpdateBoard(new BoardUpdateDto { Title = "Edited by member" }, boardId) is OkResult, "invited member can edit settings");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "missing" }, boardId) is NotFoundResult, "cannot invite nonexistent user");
var columnId = (await Board(boardId)).Columns.First().Id;
var paletteId = (int)((OkObjectResult)await controller.CreateColor(new BoardColorUpdateDto { Name = "  Haster  ", Color = " #AB1234 " }, boardId)).Value;
Check((await Board(boardId)).Colors.Any(color => color.Id == paletteId && color.Name == "Haster" && color.Color == "#ab1234"), "member can create named custom color with normalized values");
Check(await controller.UpdateColor(new BoardColorUpdateDto { Name = "Akut", Color = "#ab1234" }, boardId, paletteId) is OkResult, "ordinary member can edit palette colors");
Check(!await db.BoardColors.AnyAsync(color => color.BoardId == 1 && color.Id == paletteId), "palette is scoped to its board");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = " ", Color = "#123456" }, boardId) is BadRequestObjectResult, "palette color requires a name");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = new string('x', 65), Color = "#123456" }, boardId) is BadRequestObjectResult, "palette color name is length limited");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = "Invalid", Color = "red" }, boardId) is BadRequestObjectResult, "palette rejects invalid colors");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = "Missing" }, boardId) is BadRequestObjectResult, "palette requires a color");
var otherPaletteId = await db.BoardColors.Where(color => color.BoardId == 1).Select(color => color.Id).FirstAsync();
Check(await controller.UpdateColor(new BoardColorUpdateDto { Name = "Denied", Color = "#123456" }, boardId, otherPaletteId) is NotFoundResult, "cannot update color belonging to another board");
Check(await controller.DeleteColor(boardId, otherPaletteId) is NotFoundResult, "cannot delete color belonging to another board");
var cardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Shared", ColumnId = columnId, Color = " #DBEAFE " }, boardId)).Value;
Check(boardEvents.Changes.Contains(boardId), "card creation notifies the board");
Check((await Board(boardId)).Cards.Single().Color == "#dbeafe", "new card color is normalized and returned");
Check(await controller.CreateCard(new WorkItemCreateDto { Title = "Invalid", ColumnId = columnId, Color = "red" }, boardId) is BadRequestObjectResult, "invalid create color rejected");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#gg0000" }, boardId, cardId) is BadRequestObjectResult, "invalid update color rejected");
Check((await Board(boardId)).Cards.Single().Color == "#dbeafe", "invalid update preserves stored color");
var search = (List<BoardMemberDto>)((OkObjectResult)await controller.SearchAssignees("outsider", boardId)).Value;
Check(search.Single().Id == "outsider", "member can find existing ordinary users");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#dbeafe", AssigneeUserIds = [" member ", "friend", "member"] }, boardId, cardId) is OkResult, "card accepts multiple assignees and normalizes duplicate IDs");
Check((await Board(boardId)).Cards.Single().Assignees.Select(user => user.Id).Order().SequenceEqual(new[] { "friend", "member" }), "card returns both assigned users");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", AssigneeUserIds = ["member", "missing"] }, boardId, cardId) is BadRequestObjectResult, "unknown assignee rejects entire update");
Check((await Board(boardId)).Cards.Single().Assignees.Count == 2, "invalid update preserves all assignees");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#dbeafe", AssigneeUserIds = ["friend"] }, boardId, cardId) is OkResult, "individual assignee can be removed");
Check((await Board(boardId)).Cards.Single().Assignees.Single().Id == "friend", "removing one assignee preserves the other");

var releaseTag = (int)((OkObjectResult)await controller.CreateTag(new BoardTagUpdateDto { Name = " v0.2.13 ", Color = "#DBEAFE" }, boardId)).Value;
var fixTag = (int)((OkObjectResult)await controller.CreateTag(new BoardTagUpdateDto { Name = "Fix" }, boardId)).Value;
Check((await Board(boardId)).Tags.Single(tag => tag.Id == releaseTag).Color == "#dbeafe", "tag color and name are normalized");
Check(await controller.CreateTag(new BoardTagUpdateDto { Name = "V0.2.13" }, boardId) is ConflictObjectResult, "tag names are case-insensitively unique per board");
Check(await controller.CreateTag(new BoardTagUpdateDto { Name = " " }, boardId) is BadRequestObjectResult, "empty tag name rejected");
Check(await controller.CreateTag(new BoardTagUpdateDto { Name = new string('x', 65) }, boardId) is BadRequestObjectResult, "long tag name rejected");
Check(await controller.CreateTag(new BoardTagUpdateDto { Name = "Bad", Color = "red" }, boardId) is BadRequestObjectResult, "invalid tag color rejected");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", AssigneeUserIds = ["friend"], TagIds = [releaseTag, fixTag, releaseTag] }, boardId, cardId) is OkResult, "card accepts multiple tags and deduplicates IDs");
Check((await Board(boardId)).Cards.Single().Tags.Count == 2, "assigned tags returned on card");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", AssigneeUserIds = ["friend"] }, boardId, cardId) is OkResult, "legacy update without tagIds succeeds");
Check((await Board(boardId)).Cards.Single().Tags.Count == 2, "legacy update preserves tags");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Must not save", TagIds = [-1] }, boardId, cardId) is BadRequestObjectResult, "unknown tag rejects entire card update");
Check((await Board(boardId)).Cards.Single().Title == "Shared", "invalid tag does not partially update card");

var taggedCardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Tagged on creation", ColumnId = columnId, TagIds = [releaseTag, fixTag] }, boardId)).Value;
Check((await Board(boardId)).Cards.Single(card => card.Id == taggedCardId).Tags.Count == 2, "new card persists tag assignments atomically");
Check(await controller.CreateCard(new WorkItemCreateDto { Title = "Invalid", ColumnId = columnId, TagIds = [-1] }, boardId) is BadRequestObjectResult, "unknown tag rejects new card");
Check(await controller.RemoveCard(boardId, taggedCardId) is OkResult, "tagged placement can be removed");
Check((await Board(boardId)).Cards.Count == 1, "removing tagged placement preserves existing card");

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
    ["create tag"] = await controller.CreateTag(new BoardTagUpdateDto { Name = "Denied" }, boardId),
    ["update tag"] = await controller.UpdateTag(new BoardTagUpdateDto { Name = "Denied" }, boardId, releaseTag),
    ["delete tag"] = await controller.DeleteTag(boardId, releaseTag),
    ["create palette color"] = await controller.CreateColor(new BoardColorUpdateDto { Name = "Denied", Color = "#123456" }, boardId),
    ["update palette color"] = await controller.UpdateColor(new BoardColorUpdateDto { Name = "Denied", Color = "#123456" }, boardId, paletteId),
    ["delete palette color"] = await controller.DeleteColor(boardId, paletteId),
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
Check(boardEvents.Changes.Contains(boardId) && boardEvents.Changes.Contains(hiddenId), "shared card placement notifies both boards");
Check((await Board(boardId)).Cards.Single().Placements.Count == 2, "administrator sees both shared placements");
Check((await Board(hiddenId)).Cards.Single().Assignees.Single().Id == "friend", "assignees follow shared card across boards");
foreach (var color in new[] { "#dbeafe", "#dcfce7", "#fef3c7", "#fee2e2", "#ede9fe", "#ccfbf1", "#ab1234", "#000000", "#ffffff" })
{
    Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = color }, boardId, cardId) is OkResult, $"can set palette color {color}");
    Check((await Board(hiddenId)).Cards.Single().Color == color, $"color {color} follows shared card across boards");
}
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#ab1234" }, boardId, cardId) is OkResult, "custom palette color can be assigned to card");
Check(await controller.UpdateColor(new BoardColorUpdateDto { Name = "Nyt navn", Color = "#654321" }, boardId, paletteId) is OkResult, "palette color can be renamed and recolored");
Check((await Board(hiddenId)).Cards.Single().Color == "#ab1234", "palette edits do not recolor shared cards");
Check(await controller.DeleteColor(boardId, paletteId) is OkResult, "palette color can be deleted");
Check((await Board(boardId)).Cards.Single().Color == "#ab1234", "deleting palette color preserves existing card color");
Check((await Board(hiddenId)).Colors.All(color => color.Color != "#ab1234"), "shared card does not expose source board palette");

var privateTag = (int)((OkObjectResult)await controller.CreateTag(new BoardTagUpdateDto { Name = "Private" }, hiddenId)).Value;
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", TagIds = [privateTag] }, hiddenId, cardId) is OkResult, "shared card accepts target board tags");
Check((await Board(boardId)).Cards.Single().Tags.Count == 2, "tag assignment on other board does not alter source placement");
Check((await Board(hiddenId)).Cards.Single().Tags.Single().Id == privateTag, "shared placement returns only its own board tags");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", TagIds = [privateTag] }, boardId, cardId) is BadRequestObjectResult, "cross-board tag assignment rejected");
Check(await controller.UpdateTag(new BoardTagUpdateDto { Name = "Changed" }, boardId, privateTag) is NotFoundResult, "cross-board tag edit rejected");
Check(await controller.DeleteTag(boardId, privateTag) is NotFoundResult, "cross-board tag deletion rejected");
Check(await controller.UpdateTag(new BoardTagUpdateDto { Name = "v0.2.14", Color = "#000000" }, boardId, releaseTag) is OkResult, "tag can be renamed and recolored");
Check((await Board(boardId)).Cards.Single().Tags.Single(tag => tag.Id == releaseTag).Name == "v0.2.14", "renaming propagates to assigned cards");
Check(await controller.UpdateTag(new BoardTagUpdateDto { Name = "Fix" }, boardId, releaseTag) is ConflictObjectResult, "rename cannot duplicate another tag");
Check(await controller.DeleteTag(boardId, fixTag) is OkResult, "tag can be deleted");
Check((await Board(boardId)).Cards.Single().Tags.Single().Id == releaseTag, "deleting tag removes only its assignments");
Check((await Board(hiddenId)).Cards.Single().Tags.Single().Id == privateTag, "deleting source tag leaves other board tags intact");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", TagIds = [] }, boardId, cardId) is OkResult, "empty tagIds clears selection");
Check((await Board(boardId)).Cards.Single().Tags.Count == 0, "all tags cleared from selected placement");
Check(boardEvents.Changes.Contains(hiddenId), "tag changes publish live updates");

await using (var tagConnection = new NpgsqlConnection(connectionString))
{
    await tagConnection.OpenAsync();
    var sourcePlacementId = await new NpgsqlCommand($"SELECT id FROM ds28.board_card WHERE board_id = {boardId} AND work_item_id = {cardId}", tagConnection).ExecuteScalarAsync();
    try
    {
        await new NpgsqlCommand($"INSERT INTO ds28.board_card_tag (board_id, board_card_id, board_tag_id) VALUES ({boardId}, {sourcePlacementId}, {privateTag})", tagConnection).ExecuteNonQueryAsync();
        throw new Exception("Cross-board foreign key was not enforced");
    }
    catch (PostgresException exception) when (exception.SqlState == "23503")
    {
        Check(true, "database rejects cross-board tag assignment independently of service");
    }
}

ActAs("owner");
var remoteTagText = await mcp.CreateTag(boardId, " Remote release ", "#DBEAFE");
var remoteTag = (await Board(boardId)).Tags.Single(tag => tag.Name == "Remote release");
Check(remoteTagText.Contains($"tag {remoteTag.Id}") && remoteTag.Color == "#dbeafe", "remote MCP creates normalized tag and returns its id");
await RejectMcp(() => mcp.CreateTag(boardId, "REMOTE RELEASE"), "remote MCP reports duplicate tag conflict");
await RejectMcp(() => mcp.CreateTag(boardId, "Bad color", "red"), "remote MCP reports invalid tag color");
await mcp.UpdateTag(boardId, remoteTag.Id, name: "MCP release");
Check((await Board(boardId)).Tags.Single(tag => tag.Id == remoteTag.Id).Color == "#dbeafe", "remote tag rename preserves omitted color");
await mcp.UpdateTag(boardId, remoteTag.Id, color: "");
Check((await Board(boardId)).Tags.Single(tag => tag.Id == remoteTag.Id).Color == null, "remote tag empty color clears it");
await RejectMcp(() => mcp.UpdateTag(boardId, privateTag, name: "Denied"), "remote MCP cannot rename another board's tag");
await RejectMcp(() => mcp.DeleteTag(boardId, privateTag), "remote MCP cannot delete another board's tag");
await RejectMcp(() => mcp.CreateTag(hiddenId, "Denied"), "remote MCP cannot create tags on hidden board");
await RejectMcp(() => mcp.GetBoard(hiddenId), "remote MCP cannot read hidden board tags");
await mcp.CreateCard(boardId, "MCP tagged", columnId: columnId, tagIds: [remoteTag.Id, remoteTag.Id]);
var remoteCard = (await Board(boardId)).Cards.Single(card => card.Title == "MCP tagged");
Check(remoteCard.Tags.Single().Id == remoteTag.Id, "remote MCP creates card with normalized tag selection");
await mcp.UpdateCard(boardId, remoteCard.Id, title: "MCP renamed");
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Tags.Single().Id == remoteTag.Id, "remote card update preserves omitted tags");
await mcp.UpdateCard(boardId, remoteCard.Id, title: "MCP null", tagIds: null);
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Tags.Single().Id == remoteTag.Id, "remote card update preserves null tags");
Check((await mcp.UpdateCard(boardId, remoteCard.Id, tagIds: [remoteTag.Id])).Contains("uændret"), "remote MCP recognizes unchanged tag selection");
await mcp.UpdateCard(boardId, remoteCard.Id, tagIds: [remoteTag.Id, releaseTag, remoteTag.Id]);
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Tags.Count == 2, "remote MCP sets multiple tags");
await RejectMcp(() => mcp.UpdateCard(boardId, remoteCard.Id, title: "Must not save", tagIds: [privateTag]), "remote MCP rejects cross-board tags before mutation");
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Title == "MCP null", "remote invalid tag does not partially update card");
var remoteBoardText = await mcp.GetBoard(boardId);
Check(remoteBoardText.Contains($"id {remoteTag.Id}: MCP release | farve ingen") && remoteBoardText.Contains($"MCP release (id {remoteTag.Id})"), "remote get_board includes tag definitions and card tag ids");
Check(!remoteBoardText.Contains($"id {privateTag}: Private"), "remote get_board hides another board's tag definition");
await mcp.UpdateCard(boardId, remoteCard.Id, tagIds: []);
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Tags.Count == 0, "remote empty tagIds clears assignments");
await mcp.UpdateCard(boardId, remoteCard.Id, tagIds: [remoteTag.Id]);
await mcp.DeleteTag(boardId, remoteTag.Id);
Check((await Board(boardId)).Cards.Single(card => card.Id == remoteCard.Id).Tags.Count == 0, "remote tag deletion clears its assignments");
await mcp.DeleteCard(boardId, remoteCard.Id);

ActAs("friend");
Check((await Board(boardId)).Cards.Single().Placements.Single().BoardId == boardId, "shared card hides inaccessible board metadata");
Check(await controller.GetBoard(hiddenId) is ForbidResult, "shared card does not grant hidden board access");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Friend edit" }, boardId, cardId) is OkResult, "invited friend can edit cards");
Check((await Board(boardId)).Cards.Single().Color == null, "member can remove card color");
Check((await Board(boardId)).Cards.Single().Assignees.Count == 0, "empty assignee list clears assignments");
foreach (var paletteColor in (await Board(boardId)).Colors)
{
    Check(await controller.DeleteColor(boardId, paletteColor.Id) is OkResult, "ordinary member can remove palette color");
}
Check((await Board(boardId)).Colors.Count == 0, "board can keep an empty palette");
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    try
    {
        await new NpgsqlCommand("UPDATE ds28.work_item SET color = '#gg0000'", connection).ExecuteNonQueryAsync();
        throw new Exception("color constraint missing");
    }
    catch (PostgresException exception) when (exception.SqlState == "23514")
    {
        Console.WriteLine("PASS database rejects unsupported colors");
    }
}
var inlineCardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto
{
    Title = "Inline tags",
    ColumnId = columnId,
    TagIds = [releaseTag],
    NewTagNames = [" Draft release ", "DRAFT RELEASE", "V0.2.14"],
}, boardId)).Value;
var inlineBoard = await Board(boardId);
var inlineTag = inlineBoard.Tags.Single(tag => tag.Name == "Draft release");
Check(inlineTag.Color == null, "inline tags start without color");
Check(inlineBoard.Cards.Single(card => card.Id == inlineCardId).Tags.Count == 2, "inline creation trims names, deduplicates and reuses selected tags");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Inline updated", NewTagNames = ["Extra", "Private"] }, boardId, inlineCardId) is OkResult, "inline update appends names with omitted tagIds");
inlineBoard = await Board(boardId);
Check(inlineBoard.Cards.Single(card => card.Id == inlineCardId).Tags.Count == 4, "omitted tagIds preserves selection when adding names");
Check(inlineBoard.Tags.Single(tag => tag.Name == "Private").Id != privateTag, "inline creation never reuses another board's private tag");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Replace", TagIds = [], NewTagNames = ["DRAFT RELEASE"] }, boardId, inlineCardId) is OkResult, "inline names reuse existing tag case-insensitively");
Check((await Board(boardId)).Cards.Single(card => card.Id == inlineCardId).Tags.Single().Id == inlineTag.Id, "empty tagIds replaces selection with supplied names");
foreach (var invalidName in new[] { " ", new string('x', 65), null })
{
    Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Must not save", NewTagNames = ["Must not exist", invalidName] }, boardId, inlineCardId) is BadRequestObjectResult, "invalid inline name rejects entire update");
}
Check(!(await Board(boardId)).Tags.Any(tag => tag.Name == "Must not exist"), "validation failure leaves no unused tags");
Check((await Board(boardId)).Cards.Single(card => card.Id == inlineCardId).Title == "Replace", "validation failure preserves card title");
Check(await controller.CreateCard(new WorkItemCreateDto { Title = "Invalid inline", ColumnId = columnId, TagIds = [privateTag], NewTagNames = ["Must not exist"] }, boardId) is BadRequestObjectResult, "cross-board selection rejects inline creation before mutation");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared inline", TagIds = [], NewTagNames = ["Shared release"] }, boardId, cardId) is OkResult, "shared card accepts inline tags on current placement");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check((await Board(hiddenId)).Cards.Single().Tags.Single().Id == privateTag, "inline update preserves other board placement tags");
ActAs("outsider");
Check(await controller.CreateCard(new WorkItemCreateDto { Title = "Denied", ColumnId = columnId, NewTagNames = ["Denied tag"] }, boardId) is ForbidResult, "outsider cannot create inline tags with card");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Denied", NewTagNames = ["Denied tag"] }, boardId, inlineCardId) is ForbidResult, "outsider cannot append inline tags");
ActAs("friend");
var changesBeforeFailure = boardEvents.Changes.Count;
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await new NpgsqlCommand("""
        CREATE FUNCTION ds28.reject_inline_test() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF NEW.title = 'Force inline failure' THEN
                RAISE EXCEPTION 'forced card write failure' USING ERRCODE = '23505';
            END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER reject_inline_test BEFORE INSERT OR UPDATE ON ds28.work_item
        FOR EACH ROW EXECUTE FUNCTION ds28.reject_inline_test();
        """, connection).ExecuteNonQueryAsync();
}
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Force inline failure", TagIds = [], NewTagNames = ["Rollback update"] }, boardId, inlineCardId) is ConflictObjectResult, "database write failure returns conflict for inline update");
Check(await controller.CreateCard(new WorkItemCreateDto { Title = "Force inline failure", ColumnId = columnId, NewTagNames = ["Rollback create"] }, boardId) is ConflictObjectResult, "database write failure returns conflict for inline creation");
inlineBoard = await Board(boardId);
Check(!inlineBoard.Tags.Any(tag => tag.Name.StartsWith("Rollback")), "failed database writes roll back all newly inserted tags");
Check(inlineBoard.Cards.Single(card => card.Id == inlineCardId).Title == "Replace" && inlineBoard.Cards.Single(card => card.Id == inlineCardId).Tags.Single().Id == inlineTag.Id, "failed write rolls back title and tag replacement");
Check(!inlineBoard.Cards.Any(card => card.Title == "Force inline failure"), "failed create leaves no card");
Check(boardEvents.Changes.Count == changesBeforeFailure, "failed transactions publish no live update");
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await new NpgsqlCommand("DROP TRIGGER reject_inline_test ON ds28.work_item; DROP FUNCTION ds28.reject_inline_test()", connection).ExecuteNonQueryAsync();
}
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Retry succeeded", NewTagNames = ["Rollback update"] }, boardId, inlineCardId) is OkResult, "retry after rollback succeeds");
Check((await Board(boardId)).Tags.Count(tag => tag.Name == "Rollback update") == 1, "retry creates exactly one tag");

Check(await controller.DeleteBoard(boardId) is OkResult, "member can delete board");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "outsider" }, boardId) is ForbidResult, "deleted board rejects invitations");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = "Denied", Color = "#123456" }, boardId) is ForbidResult, "deleted board rejects palette changes");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(!(await Boards()).Any(board => board.Id == boardId), "deleted board is hidden even from administrators");
Console.WriteLine("ALL BOARD ACCESS CHECKS PASSED");

sealed class RecordingBoardEvents : IBoardEventPublisher
{
    public List<int> Changes { get; } = [];

    public Task BoardChanged(int boardId)
    {
        Changes.Add(boardId);
        return Task.CompletedTask;
    }
}


sealed class TestMcpAuthentication : IAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
    {
        if (scheme != McpAuthenticationHandler.SchemeName) throw new InvalidOperationException("Unexpected authentication scheme");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(context.User, scheme)));
    }

    public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
    public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) => Task.CompletedTask;
    public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
}
