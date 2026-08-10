using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("/api/v1/home")]
    public class HomeApiController(UserManager<User> userManager) : Controller
    {
        public static List<HQPanelEntryDto> GetShortcuts(IUrlHelper Url) => [
            // Need roles
            new() { Title = "Brugerstyring", Url = "/user", Icon = ["user-pen"], RequiredRole = nameof(AppRoles.UsersView) },
            new() { Title = "Gruppestyring", Url = "/groups", Icon = ["users-gear"], RequiredRole = nameof(AppRoles.GroupsView) },
            
            // No roles needed
            new() { Title = "Aktivitetsmodul", Url = "/activity", Icon = ["fa-solid fa-newspaper"] },
            new() { Title = "Hjemmeside", Url = "https://distriktssommerlejr.dk", Icon = ["fab", "wordpress"] },

            new()
            {
                Title = "Wordpress login",
                Url = "https://www.distriktssommerlejr.dk/wp-login.php?force_redirect=1",
                Icon = ["fab", "wordpress"],
                RequiredRoles = [nameof(AppRoles.WordPressEditor), nameof(AppRoles.WordPressAdmin)]
            },

            // Not in use
            // new() { Title = "Audit log", Url = "#", Icon = ["fa-solid fa-file-lines"], RequiredRole = nameof(AppRoles.AuditLogView) },
            // new() { Title = "Materialesystem", Url = "#", Icon = ["fa-solid fa-cart-plus"], RequiredRole = nameof(AppRoles.GroupsDelete) },
            // new() { Title = "Økonomi", Url = "#", Icon = ["fa-solid fa-money-check-dollar"], RequiredRole = nameof(AppRoles.GroupsDelete) },
            // new() { Title = "Tilmeldingssystem", Url = "#", Icon = ["fa-solid fa-plus-circle"] },
            // new() { Title = "Grafana", Url = "#", Icon = ["fa-solid fa-arrow-trend-up"] },
        ];

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var retval = new HomeViewModelDto()
            {
                Shortcuts = GetShortcuts(Url)
                    .Where(s => (s.RequiredRole == null || User.IsInRole(s.RequiredRole))
                        && (s.RequiredRoles == null || s.RequiredRoles.Length == 0 || s.RequiredRoles.Any(User.IsInRole)))
                    .ToList(),
            };

            var userId = userManager.GetUserId(HttpContext.User);
            var user = await userManager.Users
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user != null && user.Group != null)
            {
                retval.Shortcuts.Add(new HQPanelEntryDto
                {
                    Title = "Gruppe",
                    Url = "/group",
                    Icon = ["users"]
                });
            }

            return Ok(retval);
        }
    }
}