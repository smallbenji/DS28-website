using System.Linq.Expressions;
using DS.Application.Data;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Activities;

public class ActivityTeamService(IDataDbContext dataDb)
{
    public async Task AddTeam(string name, string userId)
    {
        var newTeam = new ActivityTeam
        {
            Name = name
        };

        var adminMembership = new ActivityTeamMembership
        {
            ActivityTeam = newTeam,
            User = await dataDb.Users.FindAsync(userId),
            IsAdmin = true
        };

        await dataDb.ActivityTeams.AddAsync(newTeam);
        await dataDb.ActivityTeamMemberships.AddAsync(adminMembership);

        await dataDb.SaveChangesAsync();
    }

    public async Task<bool> AddMember(int teamId, string userId, bool isAdmin)
    {
        var alreadyMember = await dataDb.ActivityTeamMemberships
            .AnyAsync(m => m.ActivityTeamId == teamId && m.User.Id == userId);
        if (alreadyMember)
        {
            return true;
        }

        var user = await dataDb.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        dataDb.ActivityTeamMemberships.Add(new ActivityTeamMembership
        {
            ActivityTeamId = teamId,
            User = user,
            IsAdmin = isAdmin
        });

        await dataDb.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMember(int teamId, string userId)
        {
            var membership = await dataDb.ActivityTeamMemberships
                .FirstOrDefaultAsync(m => m.ActivityTeamId == teamId && m.User.Id == userId);
            if (membership == null)
            {
                return false;
            }

            dataDb.ActivityTeamMemberships.Remove(membership);
            await dataDb.SaveChangesAsync();
            return true;
        }

    public async Task<List<ActivityTeam>> GetUserActivityTeamAsync(string userId)
    {
        return await GetActivityTeamsQuery(t => t.Memberships.Any(m => m.User.Id == userId));
    }

    public async Task<List<ActivityTeam>> GetAllActivityTeamsAsync()
    {
        return await GetActivityTeamsQuery(t => true);
    }

    public async Task<bool> HasAccessToTeam(string userId, int teamId, bool isAdmin = false)
    {
        return await dataDb.ActivityTeamMemberships.AnyAsync(x => x.ActivityTeamId == teamId && x.User.Id == userId && (!isAdmin || x.IsAdmin));
    }

    public async Task<List<User>> SearchActivityUsersAsync(string searchTerm, int teamId)
    {
        var activityViewGroups = AppAccess.Matrix
            .Where(kvp => kvp.Value.Contains(nameof(AppRoles.ActivityView)))
            .Select(kvp => kvp.Key.ToString());

        var groupRoleIds = dataDb.Roles
            .Where(r => activityViewGroups.Contains(r.Name))
            .Select(r => r.Id);

        var roleUserIds = dataDb.UserRoles
            .Where(ur => groupRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId);

        return await dataDb.Users
            .AsNoTracking()
            .Where(u => roleUserIds.Contains(u.Id))
            .Where(u => !dataDb.ActivityTeamMemberships.Any(m => m.ActivityTeamId == teamId && m.User.Id == u.Id))
            .Where(u => EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchTerm}%")
                || EF.Functions.ILike(u.Email, $"%{searchTerm}%"))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(10)
            .ToListAsync();
    }

    public async Task<Guid?> CreateInvitationAsync(int teamId, string email, bool isAdmin)
        {
            var teamExists = await dataDb.ActivityTeams.AnyAsync(t => t.Id == teamId);
            if (!teamExists)
            {
                return null;
            }

            var invitation = new UserInvitation
            {
                InvitationId = Guid.NewGuid(),
                Email = email,
                Roles = [nameof(AppGroups.ActivityUser)],
                ActivityTeamId = teamId,
                IsAdmin = isAdmin
            };

            dataDb.Invitations.Add(invitation);
            await dataDb.SaveChangesAsync();
            return invitation.InvitationId;
        }

    private async Task<List<ActivityTeam>> GetActivityTeamsQuery(Expression<Func<ActivityTeam, bool>> filter)
    {
        return await dataDb.ActivityTeams
            .AsNoTracking()
            .Where(filter)
            .Where(t => t.DeletedAt == null)
            .Include(t => t.Memberships)
                .ThenInclude(m => m.User)
            .Include(t => t.Activities.Where(a => a.DeletedAt == null)).ThenInclude(a => a.Catalog)
            .Include(t => t.Activities.Where(a => a.DeletedAt == null)).ThenInclude(a => a.Budget)
            .Include(t => t.Activities.Where(a => a.DeletedAt == null)).ThenInclude(a => a.ReviewedBy)
            .ToListAsync();
    }
}