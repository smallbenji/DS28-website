using DS.Domain;
using DS.Backend.Mcp;
using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol;
using DbUp;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using DS.Application.Services.Boards;
using DS.Application.Services.Email;
using DS.Application.Services.Notifications;
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
            Assignees = [new WorkItemAssigneeDto { FirstName = "Ada", LastName = "Lovelace" }],
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
services.Configure<DSSettings>(options =>
{
    options.NotificationsEnabled = true;
    options.PublicBaseUrl = "https://ds28.example.test";
});
services.AddSingleton<RecordingMailQueue>();
services.AddSingleton<IMailQueue>(provider => provider.GetRequiredService<RecordingMailQueue>());
services.AddScoped<EmailService>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
var mail = provider.GetRequiredService<RecordingMailQueue>();
Check((await db.WorkItems.SingleAsync(item => item.Title == "Existing uncolored card")).Color == null, "existing cards migrate without a color");
Check((await db.WorkItems.Include(item => item.Assignees).SingleAsync(item => item.Title == "Existing uncolored card")).Assignees.Single().UserId == "owner", "assignee migration preserves existing assignment without index name collision");
Check((await db.WorkItems.SingleAsync(item => item.Title == "Existing colored card")).Color == "#dbeafe", "board palette migration preserves existing card colors");
Check(await db.BoardColors.CountAsync() == 20, "existing boards receive ten default palette colors each");
foreach (var id in new[] { "member", "friend", "outsider", "admin" })
{
    Check((await manager.CreateAsync(new User { Id = id, UserName = id, FirstName = id, LastName = "Test", Email = $"{id}@example.test" })).Succeeded, $"create {id}");
}

var boardEvents = new RecordingBoardEvents();
var controller = new BoardApiController(new BoardService(db, manager, boardEvents, emailService), manager)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider } }
};
var mcp = new BoardTools(new BoardService(db, manager, boardEvents, emailService), new HttpContextAccessor { HttpContext = controller.HttpContext }, manager);
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
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#dbeafe" }, boardId, cardId) is OkResult, "card update without assignee list succeeds");
Check((await Board(boardId)).Cards.Single().Assignees.Single().Id == "friend", "card update without assignee list preserves assignees");
Check(await controller.AddAssignee(new WorkItemAssigneeCreateDto { UserId = "member" }, boardId, cardId) is OkResult, "single assignee can be added");
Check(await controller.AddAssignee(new WorkItemAssigneeCreateDto { UserId = "member" }, boardId, cardId) is OkResult, "adding an existing assignee is idempotent");
Check(await controller.AddAssignee(new WorkItemAssigneeCreateDto { UserId = "missing" }, boardId, cardId) is BadRequestObjectResult, "unknown single assignee is rejected");
Check((await Board(boardId)).Cards.Single().Assignees.Select(user => user.Id).Order().SequenceEqual(new[] { "friend", "member" }), "adding an assignee keeps the existing one");
Check(await controller.CompleteCard(boardId, cardId) is OkResult, "assignee can mark own part as done");
var completionAssignees = (await Board(boardId)).Cards.Single().Assignees;
Check(completionAssignees.Single(user => user.Id == "member").CompletedAt != null && completionAssignees.Single(user => user.Id == "friend").CompletedAt == null, "completion only affects the acting assignee");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Shared", Color = "#dbeafe", AssigneeUserIds = ["member", "friend"] }, boardId, cardId) is OkResult, "card update with unchanged assignees succeeds");
Check((await Board(boardId)).Cards.Single().Assignees.Single(user => user.Id == "member").CompletedAt != null, "card update keeps existing completion");
Check(await controller.ReopenCard(boardId, cardId) is OkResult, "assignee can withdraw own completion");
Check((await Board(boardId)).Cards.Single().Assignees.All(user => user.CompletedAt == null), "withdrawn completion is cleared");
Check(await controller.CompleteCard(boardId, cardId) is OkResult, "assignee can mark own part as done again");
Check(await controller.RemoveAssignee(boardId, cardId, "member") is OkResult, "single assignee can be removed");
Check(await controller.AddAssignee(new WorkItemAssigneeCreateDto { UserId = "member" }, boardId, cardId) is OkResult, "removed assignee can be added again");
Check((await Board(boardId)).Cards.Single().Assignees.Single(user => user.Id == "member").CompletedAt == null, "removing an assignee discards their completion");
Check(await controller.RemoveAssignee(boardId, cardId, "member") is OkResult, "assignee can be removed after re-adding");
Check(await controller.CompleteCard(boardId, cardId) is BadRequestObjectResult, "non-assignee cannot mark the card as done");
Check(await controller.ReopenCard(boardId, cardId) is BadRequestObjectResult, "non-assignee cannot withdraw a completion");
Check((await Board(boardId)).Cards.Single().Assignees.Single().Id == "friend", "single assignee operations leave the original assignee");

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
    ["add assignee"] = await controller.AddAssignee(new WorkItemAssigneeCreateDto { UserId = "outsider" }, boardId, cardId),
    ["remove assignee"] = await controller.RemoveAssignee(boardId, cardId, "friend"),
    ["complete card"] = await controller.CompleteCard(boardId, cardId),
    ["reopen card"] = await controller.ReopenCard(boardId, cardId),
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
await RejectMcp(() => mcp.ListBoards(), "MCP tool without direct role denied");
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
var mcpRole = new Role { Name = nameof(AppGroups.McpUser) };
Check((await roles.CreateAsync(mcpRole)).Succeeded, "create MCP role");
db.UserRoles.Add(new IdentityUserRole<string> { UserId = "owner", RoleId = mcpRole.Id });
await db.SaveChangesAsync();
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
await db.UserRoles.Where(role => role.UserId == "owner" && role.RoleId == mcpRole.Id).ExecuteDeleteAsync();
await RejectMcp(() => mcp.ListBoards(), "existing MCP session cannot call tools after role removal");


