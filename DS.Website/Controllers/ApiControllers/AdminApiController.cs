using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.AdminAccess))]
    [Route("api/v1/admin")]
    public class AdminApiController(DataDbContext dataDb) : Controller
    {
        private static List<AdminSectionDto> GetSections()
        {
            return
            [
                new()
                {
                    Title = "Konti og grupper",
                    Icon = "users-gear",
                    Entries =
                    [
                        new HQPanelEntryDto
                        {
                            Title = "Brugerstyring",
                            Url = "/user",
                            Icon = ["user-pen"],
                            RequiredRole = nameof(AppRoles.UsersView)
                        },
                        new HQPanelEntryDto
                        {
                            Title = "Gruppestyring",
                            Url = "/groups",
                            Icon = ["users-gear"],
                            RequiredRole = nameof(AppRoles.GroupsView)
                        }
                    ]
                },
                new()
                {
                    Title = "Tilmelding",
                    Icon = "sliders",
                    Entries =
                    [
                        new HQPanelEntryDto
                        {
                            Title = "Lejrindstillinger",
                            Url = "/camp-settings",
                            Icon = ["sliders"],
                            RequiredRole = nameof(AppRoles.PreSignupManage)
                        }
                    ]
                },
                new()
                {
                    Title = "System",
                    Icon = "gear",
                    Entries =
                    [
                        new HQPanelEntryDto
                        {
                            Title = "Mailkø",
                            Url = "/email-outbox",
                            Icon = ["inbox"],
                            RequiredRole = nameof(AppRoles.EmailOutboxView)
                        }
                    ]
                }
            ];
        }

        [HttpGet]
        public IActionResult Index()
        {
            var sections = GetSections()
                .Select(s => new AdminSectionDto
                {
                    Title = s.Title,
                    Icon = s.Icon,
                    Entries = s.Entries
                        .Where(e => (e.RequiredRole == null || User.IsInRole(e.RequiredRole))
                            && (e.RequiredRoles == null || e.RequiredRoles.Length == 0
                                || e.RequiredRoles.Any(User.IsInRole)))
                        .ToList()
                })
                .Where(s => s.Entries.Count > 0)
                .ToList();

            return Ok(new AdminViewModelDto
            {
                Sections = sections
            });
        }

        [HttpGet("signup-progress")]
        public async Task<IActionResult> GetSignupProgress()
        {
            var target = ParticipantData.Stats.TotalUniqueParticipants;

            var preSignup = await dataDb.GroupPreSignups
                .Select(m => (int?)(m.Beaver + m.Wolf + m.Junior + m.Trop + m.Senior + m.Rover + m.Leader))
                .SumAsync() ?? 0;

            var finalSignup = await dataDb.Scouts.CountAsync();

            return Ok(new List<SignupProgressDto>
            {
                new()
                {
                    Key = "pre-signup",
                    Label = "Forhåndstilmelding",
                    Current = preSignup,
                    Target = target,
                    Status = StatusFor(preSignup, target)
                },
                new()
                {
                    Key = "final-signup",
                    Label = "Endelig tilmelding",
                    Current = finalSignup,
                    Target = target,
                    Status = StatusFor(finalSignup, target)
                }
            });
        }

        private static string StatusFor(int current, int target)
        {
            if (target <= 0) return "reached";
            if (current >= target) return "reached";
            if (current * 2 >= target) return "halfway";

            return "behind";
        }
    }
}
