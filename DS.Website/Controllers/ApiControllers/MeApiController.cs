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
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Ok(new MeDto
                {
                    IsAuthenticated = HttpContext.User.Identity.IsAuthenticated
                });
            }

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
                IsAuthenticated = HttpContext.User.Identity.IsAuthenticated,
                Name = user.GetFullName(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Roles = roles,
                AppRoles = appRoles
            };

            return Ok(model);
        }
    }
}