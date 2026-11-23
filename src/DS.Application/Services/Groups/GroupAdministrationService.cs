using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Groups
{
    public class GroupAdministrationService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<ServiceResult<GroupsDto>> GetGroups()
        {
            var groups = await dataDb.Groups
                .Include(g => g.Patrols).ThenInclude(p => p.Memberships)
                .Include(g => g.Scouts).ThenInclude(s => s.Memberships)
                .Select(x => new GroupDto(x))
                .AsNoTracking()
                .ToListAsync();

            var users = await userManager.Users
                .Include(user => user.Group)
                .Include(user => user.ProfilePicture)
                .Where(user => user.Group != null)
                .ToListAsync();

            var result = new GroupsDto(groups)
            {
                Users = users
                    .GroupBy(user => user.Group.Id)
                    .ToDictionary(
                        group => group.Key.ToString(),
                        group => group.Select(user => new UserDto(user)).ToList()
                    )
            };

            return new ServiceResult<GroupsDto>(ServiceOutcome.Success, result);
        }

        public async Task<ServiceResult<GroupRegistrationHistoryDto>> GetRegistrationHistory(int groupId)
        {
            if (!await dataDb.Groups.AnyAsync(group => group.Id == groupId))
            {
                return new ServiceResult<GroupRegistrationHistoryDto>(ServiceOutcome.NotFound, Error: "Group not found.");
            }

            var presignups = await dataDb.HistoricPresignups.AsNoTracking()
                .Where(row => row.GroupId == groupId && row.CampYear == 2024)
                .OrderBy(row => row.Category)
                .Select(row => new HistoricPresignupDto
                {
                    CampYear = row.CampYear,
                    Category = row.Category,
                    ParticipantCount = row.ParticipantCount,
                    GuestCount = row.GuestCount
                })
                .ToListAsync();

            var signups = await dataDb.HistoricSignups.AsNoTracking()
                .Where(row => row.GroupId == groupId && row.CampYear == 2024)
                .OrderBy(row => row.Category).ThenBy(row => row.DayIndex)
                .Select(row => new HistoricSignupDto
                {
                    CampYear = row.CampYear,
                    Category = row.Category,
                    DayIndex = row.DayIndex,
                    ParticipantCount = row.ParticipantCount
                })
                .ToListAsync();

            return new ServiceResult<GroupRegistrationHistoryDto>(ServiceOutcome.Success, new GroupRegistrationHistoryDto
            {
                Presignups = presignups,
                Signups = signups
            });
        }

        public async Task<ServiceResult> CreateGroup(GroupDto data)
        {
            dataDb.Groups.Add(new Group(data));

            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<PatrolDto>> CreatePatrol(CreatePatrolDto data)
        {
            var group = await dataDb.Groups
                .Include(x => x.Patrols)
                .FirstOrDefaultAsync(x => x.Id == data.GroupId);
            if (group == null) return new ServiceResult(ServiceOutcome.Invalid, "No group exists.");

            var patrol = group.CreatePatrol(new Patrol(data));

            await dataDb.SaveChangesAsync();

            return new ServiceResult<PatrolDto>(ServiceOutcome.Success, new PatrolDto(patrol));
        }

        public async Task<ServiceResult<ScoutDto>> CreateScout(CreateScoutDto data)
        {
            var group = await dataDb.Groups
                .Include(x => x.Scouts)
                .FirstOrDefaultAsync(x => x.Id == data.GroupId);
            if (group == null) return new ServiceResult(ServiceOutcome.Invalid, "Group does not exist.");

            var scout = group.CreateScout(new Scout(data));

            await dataDb.SaveChangesAsync();

            return new ServiceResult<ScoutDto>(ServiceOutcome.Success, new ScoutDto(scout));
        }

        public async Task<ServiceResult> AddPatrol(ScoutPatrolDto data)
        {
            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, "Scout not found.");

            var patrol = await dataDb.Patrols
                .Include(x => x.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, "Patrol not found.");

            patrol.AssignScout(scout);

            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemovePatrol(ScoutPatrolDto data)
        {
            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, "Scout not found.");

            var patrol = await dataDb.Patrols
                .Include(x => x.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, "Patrol not found.");

            patrol.RemoveScout(scout);

            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<PatrolMembershipDto>> ToggleLeader(ScoutPatrolDto data)
        {
            var membership = await dataDb.PatrolMemberships
                .FirstOrDefaultAsync(pm =>
                    pm.ScoutId == data.ScoutId && pm.PatrolId == data.PatrolId);
            if (membership == null) return new ServiceResult(ServiceOutcome.NotFound, "Membership not found.");

            membership.IsPatrolLeader = !membership.IsPatrolLeader;
            await dataDb.SaveChangesAsync();

            return new ServiceResult<PatrolMembershipDto>(ServiceOutcome.Success, new PatrolMembershipDto(membership));
        }

        public async Task<ServiceResult> DeletePatrol(int id)
        {
            var patrol = await dataDb.Patrols.FindAsync(id);
            if (patrol == null) return new ServiceResult(ServiceOutcome.NotFound, $"Patrol with ID {id} not found.");

            dataDb.Patrols.Remove(patrol);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteScout(int id)
        {
            var scout = await dataDb.Scouts
                .Include(s => s.CampDays)
                .SingleOrDefaultAsync(s => s.Id == id);
            if (scout == null) return new ServiceResult(ServiceOutcome.NotFound, $"Scout with ID {id} not found.");

            dataDb.ScoutCampDays.RemoveRange(scout.CampDays);
            dataDb.Scouts.Remove(scout);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> UpdateGroup(GroupDto data, int id)
        {
            if (data.Id != id) return new ServiceResult(ServiceOutcome.Invalid, "ID mismatch");

            var group = await dataDb.Groups.FindAsync(id);
            if (group == null) return new ServiceResult(ServiceOutcome.NotFound, $"Group with ID {id} not found.");

            group.Name = data.Name;
            group.District = data.District;
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteGroup(int id)
        {
            var group = await dataDb.Groups.FindAsync(id);
            if (group == null) return new ServiceResult(ServiceOutcome.NotFound, $"Group with ID {id} not found.");

            dataDb.Groups.Remove(group);
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }
    }
}
