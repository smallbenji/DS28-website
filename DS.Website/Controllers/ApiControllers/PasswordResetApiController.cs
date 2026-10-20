using DS.Application.Services.Accounts;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("api/v1/reset-password")]
    public class PasswordResetApiController(AccountService accountService) : Controller
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

            var result = await accountService.ResetPassword(data);
            if (result.UserNotFound) return BadRequest("Brugeren findes ikke.");

            if (!result.Result.Succeeded) return BadRequest(result.Result.Errors);

            return Ok();
        }
    }
}
