using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Components
{
    public class UserProfileViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new UserProfileViewModel();

            if (User.Identity.IsAuthenticated == true)
            {
                model.IsLoggedIn = true;
                model.Name = HttpContext.User.FindFirstValue("full_name")
                    ?? HttpContext.User.Identity?.Name
                    ?? string.Empty;

                model.Groups = HttpContext.User.Claims
                    .Where(claim => claim.Type == ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .Where(groupName => Enum.TryParse<AppGroups>(groupName, out _))
                    .Distinct()
                    .OrderBy(groupName => groupName)
                    .ToList();
            }

            return View(model);
        }
    }

    public class UserProfileViewModel
    {
        public string Name { get; set; }
        public bool IsLoggedIn { get; set; }
        public List<string> Groups { get; set; } = [];
    }
}