using System.Security.Claims;
using System.Text.Encodings.Web;
using DS.Application.Services.AccessTokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DS.Backend.Authentication
{
    public class AccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AccessTokenService accessTokenService)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "AccessToken";
        public const string CookieOrTokenScheme = "IdentityOrAccessToken";
        private const string HeaderPrefix = "Bearer ";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

            var token = header[HeaderPrefix.Length..].Trim();
            if (token.Length == 0) return AuthenticateResult.NoResult();

            var identity = await accessTokenService.Authenticate(token);
            if (identity == null) return AuthenticateResult.Fail("Ukendt, udløbet eller tilbagekaldt adgangstoken.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, identity.UserId),
                new Claim(ClaimTypes.Name, identity.UserName ?? identity.UserId),
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role));

            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }
    }
}
