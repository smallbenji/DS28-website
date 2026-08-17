using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DS.Website;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
{
    public AppClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        if (!string.IsNullOrWhiteSpace(fullName) && !identity.HasClaim(c => c.Type == "full_name"))
        {
            identity.AddClaim(new Claim("full_name", fullName));
        }

        var userRoles = await UserManager.GetRolesAsync(user);
        foreach (var subRole in AppAccess.ResolveAppRoles(userRoles))
        {
            if (!identity.HasClaim(identity.RoleClaimType, subRole))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, subRole));
            }
        }

        return identity;
    }
}