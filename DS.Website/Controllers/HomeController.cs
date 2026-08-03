using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public HomeController() { }

        public static List<HQPanelEntry> GetShortcuts(IUrlHelper Url) => [
            // Need roles
            new() { Title = "Brugerstyring", URL = Url.Action<UserManagementController>(c => c.Index()), Icon = "fa-solid fa-user", RequiredRole = nameof(AppRoles.UsersView) },
            new() { Title = "Gruppestyring", URL = "#", Icon = "fa-solid fa-users", RequiredRole = nameof(AppRoles.GroupView) },
            
            // No roles needed
            new() { Title = "Wordpress", URL = "https://distriktssommerlejr.dk", Icon = "fa-brands fa-wordpress" },

            // Not in use
            // new() { Title = "Audit log", URL = "#", Icon = "fa-solid fa-file-lines", RequiredRole = nameof(AppRoles.AuditLogView) },
            // new() { Title = "Materialesystem", URL = "#", Icon = "fa-solid fa-cart-plus", RequiredRole = nameof(AppRoles.GroupDelete) },
            // new() { Title = "Økonomi", URL = "#", Icon = "fa-solid fa-money-check-dollar", RequiredRole = nameof(AppRoles.GroupDelete) },
            // new() { Title = "Tilmeldingssystem", URL = "#", Icon = "fa-solid fa-plus-circle" },
            // new() { Title = "Grafana", URL = "#", Icon = "fa-solid fa-arrow-trend-up" },
            // new() { Title = "Aktivitetsmodul", URL = "#", Icon = "fa-solid fa-newspaper" },
        ];

        public IActionResult Index()
        {
            var retval = new HomeViewModel()
            {
                Shortcuts = GetShortcuts(Url)
                    .Where(s => s.RequiredRole == null || User.IsInRole(s.RequiredRole))
                    .ToList(),
            };

            return View(retval);
        }
    }

    public class HomeViewModel
    {
        public List<HQPanelEntry> Shortcuts { get; set; }
    }

    public class HQPanelEntry
    {
        public string Title { get; set; }
        public string URL { get; set; }
        public string Icon { get; set; }
        public string RequiredRole { get; set; }
    }
}