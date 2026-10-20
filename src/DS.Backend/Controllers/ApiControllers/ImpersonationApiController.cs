using System.Security.Claims;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers
{
    [Authorize]
    [Route("api/v1/impersonation")]
    public class ImpersonationApiController(IDataProtectionProvider dataProtectionProvider) : Controller
    {
        [HttpGet("groups")]
        public IActionResult GetGroups()
        {
            if (!IsSysAdminOrigin(HttpContext.User)) return Forbid();

            var groups = Enum.GetValues<AppGroups>()
                .Select(group => new ImpersonationGroupDto
                {
                    Name = group.ToString(),
                    Title = AppAccess.GetGroupTitle(group),
                })
                .ToList();

            return Ok(groups);
        }

        [HttpPost("start")]
        public IActionResult Start([FromBody] StartImpersonationDto data)
        {
            if (!IsSysAdminOrigin(HttpContext.User)) return Forbid();

            if (data == null || string.IsNullOrWhiteSpace(data.Group)) return BadRequest("Rolle mangler.");
            if (!Enum.TryParse<AppGroups>(data.Group, out _)) return BadRequest("Ukendt rolle.");

            var protectedValue = Impersonation.ProtectGroups(dataProtectionProvider, [data.Group]);

            Response.Cookies.Append(Impersonation.CookieName, protectedValue, BuildCookieOptions());

            return Ok();
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            if (!IsSysAdminOrigin(HttpContext.User)) return Forbid();

            if (Request.Cookies.ContainsKey(Impersonation.CookieName))
            {
                Response.Cookies.Delete(Impersonation.CookieName, BuildCookieOptions());
            }

            return Ok();
        }

        private static bool IsSysAdminOrigin(ClaimsPrincipal user)
        {
            return user.HasClaim(Impersonation.OriginClaimType, nameof(AppGroups.SysAdmin));
        }

        private CookieOptions BuildCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = HttpContext.Request.IsHttps,
                Path = "/",
            };
        }
    }
}