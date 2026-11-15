using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using DS.Application.Services;
using DS.Application.Services.Groups;
using DS.Backend.Controllers.ApiControllers;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Infrastructure;
using DS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}

var services = new ServiceCollection();
services.AddLogging();
services.AddAuthorization();
services.AddDbContext<DataDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
services.AddDsIdentityCore();
using var provider = services.BuildServiceProvider();
var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
var authorization = provider.GetRequiredService<IAuthorizationService>();
var attributes = typeof(GroupsApiController).GetCustomAttributes<AuthorizeAttribute>()
    .Concat(typeof(GroupsApiController).GetMethod(nameof(GroupsApiController.RegistrationHistory))!
        .GetCustomAttributes<AuthorizeAttribute>());
var policy = await AuthorizationPolicy.CombineAsync(policyProvider, attributes);

ClaimsPrincipal User(params string[] roles)
{
    return new ClaimsPrincipal(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "test"));
}

Check(!(await authorization.AuthorizeAsync(User(), null, policy!)).Succeeded, "Group member cannot read history");
Check(!(await authorization.AuthorizeAsync(User(nameof(AppRoles.GroupsView)), null, policy!)).Succeeded, "GroupsView alone cannot read history");
Check(!(await authorization.AuthorizeAsync(User(nameof(AppRoles.AdminAccess)), null, policy!)).Succeeded, "AdminAccess alone cannot read history");
Check((await authorization.AuthorizeAsync(User(nameof(AppRoles.GroupsView), nameof(AppRoles.AdminAccess)), null, policy!)).Succeeded, "Staff with both permissions can read history");

using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
db.Groups.AddRange(new Group { Id = 1, Name = "One" }, new Group { Id = 2, Name = "Two" });
db.GroupPreSignups.Add(new GroupPreSignup { GroupId = 1, Junior = 12 });
db.HistoricPresignups.AddRange(
    new HistoricPresignup { GroupId = 1, CampYear = 2024, Category = "junior", ParticipantCount = 7, GuestCount = 2, SourceDocumentId = "private-source", SourceSha256 = "private-hash" },
    new HistoricPresignup { GroupId = 2, CampYear = 2024, Category = "junior", ParticipantCount = 99, SourceDocumentId = "other", SourceSha256 = "hash" },
    new HistoricPresignup { GroupId = 1, CampYear = 2028, Category = "junior", ParticipantCount = 88, SourceDocumentId = "future", SourceSha256 = "hash" });
db.HistoricSignups.AddRange(
    new HistoricSignup { GroupId = 1, CampYear = 2024, Category = "leader", DayIndex = 0, ParticipantCount = 3, SourceDocumentId = "private-source", SourceSha256 = "private-hash" },
    new HistoricSignup { GroupId = 1, CampYear = 2024, Category = "junior", DayIndex = 0, ParticipantCount = 7, SourceDocumentId = "private-source", SourceSha256 = "private-hash" },
    new HistoricSignup { GroupId = 1, CampYear = 2024, Category = "junior", DayIndex = 1, ParticipantCount = 0, SourceDocumentId = "private-source", SourceSha256 = "private-hash" },
    new HistoricSignup { GroupId = 2, CampYear = 2024, Category = "junior", DayIndex = 0, ParticipantCount = 99, SourceDocumentId = "other", SourceSha256 = "hash" },
    new HistoricSignup { GroupId = 1, CampYear = 2028, Category = "junior", DayIndex = 0, ParticipantCount = 88, SourceDocumentId = "future", SourceSha256 = "hash" });
await db.SaveChangesAsync();
var service = new GroupAdministrationService(db, null);
var result = await service.GetRegistrationHistory(1);
Check(result.Outcome == ServiceOutcome.Success && result.Value.Presignups.Count == 1, "Presignups scoped to selected group and 2024");
Check(result.Value.CurrentPresignup?.Junior == 12, "Current counts returned separately from history");
Check(result.Value.Presignups[0].GuestCount == 2, "Guest counts stay separate");
Check(result.Value.Signups.Count == 3 && result.Value.Signups.All(row => row.CampYear == 2024 && row.ParticipantCount != 99 && row.ParticipantCount != 88), "Signups scoped to selected group and 2024");
Check(result.Value.Signups.Single(row => row.Category == "junior" && row.DayIndex == 1).ParticipantCount == 0, "Recorded zero stays zero");
Check(!result.Value.Signups.Any(row => row.Category == "leader" && row.DayIndex > 0), "Missing leader attendance stays unknown");
Check(!JsonSerializer.Serialize(result.Value).Contains("private-"), "Source metadata is not returned");
Check((await service.GetRegistrationHistory(999)).Outcome == ServiceOutcome.NotFound, "Unknown group returns not found");
db.Groups.Add(new Group { Id = 3, Name = "Empty" });
await db.SaveChangesAsync();
var empty = await service.GetRegistrationHistory(3);
Check(empty.Outcome == ServiceOutcome.Success && empty.Value.Signups.Count == 0 && empty.Value.Presignups.Count == 0 && empty.Value.CurrentPresignup == null, "Group without history returns empty lists");
var publicGroup = JsonSerializer.Serialize(new GroupDto(await db.Groups.FindAsync(1)));
Check(!publicGroup.Contains("Historic", StringComparison.OrdinalIgnoreCase) && !publicGroup.Contains("2024"), "Shared group response contains no history or history flags");
Console.WriteLine("All group history checks passed.");
