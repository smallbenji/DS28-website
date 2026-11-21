using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DS.Domain;
using DS.Application.Services.Groups;
using DS.Application.Services.HistoricRegistration;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize(Roles = nameof(AppRoles.AdminAccess))]
    [Route("api/v1/admin")]
    public class AdminApiController(GroupInfoService groupInfoService, IHistoricRegistrationSyncQueue historicRegistrationSyncQueue) : Controller
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
                            Url = "/admin/user",
                            Icon = ["user-pen"],
                            RequiredRole = nameof(AppRoles.UsersView)
                        },
                        new HQPanelEntryDto
                        {
                            Title = "Gruppestyring",
                            Url = "/admin/groups",
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
                            Url = "/admin/camp-settings",
                            Icon = ["sliders"],
                            RequiredRoles = [nameof(AppRoles.PreSignupManage), nameof(AppRoles.AdminAccess)]
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
                            Url = "/admin/exports",
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
            var targetYear = await groupInfoService.GetLatestHistoricYear();

            var preSignup = await groupInfoService.GetTotalPresignup();
            var preSignupTarget = targetYear == null ? null : await groupInfoService.GetHistoricPresignupTotal(targetYear.Value);

            var finalSignup = await groupInfoService.GetTotalSignups();
            var finalSignupTarget = targetYear == null ? null : await groupInfoService.GetHistoricSignupTotal(targetYear.Value);

            return Ok(new List<SignupProgressDto>
            {
                new()
                {
                    Key = "pre-signup",
                    Label = "Forhåndstilmelding",
                    Current = preSignup,
                    Target = preSignupTarget,
                    TargetYear = preSignupTarget == null ? null : targetYear,
                    Status = StatusFor(preSignup, preSignupTarget)
                },
                new()
                {
                    Key = "final-signup",
                    Label = "Endelig tilmelding",
                    Current = finalSignup,
                    Target = finalSignupTarget,
                    TargetYear = finalSignupTarget == null ? null : targetYear,
                    Status = StatusFor(finalSignup, finalSignupTarget)
                }
            });
        }

        [HttpGet("system-counts")]
        public async Task<IActionResult> GetSystemCounts()
        {
            var users = await groupInfoService.GetTotalUsers();

            var groups = await groupInfoService.GetTotalGroups();

            return Ok(new SystemCountsDto { Users = users, Groups = groups });
        }

        [HttpPost("historic-registration/sync")]
        public IActionResult SyncHistoricRegistration()
        {
            if (!historicRegistrationSyncQueue.IsConfigured)
            {
                return BadRequest("Historisk synkronisering er ikke konfigureret. Sæt DS__HistoricRegistration__FirebaseCredentials og DS__HistoricRegistration__Year.");
            }

            historicRegistrationSyncQueue.Enqueue();

            return Accepted();
        }

        private static string StatusFor(int current, int? target)
        {
            if (target == null) return "no-target";
            if (target <= 0) return "reached";
            if (current >= target) return "reached";
            if (current * 2 >= target) return "halfway";

            return "behind";
        }
    }
}
