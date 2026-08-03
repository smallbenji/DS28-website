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
        // Hvis WordPress har sendt en 'scope' parameter med i sin POST, 
        // fjerner vi den fra anmodningen, så OpenIddict ikke kaster en fejl.
        if (Request.HasFormContentType && Request.Form.ContainsKey("scope"))
        {
            // Vi laver en modificeret samling af form-parametre uden 'scope'
            var formFields = Request.Form.ToDictionary(x => x.Key, x => x.Value);
            formFields.Remove("scope");

            // Overskriv anmodningens form-data
            Request.Form = new FormCollection(formFields.ToDictionary(x => x.Key, x => x.Value));
        }

        // Nu kan OpenIddict udtrække anmodningen uden at fejle på ID2074
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