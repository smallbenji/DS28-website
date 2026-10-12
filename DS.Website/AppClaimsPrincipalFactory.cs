using DS.Data;
using DS.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DS.Website;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
{
    private readonly DataDbContext dataDb;

    public AppClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        DataDbContext dataDb)
        : base(userManager, roleManager, optionsAccessor)
    {
        this.dataDb = dataDb;
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

        var userRoles = (await UserManager.GetRolesAsync(user)).ToList();

        var positionGroups = await OrgPositionAccess.GetEffectiveGroups(dataDb, user.Id).ToListAsync();

        foreach (var group in positionGroups)
        {
            if (!identity.HasClaim(identity.RoleClaimType, group))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, group));
            }
        }

        foreach (var subRole in AppAccess.ResolveAppRoles(userRoles.Concat(positionGroups)))
        {
            if (!identity.HasClaim(identity.RoleClaimType, subRole))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, subRole));
            }
        }

        return identity;
    }
}
