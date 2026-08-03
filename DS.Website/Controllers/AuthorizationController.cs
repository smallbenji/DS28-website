using System.Security.Claims;
using DS;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

public class AuthorizationController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AuthorizationController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        // 1. Hent OIDC-anmodningen fra OpenIddict
        var request = HttpContext.GetOpenIddictServerRequest() ?? 
            throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

        // 2. Hvis brugeren IKKE er logget ind i din management platform, send dem til dit login-skærm
        if (!User.Identity.IsAuthenticated)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.Path + Request.QueryString
            });
        }

        // 3. Hent brugeren og opret et "ClaimsPrincipal" som OpenIddict kan bruge til at lave et token
        var user = await _userManager.GetUserAsync(User);
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Sæt scopes (f.eks. openid, profile, email)
        principal.SetScopes(request.GetScopes());

        // 4. Send brugeren tilbage til WordPress med en godkendelses-kode
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        // Dette endpoint kalder WordPress i baggrunden for at bytte koden til et token
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("Grant-typen understøttes ikke.");
    }
}