ActAs("friend");
Check((await Board(boardId)).Cards.Single().Placements.Single().BoardId == boardId, "shared card hides inaccessible board metadata");
Check(await controller.GetBoard(hiddenId) is ForbidResult, "shared card does not grant hidden board access");
Check(await controller.UpdateCard(new WorkItemUpdateDto { Title = "Friend edit", AssigneeUserIds = [] }, boardId, cardId) is OkResult, "invited friend can edit cards");
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

ActAs("owner");
var depsHiddenId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Deps hidden" })).Value;
var depsHiddenColumns = (await Board(depsHiddenId)).Columns;
var hiddenCardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Dep hidden", ColumnId = depsHiddenColumns.First().Id }, depsHiddenId)).Value;
var mcpCardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Dep MCP", ColumnId = depsHiddenColumns.First().Id }, depsHiddenId)).Value;
db.UserRoles.Add(new IdentityUserRole<string> { UserId = "owner", RoleId = mcpRole.Id });
await db.SaveChangesAsync();
const string firstPullRequest = "https://github.com/example/project/pull/10";
const string secondPullRequest = "https://github.com/example/project/pull/11";
var developerSettings = new BoardUpdateDto { Title = "Deps hidden", IsDeveloperBoard = true };
Check(!await db.Boards.AnyAsync(board => board.IsDeveloperBoard), "existing and new boards start with developer mode off");
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is ForbidResult, "board member cannot activate developer mode");
ActAs("admin", nameof(AppGroups.CampAdmin));
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is ForbidResult, "camp admin cannot activate developer mode");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is OkResult, "sysadmin can activate developer mode");

