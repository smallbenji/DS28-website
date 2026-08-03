using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website;

public class ClaimsTransformer(UserManager<User> userManager, IMemoryCache memoryCache) : IClaimsTransformation
{
    public static readonly string RoleCachePrefix = "user-roles:";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        var userId = userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        var roles = await GetRolesFromCache(userId);
        if (roles == null)
        {
            return principal;
        }

        var claims = identity.Claims
            .Where(claim => claim.Type != identity.RoleClaimType)
            .ToList();

        foreach (var role in roles.UserRoles)
        {
            claims.Add(new Claim(identity.RoleClaimType, role));
        }

        foreach (var appRole in roles.AppRoles)
        {
            claims.Add(new Claim(identity.RoleClaimType, appRole));
        }

        var newIdentity = new ClaimsIdentity(claims, identity.AuthenticationType,
            identity.NameClaimType, identity.RoleClaimType);

        return new ClaimsPrincipal(newIdentity);
    }

    private async Task<UserRolesCache?> GetRolesFromCache(string userId)
    {
        var cacheKey = $"{RoleCachePrefix}{userId}";

        if (memoryCache.TryGetValue(cacheKey, out UserRolesCache? cached) && cached is not null)
        {
            return cached;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var userRoles = (await userManager.GetRolesAsync(user)).ToList();

        var appRoles = userRoles
            .SelectMany(roleName =>
                Enum.TryParse<AppGroups>(roleName, out var group) &&
                AppAccess.Matrix.TryGetValue(group, out var subRoles)
                    ? subRoles
                    : [])
            .Distinct()
            .ToList();

        var entry = new UserRolesCache(userRoles, appRoles);
        memoryCache.Set(cacheKey, entry, TimeSpan.FromMinutes(5));

        return entry;
    }

    private sealed record UserRolesCache(List<string> UserRoles, List<string> AppRoles);
}
