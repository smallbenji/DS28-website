using DS.DTOs;
using DS.Models;
using DS.Website;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Repositories
{
    public class ActivityRepository(DataDbContext dataDb)
    {
        public async Task<List<Activity>> GetUserActivitiesAsync(string userId)
        {
            return await dataDb.ActivityTeamMemberships
                .AsNoTracking()
                .Where(m => m.User.Id == userId)
                .SelectMany(m => m.ActivityTeam.Activities)
                .Include(a => a.Budget)
                .Include(a => a.Catalog)
                .ToListAsync();
        }

        public async Task<List<ActivityTeam>> GetUserActivityTeamAsync(string userId)
        {
            return await GetActivityTeamsQuery(t => t.Memberships.Any(m => m.User.Id == userId));
        }

        public async Task<List<ActivityTeam>> GetAllActivityTeamsAsync()
        {
            return await GetActivityTeamsQuery(t => true);
        }

        private async Task<List<ActivityTeam>> GetActivityTeamsQuery(System.Linq.Expressions.Expression<Func<ActivityTeam, bool>> filter)
        {
            return await dataDb.ActivityTeams
                .AsNoTracking()
                .Where(filter)
                .Include(t => t.Memberships)
                    .ThenInclude(m => m.User)
                .Include(t => t.Activities)
                .ToListAsync();
        }

        public async Task AddActivity(int teamId, Activity activity)
        {
            activity.ActivityTeamId = teamId;

            await dataDb.Activities.AddAsync(activity);

            await dataDb.SaveChangesAsync();
        }

        public async Task<Activity> GetActivityAsync(int activityId)
        {
            return await dataDb.Activities
                .AsNoTracking()
                .Include(a => a.Budget)
                .Include(a => a.Catalog)
                .FirstOrDefaultAsync(a => a.Id == activityId);
        }

        public async Task UpdateActivityAsync(int activityId, ActivityDto data)
        {
            var activity = await dataDb.Activities
                .Include(a => a.Catalog)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
            {
                return;
            }

            activity.Name = data.Name;
            activity.Budget = new ActivityBudget { Budget = data.Budget };

            if (data.Catalog != null)
            {
                activity.Catalog ??= new CatalogData();
                activity.Catalog.Name = data.Catalog.Name;
                activity.Catalog.Summary = data.Catalog.Summary;
                activity.Catalog.Description = data.Catalog.Description;
            }

            await dataDb.SaveChangesAsync();
        }

        public async Task AddTeamAsync(string name, string userId)
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

        public async Task<bool> AddMemberAsync(int teamId, string userId, bool isAdmin)
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

        public async Task<bool> RemoveMemberAsync(int teamId, string userId)
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
    }
}