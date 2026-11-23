using System.Data.Common;
using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Groups
{
    public class GroupService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        private const string UnknownCampDayMessage = "En eller flere af de valgte dage er ikke lejrdage.";

        public async Task<ServiceResult<object>> GetPreSignup(string userId)
        {
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var signup = user.Group.PreSignup;
            if (signup == null)
            {
                signup = new GroupPreSignup
                {
                    GroupId = user.Group.Id
                };
                dataDb.GroupPreSignups.Add(signup);
                await dataDb.SaveChangesAsync();
            }

            return new ServiceResult<object>(ServiceOutcome.Success, new
            {
                GroupName = user.Group.Name,
                GroupId = user.Group.Id,
                Counts = new UpdateGroupPreSignupDto(signup)
            });
        }

        public async Task<ServiceResult> UpdatePreSignup(string userId, UpdateGroupPreSignupDto data)
        {
            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            if (!await dataDb.IsPreSignupOpenAsync(lockSettings: true))
            {
                return new ServiceResult(ServiceOutcome.Conflict, "Forhåndstilmeldingen er lukket. Deltagerantallene kan ikke ændres.");
            }

            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var signup = user.Group.PreSignup;
            if (signup == null)
            {
                signup = new GroupPreSignup
                {
                    GroupId = user.Group.Id
                };
                dataDb.GroupPreSignups.Add(signup);
            }

            data.ApplyTo(signup);

            await dataDb.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<GroupDto>> GetGroup(string userId)
        {
            var user = await userManager.Users
                .Include(x => x.Group)
                .ThenInclude(x => x.Patrols).ThenInclude(p => p.Memberships)
                .Include(x => x.Group)
                .ThenInclude(x => x.Scouts).ThenInclude(s => s.Memberships)
                .Include(x => x.Group)
                .ThenInclude(x => x.Scouts).ThenInclude(s => s.CampDays)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound);

            var users = await userManager.Users
                .Include(u => u.ProfilePicture)
                .Where(u => u.Group != null && u.Group.Id == user.Group.Id)
                .ToListAsync();

            return new ServiceResult<GroupDto>(ServiceOutcome.Success, new GroupDto(user.Group, users));
        }

        public async Task<ServiceResult<ScoutDto>> CreateScout(string userId, CreateScoutDto data)
        {
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.Scouts)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var campDayIds = data.CampDayIds?.Distinct().ToList() ?? [];
            if (!await CampDaysExist(campDayIds)) return new ServiceResult(ServiceOutcome.Invalid, UnknownCampDayMessage);

            data.GroupId = user.Group.Id;
            var scout = user.Group.CreateScout(new Scout(data));
            scout.SetCampDays(campDayIds);

            await dataDb.SaveChangesAsync();

            return new ServiceResult<ScoutDto>(ServiceOutcome.Success, new ScoutDto(scout));
        }

        public async Task<ServiceResult> DeleteScout(string userId, int scoutId)
        {
            var user = await userManager.GetUserWithGroupAsync(userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .Include(s => s.CampDays)
                .FirstOrDefaultAsync(s => s.Id == scoutId && s.GroupId == user.Group.Id);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, "Spejderen blev ikke fundet.");

            dataDb.ScoutCampDays.RemoveRange(scout.CampDays);
            dataDb.Scouts.Remove(scout);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<object>> SetScoutCampDays(string userId, SetScoutCampDaysDto data)
        {
            var scoutIds = data.ScoutIds?.Distinct().ToList() ?? [];
            var campDayIds = data.CampDayIds?.Distinct().ToList() ?? [];
            if (scoutIds.Count == 0) return new ServiceResult(ServiceOutcome.Invalid, "Vælg mindst én spejder.");
            if (campDayIds.Count == 0) return new ServiceResult(ServiceOutcome.Invalid, "Vælg mindst én dag. En spejder, der ikke deltager, skal slettes.");

            var user = await userManager.GetUserWithGroupAsync(userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            if (!await CampDaysExist(campDayIds)) return new ServiceResult(ServiceOutcome.Invalid, UnknownCampDayMessage);

            var scouts = await dataDb.Scouts
                .Include(s => s.CampDays)
                .Where(s => s.GroupId == user.Group.Id && scoutIds.Contains(s.Id))
                .ToListAsync();
            if (scouts.Count != scoutIds.Count) return new ServiceResult(ServiceOutcome.NotFound, "En eller flere spejdere blev ikke fundet.");

            var updated = scouts
                .Where(s => !data.OnlyScoutsWithoutDays || s.CampDays.Count == 0)
                .ToList();
            foreach (var scout in updated) scout.SetCampDays(campDayIds);

            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" or "23503" })
            {
                dataDb.ChangeTracker.Clear();
                return new ServiceResult(ServiceOutcome.Conflict, "Dagene blev ændret samtidig af en anden. Hent siden igen, og prøv igen.");
            }

            return new ServiceResult<object>(ServiceOutcome.Success, new { Updated = updated.Count });
        }

        private async Task<bool> CampDaysExist(List<int> campDayIds)
        {
            if (campDayIds.Count == 0) return true;

            var found = await dataDb.CampDays.CountAsync(day => campDayIds.Contains(day.Id));

            return found == campDayIds.Count;
        }

        public async Task<ServiceResult<PatrolDto>> CreatePatrol(string userId, CreatePatrolDto data)
        {
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.Patrols)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            data.GroupId = user.Group.Id;
            var patrol = user.Group.CreatePatrol(new Patrol(data));

            await dataDb.SaveChangesAsync();

            return new ServiceResult<PatrolDto>(ServiceOutcome.Success, new PatrolDto(patrol));
        }

        public async Task<ServiceResult> DeletePatrol(string userId, int patrolId)
        {
            var user = await userManager.GetUserWithGroupAsync(userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var patrol = await dataDb.Patrols
                .FirstOrDefaultAsync(p => p.Id == patrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, "Patruljen blev ikke fundet.");

            dataDb.Patrols.Remove(patrol);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> AddScoutToPatrol(string userId, ScoutPatrolDto data)
        {
            var user = await userManager.GetUserWithGroupAsync(userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId && s.GroupId == user.Group.Id);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, "Spejderen blev ikke fundet.");

            var patrol = await dataDb.Patrols
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, "Patruljen blev ikke fundet.");

            patrol.AssignScout(scout);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveScoutFromPatrol(string userId, ScoutPatrolDto data)
        {
            var user = await userManager.GetUserWithGroupAsync(userId);
            if (user?.Group == null) return new ServiceResult(ServiceOutcome.NotFound, "Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId && s.GroupId == user.Group.Id);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, "Spejderen blev ikke fundet.");

            var patrol = await dataDb.Patrols
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, "Patruljen blev ikke fundet.");

            patrol.RemoveScout(scout);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }
    }
}
