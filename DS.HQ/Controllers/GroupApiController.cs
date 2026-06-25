using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("/api/v1/group")]
    public class GroupApiController(DataDbContext dataDb, IKeycloakHelper keycloakHelper) : Controller
    {
        public class GroupDTO
        {
            public GroupDTO(List<Group> groups)
            {
                Groups = groups;
            }

            public List<Group> Groups { get; set; }
            public Dictionary<string, List<DSUser>> Users { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = dataDb.Groups
                .Include(x => x.Patrols)
                .Include(x => x.Scouts)
                    .ThenInclude(s => s.Memberships)
                .ToList();

            var retval = new GroupDTO(data)
            {
                Users = (await keycloakHelper.GetUsers()).Where(x => !string.IsNullOrEmpty(x.GroupNumber)).GroupBy(x => x.GroupNumber).ToDictionary(x => x.Key, x => x.ToList())
            };

            return Ok(retval);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] Group data)
        {
            dataDb.Groups.Add(data);

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        public class CreatePatrolDTO
        {
            public string Name { get; set; }
            public int GroupId { get; set; }
        }

        [HttpPost("patrol")]
        public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDTO data)
        {
            var groupExists = await dataDb.Groups.AnyAsync(g => g.Id == data.GroupId);
            if (!groupExists)
            {
                return BadRequest("Group does not exist.");
            }

            var patrol = new Patrol
            {
                Name = data.Name,
                GroupId = data.GroupId
            };

            dataDb.Patrols.Add(patrol);
            await dataDb.SaveChangesAsync();

            return Ok(patrol);
        }

        public class CreateScoutDTO
        {
            public string Name { get; set; }
            public DateTime Birthday { get; set; }
            public Gender Gender { get; set; }
            public int GroupId { get; set; }
        }

        [HttpPost("scout")]
        public async Task<IActionResult> CreateScout([FromBody] CreateScoutDTO data)
        {
            var groupExists = await dataDb.Groups.AnyAsync(g => g.Id == data.GroupId);
            if (!groupExists)
            {
                return BadRequest("Group does not exist.");
            }

            var scout = new Scout
            {
                Name = data.Name,
                Birthday = DateTime.SpecifyKind(data.Birthday, DateTimeKind.Utc),
                Gender = data.Gender,
                GroupId = data.GroupId
            };

            dataDb.Scouts.Add(scout);
            await dataDb.SaveChangesAsync();

            return Ok(scout);
        }

        public class ScoutPatrolDTO
        {
            public int ScoutId { get; set; }
            public int PatrolId { get; set; }
        }

        [HttpPost("scout/add-patrol")]
        public async Task<IActionResult> AddPatrol([FromBody] ScoutPatrolDTO data)
        {
            var scoutExists = await dataDb.Scouts.AnyAsync(s => s.Id == data.ScoutId);
            if (!scoutExists)
            {
                return NotFound("Scout not found.");
            }

            var patrolExists = await dataDb.Patrols.AnyAsync(p => p.Id == data.PatrolId);
            if (!patrolExists)
            {
                return NotFound("Patrol not found.");
            }

            var alreadyMember = await dataDb.PatrolMemberships.AnyAsync(pm => pm.ScoutId == data.ScoutId && pm.PatrolId == data.PatrolId);
            if (alreadyMember)
            {
                return Ok();
            }

            var membership = new PatrolMembership
            {
                ScoutId = data.ScoutId,
                PatrolId = data.PatrolId,
                JoinedDate = DateTime.UtcNow,
                IsPatrolLeader = false
            };

            dataDb.PatrolMemberships.Add(membership);
            await dataDb.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("scout/remove-patrol")]
        public async Task<IActionResult> RemovePatrol([FromBody] ScoutPatrolDTO data)
        {
            var membership = await dataDb.PatrolMemberships
                .FirstOrDefaultAsync(pm => pm.ScoutId == data.ScoutId && pm.PatrolId == data.PatrolId);

            if (membership != null)
            {
                dataDb.PatrolMemberships.Remove(membership);
                await dataDb.SaveChangesAsync();
            }

            return Ok();
        }

        public class ToggleLeaderDTO
        {
            public int ScoutId { get; set; }
            public int PatrolId { get; set; }
        }

        [HttpPost("scout/toggle-leader")]
        public async Task<IActionResult> ToggleLeader([FromBody] ToggleLeaderDTO data)
        {
            var membership = await dataDb.PatrolMemberships
                .FirstOrDefaultAsync(pm => pm.ScoutId == data.ScoutId && pm.PatrolId == data.PatrolId);

            if (membership == null)
            {
                return NotFound("Membership not found.");
            }

            membership.IsPatrolLeader = !membership.IsPatrolLeader;
            await dataDb.SaveChangesAsync();

            return Ok(membership);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGroup([FromBody] Group data, int id)
        {
            if (data.Id != id)
            {
                return BadRequest("ID mismatch");
            }

            dataDb.Groups.Update(data);

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var group = await dataDb.Groups.FindAsync(id);

            if (group == null)
            {
                return NotFound($"Group with ID {id} not found.");
            }

            dataDb.Groups.Remove(group);
            await dataDb.SaveChangesAsync();

            return Ok();
        }
    }
}