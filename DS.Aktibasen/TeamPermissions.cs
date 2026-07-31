using System.Security.Claims;
using DS.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Aktibasen;

public enum TeamRole
{
    None,
    Member,
    Admin
}

public class TeamPermissions(DataDbContext dataDb)
{
    public static string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static bool IsGlobalActivityAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(Role.Activity);
    }

    public async Task<TeamRole> GetTeamRoleAsync(ClaimsPrincipal user, int teamId)
    {
        if (IsGlobalActivityAdmin(user)) return TeamRole.Admin;

        var userId = GetUserId(user);
        if (string.IsNullOrEmpty(userId)) return TeamRole.None;

        var membership = await dataDb.ActivityTeamMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == userId && m.ActivityTeamId == teamId);

        return membership?.IsAdmin == true ? TeamRole.Admin
            : membership != null ? TeamRole.Member
            : TeamRole.None;
    }

    public async Task<Dictionary<int, TeamRole>> GetTeamRoleMapAsync(ClaimsPrincipal user)
    {
        var map = new Dictionary<int, TeamRole>();

        if (IsGlobalActivityAdmin(user))
        {
            var teamIds = await dataDb.ActivityTeams.AsNoTracking().Select(t => t.Id).ToListAsync();
            foreach (var id in teamIds) map[id] = TeamRole.Admin;
            return map;
        }

        var userId = GetUserId(user);
        if (string.IsNullOrEmpty(userId)) return map;

        var memberships = await dataDb.ActivityTeamMemberships
            .AsNoTracking()
            .Where(m => m.UserID == userId)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            map[membership.ActivityTeamId] = membership.IsAdmin ? TeamRole.Admin : TeamRole.Member;
        }

        return map;
    }

    public async Task<TeamRole> GetActivityTeamRoleAsync(ClaimsPrincipal user, int activityId)
    {
        var activity = await dataDb.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return TeamRole.None;

        return await GetTeamRoleAsync(user, activity.ActivityTeamId);
    }
}
