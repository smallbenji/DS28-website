using System.Security.Claims;
using System.Text.Encodings.Web;
using DS.Application.Services.AccessTokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace DS.Backend.Mcp
{
    public class McpAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AccessTokenService accessTokens) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Mcp";
        public const string PolicyName = "Mcp";
        public const string MetadataPath = "/.well-known/oauth-protected-resource";
        private const string BearerPrefix = "Bearer ";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

            var token = header[BearerPrefix.Length..].Trim();
            if (token.Length == 0) return AuthenticateResult.NoResult();

            var handlers = Context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            var handler = await handlers.GetHandlerAsync(Context, OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            var authenticated = await handler.AuthenticateAsync();
            if (authenticated.Succeeded)
            {
                return AuthenticateResult.Success(new AuthenticationTicket(WithUserId(authenticated.Principal), SchemeName));
            }

            var identity = await accessTokens.Authenticate(token);
            if (identity == null) return AuthenticateResult.Fail("Ukendt, udløbet eller tilbagekaldt adgangstoken.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, identity.UserId),
                new Claim(ClaimTypes.Name, identity.UserName ?? identity.UserId),
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var origin = $"{Request.Scheme}://{Request.Host}";
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{origin}{MetadataPath}\", scope=\"openid profile email roles offline_access\"";

            return Task.CompletedTask;
        }

        public static ClaimsPrincipal WithUserId(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity;
            if (identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier)) return principal;

            var subject = identity.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            if (string.IsNullOrEmpty(subject)) return principal;

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
            return principal;
        }
    }
}
