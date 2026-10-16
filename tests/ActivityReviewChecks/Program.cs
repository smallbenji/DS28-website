using DbUp;
using DS.Data;
using DS.DTOs;
using DS.Models;
using DS.Website;
using DS.Website.Controllers;
using DS.Website.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using System.Security.Claims;

var connectionString = Environment.GetEnvironmentVariable("ACTIVITY_REVIEW_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || new NpgsqlConnectionStringBuilder(connectionString).Database != "ds28_activity_review_tests")
{
    throw new InvalidOperationException("Use run.sh to start a disposable ds28_activity_review_tests database.");
}

var assembly = typeof(DataDbContext).Assembly;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine($"PASS {message}"); }
var initial = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions")
    .WithScriptsEmbeddedInAssembly(assembly, name => string.CompareOrdinal(name, "DS.Migrations.021_activity_review.sql") < 0)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(initial.Successful, $"pre-review migrations: {initial.Error?.Message}");
await using (var conn = new NpgsqlConnection(connectionString)) {
    await conn.OpenAsync();
    await new NpgsqlCommand("INSERT INTO ds28.activity_team(name) VALUES ('Test team'); INSERT INTO ds28.activity(name, activity_team_id) VALUES ('Existing activity', 1);", conn).ExecuteNonQueryAsync();
}
var upgrade = DeployChanges.To.PostgresqlDatabase(connectionString, "ds28")
    .JournalToPostgresqlTable("ds28", "schemaversions").WithScriptsEmbeddedInAssembly(assembly)
    .WithVariablesDisabled().WithTransaction().LogToNowhere().Build().PerformUpgrade();
