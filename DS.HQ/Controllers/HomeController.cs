using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.HQ.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public HomeController() { }

        public IActionResult Index()
        {
            var retval = new HomeViewModel()
            {
                Name = HttpContext.User.Identity.Name,
                Shortcuts = [],
            };

            var roles = HttpContext.User.FindAll("groups").Select(x => x.Value).ToList();

            if (HttpContext.User.FindAll("groupnumber").Any())
            {
                retval.GroupNumber = HttpContext.User.FindFirst("groupnumber").Value;
            }

            if (HttpContext.User.IsInRole(Role.Admin))
            {
                retval.Shortcuts.AddRange(new List<HQPanelEntry>
                {
                    new()
                    {
                        Icon = "fa-solid fa-user",
                        Title = "Brugerstyring",
                        URL = "./User"
                    },
                    new()
                    {
                        Icon = "fa-solid fa-user-plus",
                        Title = "Gruppestyring",
                        URL = "./Group"
                    },
                    new()
                    {
                        Icon = "fa-solid fa-file-lines",
                        Title = "Audit log",
                        URL = "https://fisk.dk"
                    },
                });
            }

            if (HttpContext.User.IsInRole(Role.Material))
            {
                retval.Shortcuts.AddRange(new List<HQPanelEntry>
                {
                    new()
                    {
                        Icon = "fa-solid fa-cart-plus",
                        Title = "Materialesystem",
                        URL = "https://fisk.dk"
                    }
                });
            }

            retval.Shortcuts.AddRange(new List<HQPanelEntry>()
            {
                new()
                {
                    Icon = "fa-solid fa-money-check-dollar",
                    Title = "Økonomi",
                    URL = "https://fisk.dk"
                },
                new()
                {
                    Icon = "fa-brands fa-wordpress",
                    Title = "Wordpress",
                    URL = "https://fisk.dk"
                },
                new()
                {
                    Icon = "fa-solid fa-plus-circle",
                    Title = "Tilmeldingssystem",
                    URL = "https://fisk.dk"
                },
                new()
                {
                    Icon = "fa-solid fa-arrow-trend-up",
                    Title = "Grafana",
                    URL = "https://fisk.dk"
                },
                new()
                {
                    Icon = "fa-solid fa-newspaper",
                    Title = "Aktivitetsmodul",
                    URL = "https://fisk.dk"
                }
            });

            return View(retval);
        }
    }

    public class HomeViewModel
    {
        public List<HQPanelEntry> Shortcuts { get; set; }
        public string Name { get; set; }
        public string GroupNumber { get; set; }
    }

    public class HQPanelEntry
    {
        public string Title { get; set; }
        public string URL { get; set; }
        public string Icon { get; set; }
    }
}