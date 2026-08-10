using DS.Models;
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
            return await dataDb.ActivityTeamMemberships
                .AsNoTracking()
                .Where(x => x.User.Id == userId)
                .Select(x => x.ActivityTeam)
                .ToListAsync();
        }

        public async Task AddActivity(int teamId, Activity activity)
        {
            activity.ActivityTeamId = teamId;

            await dataDb.Activities.AddAsync(activity);

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
                User = new User { Id = userId },
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
    }
}