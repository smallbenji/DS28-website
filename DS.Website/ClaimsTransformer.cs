using DS.Data;
using DS.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website;

public class ClaimsTransformer(
    UserManager<User> userManager,
    DataDbContext dataDb,
    IMemoryCache memoryCache,
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider) : IClaimsTransformation
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

        foreach (var realGroup in roles.UserRoles)
        {
            claims.Add(new Claim(Impersonation.OriginClaimType, realGroup));
        }

        var effectiveGroups = roles.UserRoles;

        if (roles.UserRoles.Contains(nameof(AppGroups.SysAdmin)))
        {
            var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[Impersonation.CookieName];
            var impersonatedGroups = Impersonation.ResolveCookieGroups(dataProtectionProvider, cookieValue);
            if (impersonatedGroups != null)
            {
                effectiveGroups = impersonatedGroups;
                claims.Add(new Claim(Impersonation.ActiveClaimType, string.Join(',', impersonatedGroups)));
            }
        }

        foreach (var group in effectiveGroups)
        {
            claims.Add(new Claim(identity.RoleClaimType, group));
        }

        foreach (var appRole in AppAccess.ResolveAppRoles(effectiveGroups))
        {
            claims.Add(new Claim(identity.RoleClaimType, appRole));
        }

        var newIdentity = new ClaimsIdentity(claims, identity.AuthenticationType,
            identity.NameClaimType, identity.RoleClaimType);

        return new ClaimsPrincipal(newIdentity);
    }

    private async Task<UserRolesCache> GetRolesFromCache(string userId)
    {
        var cacheKey = $"{RoleCachePrefix}{userId}";

        if (memoryCache.TryGetValue(cacheKey, out UserRolesCache cached) && cached is not null)
        {
            return cached;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var userRoles = (await userManager.GetRolesAsync(user)).ToList();

        var positionGroups = await OrgPositionAccess.GetEffectiveGroups(dataDb, userId).ToListAsync();

        userRoles = userRoles.Concat(positionGroups).Distinct().ToList();

        var appRoles = AppAccess.ResolveAppRoles(userRoles);

        var entry = new UserRolesCache(userRoles, appRoles);
        memoryCache.Set(cacheKey, entry, TimeSpan.FromMinutes(5));

        return entry;
    }

    public static void Evict(IMemoryCache memoryCache, string userId)
    {
        memoryCache.Remove($"{RoleCachePrefix}{userId}");
    }

    private sealed record UserRolesCache(List<string> UserRoles, List<string> AppRoles);
}