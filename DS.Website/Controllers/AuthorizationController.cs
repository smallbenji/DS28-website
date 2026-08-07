using System.Security.Claims;
using DS;
using DS.Website;
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
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

        if (!User.Identity.IsAuthenticated)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.Path + Request.QueryString
            });
        }

        // 1. Hent brugeren ud fra din DS.User klasse
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.Path + Request.QueryString
            });
        }

        // Kun brugere med en WordPress-rolle (WordPressEditor/WordPressAdmin) må logge ind i WordPress
        if (!User.IsInRole(nameof(AppRoles.WordPressEditor)) && !User.IsInRole(nameof(AppRoles.WordPressAdmin)))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // 2. Opret det rå principal fra ASP.NET Core Identity
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // 3. VIGTIGT: OpenIddict skal vide præcis, hvilket ID der er 'subject' (sub)
        var userId = await _userManager.GetUserIdAsync(user);

        // Vi tilføjer eksplicit sub claimet, hvis det ikke sidder rigtigt i forvejen
        var identity = (ClaimsIdentity)principal.Identity!;
        if (!principal.HasClaim(OpenIddictConstants.Claims.Subject))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, userId));
        }

        // 4. Fortæl OpenIddict hvor claims må sendes hen (Destinations)
        // Uden dette vil WordPress ikke kunne læse e-mail eller navn fra tokens
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, request));
        }

        // 5. Sæt de tilladte scopes på dit principal
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("OIDC anmodningen kunne ikke hentes.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal;

            if (principal == null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Sørg for at gensætte scopes og destinationer på tokens
            principal.SetScopes(request.GetScopes());

            var identity = (ClaimsIdentity)principal.Identity!;
            foreach (var claim in identity.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, request));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("Grant-typen understøttes ikke.");
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.GetUserAsync(User);
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
            [OpenIddictConstants.Claims.Subject] = await _userManager.GetUserIdAsync(user)
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = await _userManager.GetEmailAsync(user);
            claims[OpenIddictConstants.Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = user.GetFullName();
            claims[OpenIddictConstants.Claims.PreferredUsername] = await _userManager.GetUserNameAsync(user);
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Roles))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var wordpressRoles = userRoles
                .SelectMany(roleName =>
                    Enum.TryParse<AppGroups>(roleName, out var group) &&
                    AppAccess.Matrix.TryGetValue(group, out var subRoles)
                        ? subRoles
                        : [])
                .Where(role => role is nameof(AppRoles.WordPressEditor) or nameof(AppRoles.WordPressAdmin))
                .Distinct()
                .ToList();

            if (wordpressRoles.Count > 0)
            {
                claims["roles"] = wordpressRoles;
            }
        }

        return Ok(claims);
    }

    // Hjælpemetode til at styre, hvilke data der sendes med i Access Tokens og ID Tokens
    private static IEnumerable<string> GetDestinations(Claim claim, OpenIddictRequest request)
    {
        // Standard destination: Gem det altid i Access Token
        yield return OpenIddictConstants.Destinations.AccessToken;

        // Hvis det er følsomme/profiloplysninger, smider vi det også i ID Token, 
        // så WordPress kan læse det direkte uden ekstra userinfo kald, hvis den vil det.
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.Role:
                if (request.HasScope(OpenIddictConstants.Scopes.Profile) || request.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                break;
        }
    }
}