ActAs("outsider");
Check(await controller.AddPullRequest(new PullRequestLinkDto { Url = firstPullRequest }, depsHiddenId, mcpCardId) is ForbidResult, "outsider cannot add PR links");
ActAs("owner");
Check(await controller.UpdateBoard(new BoardUpdateDto { Title = "Deps hidden" }, depsHiddenId) is OkResult, "member can edit general settings on a developer board");
Check((await Board(depsHiddenId)).IsDeveloperBoard, "omitted setting preserves developer mode");
Check(await controller.AddPullRequest(new PullRequestLinkDto { Url = "https://github.com.example.org/example/project/pull/10" }, depsHiddenId, mcpCardId) is BadRequestObjectResult, "fake GitHub host rejected");
var eventsBeforePullRequest = boardEvents.Changes.Count;
await mcp.AddPullRequest(depsHiddenId, mcpCardId, firstPullRequest);
Check(boardEvents.Changes.Skip(eventsBeforePullRequest).SequenceEqual([depsHiddenId]), "MCP link updates only the current board");
await mcp.AddPullRequest(depsHiddenId, mcpCardId, secondPullRequest);
await RejectMcp(() => mcp.AddPullRequest(depsHiddenId, mcpCardId, "https://github.com/Example/Project/pull/11/"), "normalized duplicate PR link rejected");
var developerBoardText = await mcp.GetBoard(depsHiddenId);
Check(developerBoardText.Contains(firstPullRequest) && developerBoardText.Contains(secondPullRequest), "MCP shows all links on the card");

var otherBoardId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Other PR board" })).Value;
var otherColumn = (await Board(otherBoardId)).Columns.First().Id;
Check(await controller.AddCardPlacement(new WorkItemPlacementCreateDto { ColumnId = otherColumn }, otherBoardId, mcpCardId) is OkResult, "shared card added to second board");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(await controller.UpdateBoard(new BoardUpdateDto { Title = "Other PR board", IsDeveloperBoard = true }, otherBoardId) is OkResult, "second board has developer mode");
Check((await Board(otherBoardId)).Cards.Single().PullRequests.Count == 0, "PR links do not follow the card to another developer board");
developerSettings.IsDeveloperBoard = false;
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is OkResult, "sysadmin can disable developer mode");
ActAs("owner");
Check(!(await mcp.GetBoard(depsHiddenId)).Contains(firstPullRequest), "disabled board hides stored links from MCP");
await RejectMcp(() => mcp.RemovePullRequest(depsHiddenId, mcpCardId, firstPullRequest), "disabled board rejects link removal");
ActAs("admin", nameof(AppGroups.SysAdmin));
developerSettings.IsDeveloperBoard = true;
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is OkResult, "sysadmin can reactivate developer mode");
ActAs("owner");
Check((await Board(depsHiddenId)).Cards.Single(card => card.Id == mcpCardId).PullRequests.SequenceEqual([firstPullRequest, secondPullRequest]), "reactivation restores both links");
await mcp.RemovePullRequest(depsHiddenId, mcpCardId, firstPullRequest);
Check((await Board(depsHiddenId)).Cards.Single(card => card.Id == mcpCardId).PullRequests.SequenceEqual([secondPullRequest]), "MCP removes only the specified PR link");