Check(upgrade.Successful, $"review migration: {upgrade.Error?.Message}");
var identityServices = new ServiceCollection();
identityServices.Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
using var identityProvider = identityServices.BuildServiceProvider();
var options = new DbContextOptionsBuilder<DataDbContext>().UseNpgsql(connectionString).UseSnakeCaseNamingConvention().UseOpenIddict().UseApplicationServiceProvider(identityProvider).Options;
DataDbContext Db() => new(options);
async Task<Activity> Load(int id) { await using var db = Db(); return await new ActivityRepository(db).GetActivityAsync(id); }
async Task<ActivityChangeResult> Review(int id, int version, ActivityReviewAction action, string feedback = null) {
    await using var db = Db(); return await new ActivityRepository(db).ReviewActivityAsync(id, version, action, "reviewer", feedback);
}
async Task<ActivityChangeResult> Update(int id, int version, string name, CatalogDataDto catalog) {
    await using var db = Db(); return await new ActivityRepository(db).UpdateActivityAsync(id, new ActivityUpdateDto { Name = name, Catalog = catalog, ReviewVersion = version }, "editor");
}
var old = await Load(1);
Check(old.ReviewStatus == ActivityReviewStatus.Draft && old.ReviewVersion == 0, "existing activities migrate as drafts");
await using (var db = Db()) {
    db.Users.AddRange(new User { Id = "reviewer", UserName = "reviewer", FirstName = "Review", LastName = "User" }, new User { Id = "editor", UserName = "editor" }, new User { Id = "reader", UserName = "reader" }, new User { Id = "outsider", UserName = "outsider" });
    db.ActivityTeamMemberships.AddRange(new ActivityTeamMembership { UserId = "editor", ActivityTeamId = 1, IsAdmin = true }, new ActivityTeamMembership { UserId = "reader", ActivityTeamId = 1, IsAdmin = false });
    db.ActivityBudgets.Add(new ActivityBudget { ActivityId = 1, Budget = 1500 });
    await db.SaveChangesAsync();
}
Check(await Review(1, 0, ActivityReviewAction.Submit) == ActivityChangeResult.Incomplete, "incomplete activity cannot be submitted");
var catalog = new CatalogDataDto { Name = "Catalog name", Summary = "Summary", Description = "Description" };
Check(await Update(1, 0, "Complete activity", catalog) == ActivityChangeResult.Success, "save complete content");
Check((await Load(1)).Budget.Budget == 1500, "content editing preserves allocated budget");
Check(await Review(1, 1, ActivityReviewAction.Approve) == ActivityChangeResult.InvalidState, "draft cannot be approved directly");
Check(await Review(1, 1, ActivityReviewAction.Submit) == ActivityChangeResult.Success, "submit complete draft");
Check(await Review(1, 1, ActivityReviewAction.Approve) == ActivityChangeResult.Conflict, "stale decision rejected");
Check(await Review(1, 2, ActivityReviewAction.RequestChanges, "  ") == ActivityChangeResult.InvalidFeedback, "return requires a reason");
Check(await Review(1, 2, ActivityReviewAction.RequestChanges, new string('x', 4001)) == ActivityChangeResult.InvalidFeedback, "return reason is length-limited");
Check(await Review(1, 2, ActivityReviewAction.RequestChanges, " Add detail ") == ActivityChangeResult.Success, "send back with feedback");
var returned = await Load(1);
Check(returned.ReviewStatus == ActivityReviewStatus.ChangesRequested && returned.ReviewFeedback == "Add detail" && returned.ReviewedBy.GetFullName() == "Review User" && returned.ReviewedAt != null, "latest feedback, reviewer and timestamp persisted");
Check(await Update(1, 3, "Revised activity", catalog) == ActivityChangeResult.Success, "edit returned activity");
Check((await Load(1)).ReviewStatus == ActivityReviewStatus.ChangesRequested, "editing returned activity preserves request for changes");
Check(await Review(1, 4, ActivityReviewAction.Submit) == ActivityChangeResult.Success, "resubmit after correction");
Check((await Load(1)).ReviewFeedback == "Add detail", "feedback remains available during resubmission");
Check(await Review(1, 5, ActivityReviewAction.Approve) == ActivityChangeResult.Success, "approve submitted activity");
Check((await Load(1)).ReviewFeedback == null, "approval clears obsolete requested-change feedback");
Check(await Update(1, 6, "Revised activity", catalog) == ActivityChangeResult.Success && (await Load(1)).ReviewStatus == ActivityReviewStatus.Approved, "saving identical content preserves approval");
Check(await Update(1, 6, "Changed after approval", catalog) == ActivityChangeResult.Success, "edit approved activity");
var changed = await Load(1);
Check(changed.ReviewStatus == ActivityReviewStatus.Draft && changed.SubmittedAt == null, "editing approved content requires new submission");
Check(await Review(1, 7, ActivityReviewAction.Submit) == ActivityChangeResult.Success, "submit again");
Check(await Update(1, 8, "Edited during review", catalog) == ActivityChangeResult.Success && (await Load(1)).ReviewStatus == ActivityReviewStatus.Draft, "editing pending content removes it from review queue");
Check(await Review(1, 8, ActivityReviewAction.Approve) == ActivityChangeResult.Conflict, "old reviewer cannot approve changed content");
await using (var a = Db()) await using (var b = Db()) {
    var first = await a.Activities.FindAsync(1);
    var second = await b.Activities.FindAsync(1);
    first.ReviewVersion++; second.ReviewVersion++;
    await a.SaveChangesAsync();
    try { await b.SaveChangesAsync(); throw new Exception("concurrency token not enforced"); }
    catch (DbUpdateConcurrencyException) { Console.WriteLine("PASS database concurrency token rejects simultaneous writes"); }
}
await using (var db = Db()) {
    var rows = await new ActivityRepository(db).GetAllActivityTeamsAsync();
    var dto = new ActivityTeamDto(rows.Single());
    Check(dto.Activities.Single().ReviewMissingFields.Count == 0 && dto.Activities.Single().Budget == 1500, "admin list includes completeness, review and budget data");
}
var services = new ServiceCollection();
services.AddLogging(); services.AddAuthorization();
services.AddDbContext<DataDbContext>(builder => builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
services.AddIdentityCore<User>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).AddRoles<Role>().AddEntityFrameworkStores<DataDbContext>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
ClaimsPrincipal Principal(string id, bool admin = false) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id) }.Concat(admin ? new[] { new Claim(ClaimTypes.Role, nameof(AppRoles.ActivityAdmin)), new Claim(ClaimTypes.Role, nameof(AppRoles.ActivityView)) } : new[] { new Claim(ClaimTypes.Role, nameof(AppRoles.ActivityView)) }), "test"));
await using (var db = Db()) {
    var controller = new ActivityApiController(new ActivityRepository(db), manager, null, null, NullLogger<ActivityApiController>.Instance) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal("reader") } } };
    var read = (OkObjectResult)await controller.GetActivity(1);
    Check(!((ActivityDto)read.Value).CanEdit, "ordinary team member has read-only detail access");
    Check(await controller.SubmitActivity(new ActivityReviewRequestDto { ReviewVersion = 10 }, 1) is ForbidResult, "read-only member cannot submit");
    controller.ControllerContext.HttpContext.User = Principal("outsider");
    Check(await controller.SubmitActivity(new ActivityReviewRequestDto { ReviewVersion = 10 }, 1) is ForbidResult, "unrelated team cannot submit");
    controller.ControllerContext.HttpContext.User = Principal("editor");
    var edit = (OkObjectResult)await controller.GetActivity(1);
    Check(((ActivityDto)edit.Value).CanEdit, "existing team editor can edit and submit");
    controller.ControllerContext.HttpContext.User = Principal("reviewer", true);
    var adminRead = (OkObjectResult)await controller.GetActivity(1);
    Check(((ActivityDto)adminRead.Value).CanEdit, "global activity administrator can review any team");
}
var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
var policyProvider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
foreach (var name in new[] { "ApproveActivity", "RequestActivityChanges" }) {
    var attrs = typeof(ActivityApiController).GetCustomAttributes(true).OfType<IAuthorizeData>().Concat(typeof(ActivityApiController).GetMethod(name).GetCustomAttributes(true).OfType<IAuthorizeData>());
    var policy = await AuthorizationPolicy.CombineAsync(policyProvider, attrs);
    Check(!(await authorization.AuthorizeAsync(Principal("editor"), null, policy)).Succeeded, $"{name} policy denies team editor");
    Check((await authorization.AuthorizeAsync(Principal("reviewer", true), null, policy)).Succeeded, $"{name} policy permits activity admin");
}
await using (var conn = new NpgsqlConnection(connectionString)) {
    await conn.OpenAsync();
    try { await new NpgsqlCommand("UPDATE ds28.activity SET review_status = 'Published'", conn).ExecuteNonQueryAsync(); throw new Exception("status constraint missing"); }
    catch (PostgresException ex) when (ex.SqlState == "23514") { Console.WriteLine("PASS database constrains allowed review statuses"); }
}
Console.WriteLine("ALL ACTIVITY REVIEW CHECKS PASSED");
