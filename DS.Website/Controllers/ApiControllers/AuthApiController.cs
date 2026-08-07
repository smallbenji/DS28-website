using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [Route("/api/v1/auth")]
    public class AuthApiController(UserManager<User> userManager, SignInManager<User> signInManager) : Controller
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Password))
            {
                return BadRequest("Email og adgangskode skal udfyldes.");
            }

            var user = await userManager.FindByEmailAsync(data.Email);
            if (user == null)
            {
                return BadRequest("Ugyldigt loginforsøg.");
            }

            var result = await signInManager.PasswordSignInAsync(
                user.UserName!,
                data.Password,
                isPersistent: true,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return Ok(new AuthResultDto { ReturnUrl = ResolveReturnUrl(data.ReturnUrl) });
            }

            if (result.RequiresTwoFactor)
            {
                return Ok(new AuthResultDto { RequiresTwoFactor = true });
            }

            if (result.IsLockedOut)
            {
                return BadRequest("Denne bruger er blevet låst, venligst kontakt IT");
            }

            return BadRequest("Ugyldigt loginforsøg.");
        }

        [HttpPost("2fa")]
        public async Task<IActionResult> TwoFactorLogin([FromBody] TwoFactorLoginDto data)
        {
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest("Ugyldigt loginforsøg.");
            }

            if (data == null || string.IsNullOrWhiteSpace(data.TwoFactorCode))
            {
                return BadRequest("Kode skal udfyldes.");
            }

            var authenticatorCode = data.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode,
                isPersistent: true,
                rememberClient: data.RememberMachine
            );

            if (result.Succeeded)
            {
                return Ok(new AuthResultDto { ReturnUrl = ResolveReturnUrl(data.ReturnUrl) });
            }

            if (result.IsLockedOut)
            {
                return BadRequest("Denne bruger er blevet låst, venligst kontakt IT");
            }

            return BadRequest("Ugyldig autentificeringskode.");
        }

        [HttpPost("recovery-code")]
        public async Task<IActionResult> RecoveryCodeLogin([FromBody] RecoveryCodeLoginDto data)
        {
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest("Ugyldigt loginforsøg.");
            }

            if (data == null || string.IsNullOrWhiteSpace(data.RecoveryCode))
            {
                return BadRequest("Recovery code skal udfyldes.");
            }

            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(data.RecoveryCode);

            if (result.Succeeded)
            {
                return Ok(new AuthResultDto { ReturnUrl = ResolveReturnUrl(data.ReturnUrl) });
            }

            if (result.IsLockedOut)
            {
                return BadRequest("Denne bruger er blevet låst, venligst kontakt IT");
            }

            return BadRequest("Ugyldig recovery code.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto data)
        {
            if (data == null ||
                string.IsNullOrWhiteSpace(data.FirstName) ||
                string.IsNullOrWhiteSpace(data.LastName) ||
                string.IsNullOrWhiteSpace(data.Email) ||
                string.IsNullOrWhiteSpace(data.Password))
            {
                return BadRequest("Alle felter skal udfyldes.");
            }

            var newUser = new User
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                UserName = data.Email,
                Email = data.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser, data.Password);

            if (!result.Succeeded)
            {
                return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return Ok();
        }

        private static string ResolveReturnUrl(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
            {
                return returnUrl;
            }

            return "/";
        }
    }
}