var epicColumns = (await Board(depsHiddenId)).Columns;
var epicBacklog = epicColumns.First().Id;
var epicDone = epicColumns.Single(column => column.IsDoneColumn).Id;
async Task<int> CreateEpicTestCard(int targetBoardId, int targetColumnId, string title)
{
    return (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = title, ColumnId = targetColumnId }, targetBoardId)).Value;
}
WorkItemDto EpicCard(BoardDetailDto board, int id)
{
    return board.Cards.Single(card => card.Id == id);
}
var epicId = await CreateEpicTestCard(depsHiddenId, epicBacklog, "Epic samling");
var epicChildA = await CreateEpicTestCard(depsHiddenId, epicBacklog, "Epic del A");
var epicChildB = await CreateEpicTestCard(depsHiddenId, epicBacklog, "Epic del B");
var secondEpicId = await CreateEpicTestCard(depsHiddenId, epicBacklog, "Epic anden");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, epicChildA) is BadRequestObjectResult, "card cannot belong to a card that is not marked as epic");
boardEvents.Changes.Clear();
Check(await controller.MarkAsEpic(depsHiddenId, epicId) is OkResult, "member can mark a card as epic");
Check(boardEvents.Changes.SequenceEqual([depsHiddenId]), "marking an epic updates only the current board");
Check(await controller.MarkAsEpic(depsHiddenId, epicId) is OkResult, "marking an epic again is idempotent");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, epicChildA) is OkResult, "card can belong to an epic");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, epicChildB) is OkResult, "epic can have several cards");
var epicBoard = await Board(depsHiddenId);
Check(EpicCard(epicBoard, epicChildA).EpicCardId == epicId && !EpicCard(epicBoard, epicChildA).IsEpic && EpicCard(epicBoard, epicChildA).EpicProgress == null, "child card returns the DS number of its epic");
Check(EpicCard(epicBoard, epicId).IsEpic && EpicCard(epicBoard, epicId).EpicCardId == null && EpicCard(epicBoard, epicId).EpicProgress is { Done: 0, Total: 2 }, "epic progress counts its cards on the board");
Check(EpicCard(epicBoard, secondEpicId).EpicProgress == null && !EpicCard(epicBoard, secondEpicId).IsEpic, "ordinary cards have no epic data");
Check(await controller.MoveCard(new WorkItemMoveDto { ColumnId = epicDone }, depsHiddenId, epicChildA) is OkResult, "epic child can be moved to a done column");
Check(EpicCard(await Board(depsHiddenId), epicId).EpicProgress is { Done: 1, Total: 2 }, "children in a done column count as done");
Check(EpicCard(await Board(depsHiddenId), epicId).ColumnId == epicBacklog, "epic is not moved automatically");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, epicId) is BadRequestObjectResult, "card cannot be its own epic");
Check(await controller.MarkAsEpic(depsHiddenId, epicChildA) is BadRequestObjectResult, "card belonging to an epic cannot become an epic");
Check(await controller.MarkAsEpic(depsHiddenId, secondEpicId) is OkResult, "board can have several epics");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, secondEpicId) is BadRequestObjectResult, "epic cannot belong to another epic");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = 999999 }, depsHiddenId, epicChildB) is BadRequestObjectResult, "unknown epic is rejected");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = secondEpicId }, depsHiddenId, epicChildB) is OkResult, "card can move to another epic");
epicBoard = await Board(depsHiddenId);
Check(EpicCard(epicBoard, epicChildB).EpicCardId == secondEpicId && EpicCard(epicBoard, epicId).EpicProgress.Total == 1 && EpicCard(epicBoard, secondEpicId).EpicProgress.Total == 1, "card belongs to at most one epic");

var foreignEpicId = await CreateEpicTestCard(otherBoardId, otherColumn, "Epic fremmed");
Check(await controller.MarkAsEpic(otherBoardId, foreignEpicId) is OkResult, "epic can be marked on another developer board");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = foreignEpicId }, depsHiddenId, epicChildB) is BadRequestObjectResult, "epic on another board is rejected");
Check(await controller.MarkAsEpic(depsHiddenId, mcpCardId) is OkResult, "shared card can be marked as epic on one board");
Check(!EpicCard(await Board(otherBoardId), mcpCardId).IsEpic, "epic marking does not follow the card to another board");
Check(await controller.UnmarkAsEpic(depsHiddenId, mcpCardId) is OkResult, "shared card epic marking can be removed");

ActAs("outsider");
Check(await controller.MarkAsEpic(depsHiddenId, epicChildB) is ForbidResult, "outsider cannot mark epics");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = epicId }, depsHiddenId, epicChildB) is ForbidResult, "outsider cannot change a card's epic");
Check(await controller.RemoveCardEpic(depsHiddenId, epicChildB) is ForbidResult, "outsider cannot remove a card's epic");
ActAs("owner");
var plainBoardId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Almindelig tavle" })).Value;
var plainCardId = await CreateEpicTestCard(plainBoardId, (await Board(plainBoardId)).Columns.First().Id, "Epic almindelig");
Check(await controller.MarkAsEpic(plainBoardId, plainCardId) is ForbidResult, "epics require developer features");

