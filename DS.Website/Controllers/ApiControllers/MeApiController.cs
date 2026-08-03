using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Route("/api/v1/me")]
    public class MeApiController(UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var roles = (await userManager.GetRolesAsync(user)).ToList();

            var appRoles = roles
                .SelectMany(roleName =>
                    Enum.TryParse<AppGroups>(roleName, out var group) &&
                    AppAccess.Matrix.TryGetValue(group, out var subRoles)
                        ? subRoles
                        : [])
                .Distinct()
                .ToList();

            var model = new MeDTO
            {
                Name = user.GetFullName(),
                Roles = roles,
                AppRoles = appRoles
            };

            return Ok(model);
        }
    }

    public class MeDTO
    {
        public string Name { get; set; }
        public List<string> Roles { get; set; } = [];
        public List<string> AppRoles { get; set; } = [];
    }
}