using DS.DTOs;
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

            var model = new MeDto
            {
                Name = user.GetFullName(),
                Roles = roles,
                AppRoles = appRoles
            };

            return Ok(model);
        }
    }
}