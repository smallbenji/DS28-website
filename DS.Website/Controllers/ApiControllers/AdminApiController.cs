using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DS.Domain;
using DS.Application.Services.Groups;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.AdminAccess))]
    [Route("api/v1/admin")]
    public class AdminApiController(GroupInfoService groupInfoService) : Controller
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
                            Title = "Dataudtræk",
                            Url = "/exports",
                            Icon = ["file-arrow-down"],
                            RequiredRole = nameof(AppRoles.ExportsView)
                        },
                        new HQPanelEntryDto
                        {
                            Title = "Hangfire",
                            Url = "/hangfire",
                            Icon = ["fa", "h"],
                            Mvc = true,
                            RequiredRole = nameof(AppRoles.HangfireView)
                        },
                        new HQPanelEntryDto
                        {
                            Title = "Google Drev",
                            Url = "/admin/google",
                            Icon = ["globe"],
                            RequiredRole = nameof(AppRoles.AdminAccess)
                        },
                        new HQPanelEntryDto
                        {
                            Title = "OpenIddict-klienter",
                            Url = "/admin/openiddict",
                            Icon = ["gear"],
                            RequiredRole = nameof(AppRoles.OpenIddictManage)
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

            var preSignup = await groupInfoService.GetTotalPresignup();

            var finalSignup = await groupInfoService.GetTotalSignups();

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
