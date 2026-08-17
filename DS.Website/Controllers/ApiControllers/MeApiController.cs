using DS.DTOs;
using Microsoft.AspNetCore.Authentication;
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
            if (user == null)
                return NotFound();

            var roles = (await userManager.GetRolesAsync(user)).ToList();

            var appRoles = roles
                .SelectMany(roleName =>
                    Enum.TryParse<AppGroups>(roleName, out var group) &&
                    AppAccess.Matrix.TryGetValue(group, out var subRoles)
                        ? subRoles
                        : [])
                .Distinct()
                .ToList();

            var passkeys = (await userManager.GetPasskeysAsync(user)).Select(p => new PasskeyDto
            {
                Id = Base64UrlTextEncoder.Encode(p.CredentialId),
                Name = p.Name,
                CreatedAt = p.CreatedAt,
                Transports = p.Transports ?? [],
                IsBackedUp = p.IsBackedUp
            });

            var model = new MeDto
            {
                IsAuthenticated = HttpContext.User.Identity.IsAuthenticated,
                Id = user.Id,
                Name = user.GetFullName(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                MustEnableTwoFactor = await userManager.IsInRoleAsync(user, nameof(AppGroups.SysAdmin)) && !user.TwoFactorEnabled,
                Roles = roles,
                AppRoles = appRoles,
                Passkeys = passkeys.ToList()
            };

            return Ok(model);
        }
    }
}