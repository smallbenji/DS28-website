using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DS.Backend.Authentication
{
    public class CookieAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter, IOrderedFilter
    {
        private static readonly string[] SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

        public int Order => 1000;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (SafeMethods.Contains(context.HttpContext.Request.Method, StringComparer.OrdinalIgnoreCase)) return;
            if (IsIgnored(context.ActionDescriptor)) return;
            if (IsAccessTokenRequest(context.HttpContext.User)) return;

            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new AntiforgeryValidationFailedResult();
            }
        }

        private static bool IsAccessTokenRequest(ClaimsPrincipal user)
        {
            return user.Identities.Any(identity => identity.AuthenticationType == AccessTokenAuthenticationHandler.SchemeName);
        }

        private static bool IsIgnored(ActionDescriptor descriptor)
        {
            return descriptor is ControllerActionDescriptor action
                && (action.MethodInfo.IsDefined(typeof(IgnoreAntiforgeryTokenAttribute), true)
                    || action.ControllerTypeInfo.IsDefined(typeof(IgnoreAntiforgeryTokenAttribute), true));
        }
    }
}
