using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("/api/v1/home")]
    public class HomeApiController : Controller
    {
        public HomeApiController() { }

        public static List<HQPanelEntryDto> GetShortcuts(IUrlHelper Url) => [
            // Need roles
            new() { Title = "Brugerstyring", Url = "/user", Icon = ["user"], RequiredRole = nameof(AppRoles.UsersView) },
            new() { Title = "Gruppestyring", Url = "/groups", Icon = ["users"], RequiredRole = nameof(AppRoles.GroupsView) },
            
            // No roles needed
            new() { Title = "Wordpress", Url = "https://distriktssommerlejr.dk", Icon = ["fab", "wordpress"] },

            // Not in use
            // new() { Title = "Audit log", Url = "#", Icon = ["fa-solid fa-file-lines"], RequiredRole = nameof(AppRoles.AuditLogView) },
            // new() { Title = "Materialesystem", Url = "#", Icon = ["fa-solid fa-cart-plus"], RequiredRole = nameof(AppRoles.GroupsDelete) },
            // new() { Title = "Økonomi", Url = "#", Icon = ["fa-solid fa-money-check-dollar"], RequiredRole = nameof(AppRoles.GroupsDelete) },
            // new() { Title = "Tilmeldingssystem", Url = "#", Icon = ["fa-solid fa-plus-circle"] },
            // new() { Title = "Grafana", Url = "#", Icon = ["fa-solid fa-arrow-trend-up"] },
            // new() { Title = "Aktivitetsmodul", Url = "#", Icon = ["fa-solid fa-newspaper"] },
        ];

        [HttpGet]
        public IActionResult Index()
        {
            var retval = new HomeViewModelDto()
            {
                Shortcuts = GetShortcuts(Url)
                    .Where(s => s.RequiredRole == null || User.IsInRole(s.RequiredRole))
                    .ToList(),
            };

            return Ok(retval);
        }
    }
}