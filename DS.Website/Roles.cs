using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace DS.Website
{
    public enum AppGroups
    {
        SysAdmin,
        User,
    }

    public enum AppRoles
    {
        UsersView,
        UsersCreate,
        UsersDelete,
    }

    public static class AppAccess
    {
        // Vi ændrer værdien til string[], så vi bruger lynhurtige kompiler-konstanter via nameof()
        public static readonly Dictionary<AppGroups, string[]> Matrix = new()
        {
            {
                AppGroups.SysAdmin,
                [
                    nameof(AppRoles.UsersView),
                    nameof(AppRoles.UsersCreate),
                    nameof(AppRoles.UsersDelete),
                ]
            },
            {
                AppGroups.User,
                [
                    nameof(AppRoles.UsersView)
                ]
            }
        };
    }

    public class GroupClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var clone = principal.Clone();
            var newIdentity = clone.Identity as ClaimsIdentity;

            if (newIdentity == null) return Task.FromResult(principal);

            // 1. Find alle grupper brugeren har i forvejen
            var userGroups = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value); // Vi dropper .ToList() for at spare en tildeling

            // 2. Map grupperne direkte til strenge uden .ToString() i loopet
            foreach (var groupName in userGroups)
            {
                if (Enum.TryParse<AppGroups>(groupName, out var group))
                {
                    if (AppAccess.Matrix.TryGetValue(group, out var subRoles))
                    {
                        foreach (var subRoleName in subRoles)
                        {
                            // Sørg for ikke at tilføje dubletter
                            if (!newIdentity.HasClaim(ClaimTypes.Role, subRoleName))
                            {
                                newIdentity.AddClaim(new Claim(ClaimTypes.Role, subRoleName));
                            }
                        }
                    }
                }
            }

            return Task.FromResult(clone);
        }
    }
}