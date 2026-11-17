using System.Security.Claims;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Backend.Mcp
{
    public sealed class McpAccessRequirement : IAuthorizationRequirement
    {
    }

    public sealed class McpAccessHandler(UserManager<User> userManager) : AuthorizationHandler<McpAccessRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, McpAccessRequirement requirement)
        {
            if (await HasAccess(userManager, context.User)) context.Succeed(requirement);
        }

        public static async Task<bool> HasAccess(UserManager<User> userManager, ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true) return false;

            var userId = userManager.GetUserId(principal);
            var user = await userManager.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId);
            if (user == null || await userManager.IsLockedOutAsync(user)) return false;

            var roles = await userManager.GetRolesAsync(user);
            return roles.Contains(nameof(AppGroups.McpUser));
        }
    }
}