ActAs("admin", nameof(AppGroups.SysAdmin));
developerSettings.IsDeveloperBoard = false;
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is OkResult, "sysadmin disables developer mode with epics");
ActAs("owner");
epicBoard = await Board(depsHiddenId);
Check(epicBoard.Cards.All(card => !card.IsEpic && card.EpicCardId == null && card.EpicProgress == null), "disabled board hides stored epic data");
Check(await controller.RemoveCardEpic(depsHiddenId, epicChildB) is ForbidResult, "disabled board rejects epic changes");
ActAs("admin", nameof(AppGroups.SysAdmin));
developerSettings.IsDeveloperBoard = true;
Check(await controller.UpdateBoard(developerSettings, depsHiddenId) is OkResult, "sysadmin reactivates developer mode with epics");
ActAs("owner");
Check(EpicCard(await Board(depsHiddenId), epicChildB).EpicCardId == secondEpicId, "reactivation restores epic data");

var epicPlacementId = await db.BoardCards.Where(card => card.BoardId == depsHiddenId && card.WorkItemId == epicId).Select(card => card.Id).SingleAsync();
boardEvents.Changes.Clear();
Check(await controller.UnmarkAsEpic(depsHiddenId, epicId) is OkResult, "epic marking can be removed");
Check(boardEvents.Changes.SequenceEqual([depsHiddenId]), "removing an epic marking updates the board");
Check(!await db.BoardCards.AnyAsync(card => card.EpicBoardCardId == epicPlacementId), "removing an epic marking releases its cards");
Check(EpicCard(await Board(depsHiddenId), epicChildA).EpicCardId == null, "released card no longer shows an epic");
Check(await controller.RemoveCardEpic(depsHiddenId, epicChildB) is OkResult, "card can leave its epic");
Check(EpicCard(await Board(depsHiddenId), secondEpicId).EpicProgress is { Done: 0, Total: 0 }, "epic without cards has empty progress");
Check(await controller.SetCardEpic(new WorkItemEpicLinkDto { EpicWorkItemId = secondEpicId }, depsHiddenId, epicChildB) is OkResult, "card can join the epic again");
var secondEpicPlacementId = await db.BoardCards.Where(card => card.BoardId == depsHiddenId && card.WorkItemId == secondEpicId).Select(card => card.Id).SingleAsync();
db.ChangeTracker.Clear();
Check(await controller.RemoveCard(depsHiddenId, secondEpicId) is OkResult, "epic card can be deleted");
Check(!await db.BoardCards.AnyAsync(card => card.EpicBoardCardId == secondEpicPlacementId), "deleting an epic releases its cards");
Check(EpicCard(await Board(depsHiddenId), epicChildB).EpicCardId == null, "card of a deleted epic no longer shows an epic");

