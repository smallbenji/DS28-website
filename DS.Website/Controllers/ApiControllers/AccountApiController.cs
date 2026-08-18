using DS.DTOs;
using DS.Website.Services;
using Microsoft.AspNetCore.Authentication;
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
        private readonly int MAX_PASSKEY_COUNT = 3;

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
                RecoveryCodesLeft = user.TwoFactorEnabled ? await userManager.CountRecoveryCodesAsync(user) : 0,
                HasEnabledAuthenticator = user.HasEnabledAuthenticator
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
            user.HasEnabledAuthenticator = true;
            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                // Throw a warning in some kind of logging system
            }

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
            user.HasEnabledAuthenticator = false;
            await userManager.UpdateAsync(user);

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

            var passkeys = await userManager.GetPasskeysAsync(user);
            foreach(var passkey in passkeys)
            {
                await userManager.RemovePasskeyAsync(user, passkey.CredentialId);
            }
            user.HasEnabledAuthenticator = false;
            await userManager.UpdateAsync(user);

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

        [HttpGet("2fa/passkeys")]
        public async Task<IActionResult> ListPasskeys()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            var passkeys = await userManager.GetPasskeysAsync(user);

            return Ok(passkeys.ToDtoList());
        }

        [HttpPost("2fa/passkeys")]
        public async Task<IActionResult> RegisterPasskey([FromBody] PasskeyAttestationRequestDto data)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            var result = await signInManager.PerformPasskeyAttestationAsync(data.CredentialJson);

            if (!result.Succeeded || result.UserEntity.Id != user.Id)
            {
                return BadRequest("Ugyldig loginforsøg");
            }

            var passkey = result.Passkey;

            if (!string.IsNullOrEmpty(data.Name)) passkey.Name = data.Name;

            var addPasskeyResult = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
            if (!addPasskeyResult.Succeeded)
            {
                return BadRequest("kunne ikke gemme passkey");
            }

            // Hvis brugeren ikke har nogen 2fa sat op endu, slå 2fa til og generer "recovery codes"
            if (!user.TwoFactorEnabled)
            {
                await userManager.SetTwoFactorEnabledAsync(user, true);
                var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                return Ok(new EnableTwoFactorResultDto
                {
                    RecoveryCodes = recoveryCodes?.ToList() ?? []
                });
            }

            return Ok();
        }


        [HttpDelete("2fa/passkeys/{id}")]
        public async Task<IActionResult> RemovePasskey(string id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            byte[] credentailId;
            try
            {
                credentailId = Base64UrlTextEncoder.Decode(id);
            }
            catch
            {
                return BadRequest("ukendt passkey");
            }

            var passkeys = await userManager.GetPasskeysAsync(user);
            bool hasTotp = await userManager.GetAuthenticatorKeyAsync(user) != null;

            if (passkeys.Count == 1 && !hasTotp)
            {
                return BadRequest("Du kan ikke fjerne din sidste passkeyy. tilføj en anden 2FA-metode først.");

            }

            await userManager.RemovePasskeyAsync(user, credentailId);

            return Ok();
        }

        [HttpPost("2fa/passkeys/options")]
        public async Task<IActionResult> PasskeyCreationOptions([FromBody] PasskeyCreateOptionsDto options)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();
            var passkeys = await userManager.GetPasskeysAsync(user);
            if (passkeys.Count >= MAX_PASSKEY_COUNT) return BadRequest("Max antal passkeys er nået.");

            var result = await signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = user.Id,
                Name = user.UserName,
                DisplayName = options.DisplayName
            });

            return Ok(new PasskeyOptionsDto
            {
                OptionsJson = result
            });
        }

    }
}
