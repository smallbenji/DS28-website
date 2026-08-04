using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("/api/v1/home")]
    public class HomeApiController : Controller
    {
        public HomeApiController() { }

        public static List<HQPanelEntry> GetShortcuts(IUrlHelper Url) => [
            // Need roles
            new() { Title = "Brugerstyring", Url = "/user", Icon = ["user"], RequiredRole = nameof(AppRoles.UsersView) },
            new() { Title = "Gruppestyring", Url = "/groups", Icon = ["users"], RequiredRole = nameof(AppRoles.GroupView) },
            
            // No roles needed
            new() { Title = "Wordpress", Url = "https://distriktssommerlejr.dk", Icon = ["fab", "wordpress"] },

            // Not in use
            // new() { Title = "Audit log", URL = "#", Icon = "fa-solid fa-file-lines", RequiredRole = nameof(AppRoles.AuditLogView) },
            // new() { Title = "Materialesystem", URL = "#", Icon = "fa-solid fa-cart-plus", RequiredRole = nameof(AppRoles.GroupDelete) },
            // new() { Title = "Økonomi", URL = "#", Icon = "fa-solid fa-money-check-dollar", RequiredRole = nameof(AppRoles.GroupDelete) },
            // new() { Title = "Tilmeldingssystem", URL = "#", Icon = "fa-solid fa-plus-circle" },
            // new() { Title = "Grafana", URL = "#", Icon = "fa-solid fa-arrow-trend-up" },
            // new() { Title = "Aktivitetsmodul", URL = "#", Icon = "fa-solid fa-newspaper" },
        ];

        [HttpGet]
        public IActionResult Index()
        {
            var retval = new HomeViewModel()
            {
                Shortcuts = GetShortcuts(Url)
                    .Where(s => s.RequiredRole == null || User.IsInRole(s.RequiredRole))
                    .ToList(),
            };

            return Ok(retval);
        }
    }

    public class HomeViewModel
    {
        public List<HQPanelEntry> Shortcuts { get; set; }
    }

    public class HQPanelEntry
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string[] Icon { get; set; }
        public string RequiredRole { get; set; }
    }
}