Check(await mcp.AddDependency(depsHiddenId, mcpCardId, hiddenCardId) == $"DS-{mcpCardId} venter nu på DS-{hiddenCardId}.", "remote MCP adds a dependency");
var mcpBoardText = await mcp.GetBoard(depsHiddenId);
Check(mcpBoardText.Contains($"venter på: DS-{hiddenCardId} Dep hidden (ikke færdig)") && mcpBoardText.Contains($"blokerer: DS-{mcpCardId} Dep MCP (ikke færdig)"), "remote MCP board text shows waiting and blocking cards");
Check((await mcp.SearchCards($"DS-{hiddenCardId}")).StartsWith($"- DS-{hiddenCardId} Dep hidden | Deps hidden (id {depsHiddenId})"), "remote MCP searches cards by number");
await RejectMcp(() => mcp.AddDependency(depsHiddenId, hiddenCardId, mcpCardId), "remote MCP rejects dependency cycles");
await RejectMcp(() => mcp.AddDependency(depsHiddenId, mcpCardId, mcpCardId), "remote MCP rejects self-dependency");
Check(await mcp.RemoveDependency(depsHiddenId, hiddenCardId, mcpCardId) == $"Fjernede afhængigheden mellem DS-{hiddenCardId} og DS-{mcpCardId}.", "remote MCP removes a dependency from the blocking side");
await RejectMcp(() => mcp.RemoveDependency(depsHiddenId, hiddenCardId, mcpCardId), "remote MCP reports a missing dependency");
await db.UserRoles.Where(role => role.UserId == "owner" && role.RoleId == mcpRole.Id).ExecuteDeleteAsync();
ActAs("friend");
var depsId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Deps" })).Value;
var depsColumns = (await Board(depsId)).Columns;
var depsBacklog = depsColumns.First().Id;
var depsDone = depsColumns.Single(column => column.IsDoneColumn).Id;
var depA = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Dep A", ColumnId = depsBacklog }, depsId)).Value;
var depB = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Dep B", ColumnId = depsBacklog }, depsId)).Value;
var depC = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Dep C", ColumnId = depsBacklog }, depsId)).Value;
WorkItemDto DepCard(BoardDetailDto board, int id)
{
    return board.Cards.Single(card => card.Id == id);
}

Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depB }, depsId, depA) is OkResult, "member can make a card wait on another card");
var depsBoard = await Board(depsId);
var waitingOnB = DepCard(depsBoard, depA).WaitingOn.Single();
Check(waitingOnB.Id == depB && waitingOnB.IsVisible && waitingOnB.Title == "Dep B" && !waitingOnB.IsDone, "waiting card lists visible unfinished dependency");
Check(DepCard(depsBoard, depB).Blocking.Single().Id == depA, "dependency lists the card it blocks");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depB }, depsId, depA) is ConflictObjectResult, "duplicate dependency rejected");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depA }, depsId, depA) is BadRequestObjectResult, "card cannot wait on itself");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depA }, depsId, depB) is BadRequestObjectResult, "direct dependency cycle rejected");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depC }, depsId, depB) is OkResult, "dependency chain can be extended");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depA }, depsId, depC) is BadRequestObjectResult, "indirect dependency cycle rejected");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = hiddenCardId }, depsId, depA) is BadRequestObjectResult, "cannot depend on a card the member cannot see");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = 999999 }, depsId, depA) is BadRequestObjectResult, "unknown card is reported like a hidden card");

var foundById = (List<WorkItemSearchResultDto>)((OkObjectResult)await controller.SearchCards($"ds-{depB}")).Value;
Check(foundById.First().Id == depB, "card search finds DS number first");
Check(((List<WorkItemSearchResultDto>)((OkObjectResult)await controller.SearchCards("Dep hidden")).Value).Count == 0, "card search excludes cards on boards the member cannot see");

ActAs("admin", nameof(AppGroups.SysAdmin));
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = hiddenCardId }, depsId, depA) is OkResult, "administrator can depend on a card on another board");
ActAs("friend");
var hiddenDependency = DepCard(await Board(depsId), depA).WaitingOn.Single(dependency => dependency.Id == hiddenCardId);
Check(!hiddenDependency.IsVisible && hiddenDependency.Title == null && hiddenDependency.Placements.Count == 0 && !hiddenDependency.IsDone, "hidden dependency exposes only its number and done state");

Check(await controller.MoveCard(new WorkItemMoveDto { ColumnId = depsDone }, depsId, depB) is OkResult, "dependency can be moved to a done column");
Check(DepCard(await Board(depsId), depA).WaitingOn.Single(dependency => dependency.Id == depB).IsDone, "dependency in done column counts as done");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(await controller.AddCardPlacement(new WorkItemPlacementCreateDto { ColumnId = depsHiddenColumns.First().Id }, depsHiddenId, depB) is OkResult, "dependency can be shared to another board");
ActAs("friend");
Check(!DepCard(await Board(depsId), depA).WaitingOn.Single(dependency => dependency.Id == depB).IsDone, "dependency is unfinished while any placement, even hidden, is unfinished");

ActAs("admin", nameof(AppGroups.SysAdmin));
boardEvents.Changes.Clear();
Check(await controller.MoveCard(new WorkItemMoveDto { ColumnId = depsHiddenColumns.Single(column => column.IsDoneColumn).Id }, depsHiddenId, hiddenCardId) is OkResult, "hidden dependency can be completed");
Check(boardEvents.Changes.Contains(depsId), "completing a dependency notifies boards with waiting cards");
ActAs("friend");
Check(DepCard(await Board(depsId), depA).WaitingOn.Single(dependency => dependency.Id == hiddenCardId).IsDone, "completed hidden dependency counts as done");

