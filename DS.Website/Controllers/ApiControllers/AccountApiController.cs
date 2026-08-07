using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("/api/v1/account")]
    public class AccountApiController(UserManager<User> userManager, SignInManager<User> signInManager) : Controller
    {
        [HttpGet("2fa")]
        public async Task<IActionResult> TwoFactorStatus()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new TwoFactorStatusDto
            {
                TwoFactorEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = user.TwoFactorEnabled ? await userManager.CountRecoveryCodesAsync(user) : 0
            });
        }

        [HttpGet("2fa/setup")]
        public async Task<IActionResult> TwoFactorSetup()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            await AuthenticatorHelper.EnsureAuthenticatorKeyAsync(userManager, user);

            return Ok(new TwoFactorSetupDto
            {
                AuthenticatorUri = await AuthenticatorHelper.GetAuthenticatorUriAsync(userManager, user),
                ManualEntryKey = await userManager.GetAuthenticatorKeyAsync(user)
            });
        }

        [HttpGet("2fa/qr")]
        public async Task<IActionResult> TwoFactorQrCode()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            var authenticatorUri = await AuthenticatorHelper.GetAuthenticatorUriAsync(userManager, user);
            if (string.IsNullOrEmpty(authenticatorUri))
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";

            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);

            return File(qrCode.GetGraphic(20), "image/png");
        }

        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto data)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            if (user.TwoFactorEnabled)
            {
                return BadRequest("Tofaktorautentificering er allerede aktiveret.");
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Code))
            {
                return BadRequest("Kode skal udfyldes.");
            }

            await AuthenticatorHelper.EnsureAuthenticatorKeyAsync(userManager, user);

            var verificationCode = data.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var isValid = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode
            );

            if (!isValid)
            {
                return BadRequest("Den indtastede kode var ikke gyldig.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);

            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return Ok(new EnableTwoFactorResultDto
            {
                RecoveryCodes = recoveryCodes?.ToList() ?? []
            });
        }

        [HttpPost("2fa/recovery-codes")]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            if (!user.TwoFactorEnabled)
            {
                return BadRequest("Tofaktorautentificering er ikke aktiveret.");
            }

            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return Ok(new EnableTwoFactorResultDto
            {
                RecoveryCodes = recoveryCodes?.ToList() ?? []
            });
        }

        [HttpPost("2fa/reset")]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            await userManager.ResetAuthenticatorKeyAsync(user);
            await userManager.SetTwoFactorEnabledAsync(user, false);

            return Ok();
        }

        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorDto data)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Password))
            {
                return BadRequest("Adgangskode skal udfyldes.");
            }

            if (await userManager.IsInRoleAsync(user, nameof(AppGroups.SysAdmin)))
            {
                return BadRequest("SysAdmin-kontoen kan ikke deaktivere tofaktorautentificering.");
            }

            var isCorrectPassword = await userManager.CheckPasswordAsync(user, data.Password);
            if (!isCorrectPassword)
            {
                return BadRequest("Forkert adgangskode.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);

            return Ok();
        }

        [HttpPost("name")]
        public async Task<IActionResult> UpdateName([FromBody] UpdateNameDto data)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            if (data == null || string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
            {
                return BadRequest("Fornavn og efternavn skal udfyldes.");
            }

            user.FirstName = data.FirstName.Trim();
            user.LastName = data.LastName.Trim();

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await signInManager.RefreshSignInAsync(user);

            return Ok();
        }

        [HttpPost("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto data)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound();
            }

            if (data == null || string.IsNullOrWhiteSpace(data.OldPassword) || string.IsNullOrWhiteSpace(data.NewPassword))
            {
                return BadRequest("Adgangskoder skal udfyldes.");
            }

            var result = await userManager.ChangePasswordAsync(user, data.OldPassword, data.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await signInManager.RefreshSignInAsync(user);

            return Ok();
        }
    }
}
