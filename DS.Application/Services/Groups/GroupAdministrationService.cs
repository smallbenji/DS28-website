using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Groups
{
    public class GroupAdministrationService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<GroupResult> GetGroups()
        {
            var groups = await dataDb.Groups
                .Include(g => g.Patrols).ThenInclude(p => p.Memberships)
                .Include(g => g.Scouts).ThenInclude(s => s.Memberships)
                .Select(x => new GroupDto(x))
                .AsNoTracking()
                .ToListAsync();

            var users = await userManager.Users
                .Include(user => user.Group)
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

            return new GroupResult(GroupOutcome.Success, result);
        }

        public async Task<GroupResult> CreateGroup(GroupDto data)
        {
            dataDb.Groups.Add(new Group(data));

            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> CreatePatrol(CreatePatrolDto data)
        {
            var group = await dataDb.Groups
                .Include(x => x.Patrols)
                .FirstOrDefaultAsync(x => x.Id == data.GroupId);
            if (group == null) return new GroupResult(GroupOutcome.Invalid, "No group exists.");

            var patrol = group.CreatePatrol(new Patrol(data));

            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success, new PatrolDto(patrol));
        }

        public async Task<GroupResult> CreateScout(CreateScoutDto data)
        {
            var group = await dataDb.Groups
                .Include(x => x.Scouts)
                .FirstOrDefaultAsync(x => x.Id == data.GroupId);
            if (group == null) return new GroupResult(GroupOutcome.Invalid, "Group does not exist.");

            var scout = group.CreateScout(new Scout(data));

            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success, new ScoutDto(scout));
        }

        public async Task<GroupResult> AddPatrol(ScoutPatrolDto data)
        {
            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId);
            if (scout == null) return new GroupResult(GroupOutcome.NotFound, "Scout not found.");

            var patrol = await dataDb.Patrols
                .Include(x => x.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId);
            if (patrol == null) return new GroupResult(GroupOutcome.NotFound, "Patrol not found.");

            patrol.AssignScout(scout);

            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> RemovePatrol(ScoutPatrolDto data)
        {
            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId);
            if (scout == null) return new GroupResult(GroupOutcome.NotFound, "Scout not found.");

            var patrol = await dataDb.Patrols
                .Include(x => x.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId);
            if (patrol == null) return new GroupResult(GroupOutcome.NotFound, "Patrol not found.");

            patrol.RemoveScout(scout);

            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> ToggleLeader(ScoutPatrolDto data)
        {
            var membership = await dataDb.PatrolMemberships
                .FirstOrDefaultAsync(pm =>
                    pm.ScoutId == data.ScoutId && pm.PatrolId == data.PatrolId);
            if (membership == null) return new GroupResult(GroupOutcome.NotFound, "Membership not found.");

            membership.IsPatrolLeader = !membership.IsPatrolLeader;
            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success, new PatrolMembershipDto(membership));
        }

        public async Task<GroupResult> DeletePatrol(int id)
        {
            var patrol = await dataDb.Patrols.FindAsync(id);
            if (patrol == null) return new GroupResult(GroupOutcome.NotFound, $"Patrol with ID {id} not found.");

            dataDb.Patrols.Remove(patrol);
            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> DeleteScout(int id)
        {
            var scout = await dataDb.Scouts.FindAsync(id);
            if (scout == null) return new GroupResult(GroupOutcome.NotFound, $"Scout with ID {id} not found.");

            dataDb.Scouts.Remove(scout);
            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> UpdateGroup(GroupDto data, int id)
        {
            if (data.Id != id) return new GroupResult(GroupOutcome.Invalid, "ID mismatch");

            var group = await dataDb.Groups.FindAsync(id);
            if (group == null) return new GroupResult(GroupOutcome.NotFound, $"Group with ID {id} not found.");

            group.Name = data.Name;
            group.District = data.District;
            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }

        public async Task<GroupResult> DeleteGroup(int id)
        {
            var group = await dataDb.Groups.FindAsync(id);
            if (group == null) return new GroupResult(GroupOutcome.NotFound, $"Group with ID {id} not found.");

            dataDb.Groups.Remove(group);
            await dataDb.SaveChangesAsync();

            return new GroupResult(GroupOutcome.Success);
        }
    }
}
