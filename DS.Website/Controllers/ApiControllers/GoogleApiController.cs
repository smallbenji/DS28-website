using DS.Application.Services.Google;
using DS.Domain;
using DS.Domain.Models;
using DS.Website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/google")]
    public class GoogleApiController(
        IGoogleAuthService googleAuth,
        UserManager<User> userManager,
        ILogger<GoogleApiController> logger) : Controller
    {
        private const string StateCookieName = "ds_google_state";

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            return Ok(await googleAuth.GetStatusAsync(HttpContext.RequestAborted));
        }

        [Authorize(Roles = nameof(AppRoles.AdminAccess))]
        [HttpGet("connect")]
        public IActionResult Connect()
        {
            if (!googleAuth.IsConfigured) return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google Drive er ikke konfigureret.");

            var state = Guid.NewGuid().ToString("N");

            Response.Cookies.Append(StateCookieName, state, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
            });

            return Redirect(googleAuth.BuildAuthorizationUrl(state));
        }

        [Authorize(Roles = nameof(AppRoles.AdminAccess))]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
        {
            var expectedState = Request.Cookies[StateCookieName];
            Response.Cookies.Delete(StateCookieName);

            if (!string.IsNullOrWhiteSpace(error)) return Redirect("/admin/google?error=denied");

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state) || state != expectedState)
            {
                return Redirect("/admin/google?error=invalid");
            }

            try
            {
                await googleAuth.ConnectAsync(code, userManager.GetUserId(User), HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Google Drev kunne ikke forbindes.");
                return Redirect("/admin/google?error=failed");
            }

            return Redirect("/admin/google?connected=1");
        }

        [Authorize(Roles = nameof(AppRoles.AdminAccess))]
        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            await googleAuth.DisconnectAsync(HttpContext.RequestAborted);

            return Ok();
        }
    }
}
