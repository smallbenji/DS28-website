using DS.Application.Services.OpenIddict;
using System.Security.Claims;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace DS.Backend.Controllers
{
    [IgnoreAntiforgeryToken]
    public class AuthorizationController(SignInManager<User> signInManager, UserManager<User> userManager, IOpenIddictApplicationManager applications) : Controller
    {
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

            if (User.Identity?.IsAuthenticated != true)
            {
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = Request.Path + Request.QueryString
                });
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = Request.Path + Request.QueryString
                });
            }

            if (!await HasClientAccessAsync(request.ClientId, User)) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var userId = await userManager.GetUserIdAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;
            if (!principal.HasClaim(OpenIddictConstants.Claims.Subject))
            {
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, userId));
            }

            principal.SetScopes(request.GetScopes());
            foreach (var claim in identity.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            {
                throw new InvalidOperationException("Grant-typen understøttes ikke.");
            }

            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal;
            if (principal == null) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var subject = principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = string.IsNullOrEmpty(subject) ? null : await userManager.FindByIdAsync(subject);
            if (user == null || !await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsRefreshTokenGrantType() && await signInManager.ValidateSecurityStampAsync(principal) == null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var currentPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            if (!await HasClientAccessAsync(request.ClientId, currentPrincipal)) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var scopes = principal.GetScopes();
            if (request.IsRefreshTokenGrantType() && !string.IsNullOrEmpty(request.Scope))
            {
                var requestedScopes = request.GetScopes();
                if (requestedScopes.Except(scopes, StringComparer.Ordinal).Any()) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                scopes = requestedScopes;
            }

            var identity = (ClaimsIdentity)principal.Identity!;
            foreach (var claim in identity.Claims.Where(claim => !claim.Type.StartsWith("oi_", StringComparison.Ordinal)).ToList())
            {
                identity.RemoveClaim(claim);
            }
            identity.AddClaims(currentPrincipal.Claims);
            identity.SetClaim(OpenIddictConstants.Claims.Subject, subject);
            principal.SetScopes(scopes);
            foreach (var claim in identity.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo")]
        [HttpPost("~/connect/userinfo")]
        public async Task<IActionResult> Userinfo()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge(new AuthenticationProperties
                {
                    Parameters =
                    {
                        [OpenIddictConstants.Parameters.Error] = OpenIddictConstants.Errors.InvalidToken,
                        [OpenIddictConstants.Parameters.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                    }
                }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [OpenIddictConstants.Claims.Subject] = await userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(OpenIddictConstants.Scopes.Email))
            {
                claims[OpenIddictConstants.Claims.Email] = await userManager.GetEmailAsync(user);
                claims[OpenIddictConstants.Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasScope(OpenIddictConstants.Scopes.Profile))
            {
                claims[OpenIddictConstants.Claims.Name] = user.GetFullName();
                claims[OpenIddictConstants.Claims.PreferredUsername] = await userManager.GetUserNameAsync(user);
            }

            if (User.HasScope(OpenIddictConstants.Scopes.Roles))
            {
                var userRoles = await userManager.GetRolesAsync(user);

                var wordpressRoles = AppAccess.ResolveAppRoles(userRoles)
                    .Where(role => role is nameof(AppRoles.WordPressEditor) or nameof(AppRoles.WordPressAdmin))
                    .ToList();

                if (wordpressRoles.Count > 0) claims["roles"] = wordpressRoles;
            }

            return Ok(claims);
        }

        private async Task<bool> HasClientAccessAsync(string clientId, ClaimsPrincipal principal)
        {
            if (string.IsNullOrEmpty(clientId)) return false;

            var application = await applications.FindByClientIdAsync(clientId, HttpContext.RequestAborted);
            if (application == null) return false;

            var properties = await applications.GetPropertiesAsync(application, HttpContext.RequestAborted);
            var requiredRoles = OpenIddictClientPolicy.ReadRequiredRoles(properties);

            return requiredRoles.Any(principal.IsInRole);
        }

        private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            if (claim.Type.StartsWith("oi_", StringComparison.Ordinal)
                || claim.Type == "AspNet.Identity.SecurityStamp") yield break;

            yield return OpenIddictConstants.Destinations.AccessToken;

            switch (claim.Type)
            {
                case OpenIddictConstants.Claims.Name:
                case OpenIddictConstants.Claims.Email:
                case OpenIddictConstants.Claims.Role:
                    if (principal.HasScope(OpenIddictConstants.Scopes.Profile) || principal.HasScope(OpenIddictConstants.Scopes.Email))
                    {
                        yield return OpenIddictConstants.Destinations.IdentityToken;
                    }
                    break;
            }
        }
    }
}
