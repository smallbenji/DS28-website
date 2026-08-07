using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/reset-password")]
    public class PasswordResetApiController(UserManager<User> userManager) : Controller
    {
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto data)
        {
            if (data == null ||
                string.IsNullOrWhiteSpace(data.UserId) ||
                string.IsNullOrWhiteSpace(data.Token) ||
                string.IsNullOrWhiteSpace(data.NewPassword))
            {
                return BadRequest("Ugyldig anmodning.");
            }

            var user = await userManager.FindByIdAsync(data.UserId);
            if (user == null)
            {
                return BadRequest("Brugeren findes ikke.");
            }

            var result = await userManager.ResetPasswordAsync(user, data.Token, data.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok();
        }
    }
}