ActAs("outsider");
Check(await controller.AddDependency(new WorkItemDependencyCreateDto { DependsOnWorkItemId = depC }, depsId, depA) is ForbidResult, "outsider cannot add dependencies");
Check(await controller.RemoveDependency(depsId, depA, depB) is ForbidResult, "outsider cannot remove dependencies");
Check(((List<WorkItemSearchResultDto>)((OkObjectResult)await controller.SearchCards("Dep")).Value).Count == 0, "outsider search finds no cards");

ActAs("friend");
Check(await controller.RemoveDependency(depsId, depA, hiddenCardId) is OkResult, "member can remove a dependency on a hidden card");
Check(await controller.RemoveDependency(depsId, depB, depA) is OkResult, "dependency can be removed from the blocking side");
Check(DepCard(await Board(depsId), depA).WaitingOn.Count == 0, "removed dependencies disappear");
Check(await controller.RemoveDependency(depsId, depA, depB) is NotFoundResult, "removing a missing dependency returns not found");
Check(await controller.RemoveCard(depsId, depC) is OkResult, "dependency card can be deleted");
Check(!await db.WorkItemDependencies.AnyAsync(dependency => dependency.WorkItemId == depC || dependency.DependsOnWorkItemId == depC), "deleting a card removes its dependencies");
Check(DepCard(await Board(depsId), depB).WaitingOn.Count == 0, "deleted dependency no longer blocks");

ActAs("owner");
var membersBoardId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Members" })).Value;
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, membersBoardId) is OkResult, "owner invites member to member board");
var membersColumnId = (await Board(membersBoardId)).Columns.First().Id;
var assignedCardId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Assigned", ColumnId = membersColumnId, AssigneeUserIds = ["member"] }, membersBoardId)).Value;
await controller.CreateCard(new WorkItemCreateDto { Title = "Also assigned", ColumnId = membersColumnId, AssigneeUserIds = ["member", "owner"] }, membersBoardId);
var deletedAssignedId = (int)((OkObjectResult)await controller.CreateCard(new WorkItemCreateDto { Title = "Deleted assigned", ColumnId = membersColumnId, AssigneeUserIds = ["member"] }, membersBoardId)).Value;
Check(await controller.RemoveCard(membersBoardId, deletedAssignedId) is OkResult, "assigned card can be deleted");
List<BoardMemberDto> Members(IActionResult result)
{
    return (List<BoardMemberDto>)((OkObjectResult)result).Value;
}
var membersWithCounts = Members(await controller.GetMembers(membersBoardId));
Check(membersWithCounts.Single(member => member.Id == "member").AssignedCardCount == 2, "member list counts assigned cards on the board, excluding deleted cards");
Check(membersWithCounts.Single(member => member.Id == "owner").AssignedCardCount == 1, "member list counts each member's own assignments");
Check(Members(await controller.SearchAssignees("member", membersBoardId)).All(user => user.AssignedCardCount == null), "assignee search does not include card counts");

ActAs("outsider");
Check(await controller.RemoveMember(membersBoardId, "member") is ForbidResult, "outsider cannot remove members");

ActAs("owner");
boardEvents.Changes.Clear();
Check(await controller.RemoveMember(membersBoardId, "friend") is NotFoundResult, "removing a non-member returns not found");
Check(await controller.RemoveMember(membersBoardId, "member") is OkResult, "member can remove another member");
Check(!await db.BoardMembers.AnyAsync(member => member.BoardId == membersBoardId && member.UserId == "member"), "removed membership is deleted");
Check(boardEvents.Changes.Contains(membersBoardId), "removing a member notifies the board");
Check((await db.WorkItems.Include(item => item.Assignees).SingleAsync(item => item.Id == assignedCardId)).Assignees.Any(assignee => assignee.UserId == "member"), "removed member keeps card assignments");
Check(await controller.RemoveMember(membersBoardId, "owner") is ConflictObjectResult, "last member cannot leave the board");
Check(await db.BoardMembers.AnyAsync(member => member.BoardId == membersBoardId && member.UserId == "owner"), "last membership is kept");

ActAs("member");
Check(await controller.GetMembers(membersBoardId) is ForbidResult, "removed member loses access");

ActAs("owner");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "member" }, membersBoardId) is OkResult, "owner invites member again");
Check(await controller.RemoveMember(membersBoardId, "owner") is OkResult, "member can leave when another member remains");
Check(await controller.GetMembers(membersBoardId) is ForbidResult, "member who left loses access");

ActAs("admin", nameof(AppGroups.SysAdmin));
Check(await controller.RemoveMember(membersBoardId, "member") is ConflictObjectResult, "administrator cannot remove the last member either");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "friend" }, membersBoardId) is OkResult, "administrator can invite without being a member");
Check(await controller.RemoveMember(membersBoardId, "member") is OkResult, "administrator can remove a member without being one");

ActAs("friend");
Check(await controller.DeleteBoard(boardId) is OkResult, "member can delete board");
Check(await controller.AddMember(new BoardMemberCreateDto { UserId = "outsider" }, boardId) is ForbidResult, "deleted board rejects invitations");
Check(await controller.CreateColor(new BoardColorUpdateDto { Name = "Denied", Color = "#123456" }, boardId) is ForbidResult, "deleted board rejects palette changes");
ActAs("admin", nameof(AppGroups.SysAdmin));
Check(!(await Boards()).Any(board => board.Id == boardId), "deleted board is hidden even from administrators");
Check(NotificationPermissions.AllowedGroups(NotificationType.BoardCreated).SequenceEqual([nameof(AppGroups.SysAdmin), nameof(AppGroups.CampAdmin)]), "board notifications are limited to groups that see all boards");
Check(!NotificationPermissions.CanSubscribe(AppAccess.ResolveAppRoles([nameof(AppGroups.EventAdmin)]), NotificationType.BoardCreated), "event admin cannot subscribe to board notifications");
var campAdminRole = new Role { Name = nameof(AppGroups.CampAdmin) };
Check((await roles.CreateAsync(campAdminRole)).Succeeded, "create camp admin role");
foreach (var id in new[] { "chief", "former" })
{
    Check((await manager.CreateAsync(new User { Id = id, UserName = id, FirstName = id, LastName = "Test", Email = $"{id}@example.test" })).Succeeded, $"create {id}");
}
db.UserRoles.Add(new IdentityUserRole<string> { UserId = "chief", RoleId = campAdminRole.Id });
db.NotificationPreferences.Add(new UserNotificationPreference { UserId = "chief", NotificationType = NotificationType.BoardCreated });
db.NotificationPreferences.Add(new UserNotificationPreference { UserId = "former", NotificationType = NotificationType.BoardCreated });
await db.SaveChangesAsync();
mail.Messages.Clear();
ActAs("friend");
var notifiedBoardId = (int)((OkObjectResult)await controller.CreateBoard(new BoardCreateDto { Title = "Notifikationstavle" })).Value;
Check(mail.Messages.Count == 1 && mail.Messages[0].Email == "chief@example.test", "new board notifies subscribed camp admins only");
Check(mail.Messages[0].Body.Contains("Notifikationstavle") && mail.Messages[0].Body.Contains("friend Test") && mail.Messages[0].Body.Contains($"https://ds28.example.test/board/{notifiedBoardId}"), "board notification contains title, creator and link");
mail.Messages.Clear();
ActAs("chief", nameof(AppGroups.CampAdmin));
Check(await controller.CreateBoard(new BoardCreateDto { Title = "Egen tavle" }) is OkObjectResult && mail.Messages.Count == 0, "board creator is not notified about own board");
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


sealed class RecordingMailQueue : IMailQueue
{
    public List<(string Email, string Subject, string Body)> Messages { get; } = [];

    public void Enqueue(string toEmail, string toName, string subject, string body)
    {
        Messages.Add((toEmail, subject, body));
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
