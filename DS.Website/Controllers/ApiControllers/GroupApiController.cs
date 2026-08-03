using DS.Models;
using DS.Website;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.HQ.Controllers;

[Authorize(Roles = nameof(AppRoles.GroupView))]
[Route("/api/v1/group")]
public class GroupApiController : Controller
{
    private readonly DataDbContext dataDb;
    private readonly UserManager<User> userManager;

    public GroupApiController(DataDbContext dataDb, UserManager<User> userManager)
    {
        this.dataDb = dataDb;
        this.userManager = userManager;
    }

    public class GroupDTO
    {
        public GroupDTO(List<GroupSummaryDTO> groups)
        {
            Groups = groups;
        }

        public List<GroupSummaryDTO> Groups { get; set; }
        public Dictionary<string, List<UserSummaryDTO>> Users { get; set; } = new();
    }

    public class UserSummaryDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class GroupSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<PatrolSummaryDTO> Patrols { get; set; } = new();
        public List<ScoutSummaryDTO> Scouts { get; set; } = new();
    }

    public class PatrolSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GroupId { get; set; }
        public List<PatrolMembershipSummaryDTO> Memberships { get; set; } = new();
    }

    public class ScoutSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public Gender Gender { get; set; }
        public int GroupId { get; set; }
        public List<PatrolMembershipSummaryDTO> Memberships { get; set; } = new();
    }

    public class PatrolMembershipSummaryDTO
    {
        public int Id { get; set; }
        public int ScoutId { get; set; }
        public int PatrolId { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsPatrolLeader { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groups = await dataDb.Groups
            .Select(group => new GroupSummaryDTO
            {
                Id = group.Id,
                Name = group.Name,
                Patrols = group.Patrols.Select(patrol => new PatrolSummaryDTO
                {
                    Id = patrol.Id,
                    Name = patrol.Name,
                    GroupId = patrol.GroupId,
                    Memberships = patrol.Memberships.Select(membership => new PatrolMembershipSummaryDTO
                    {
                        Id = membership.Id,
                        ScoutId = membership.ScoutId,
                        PatrolId = membership.PatrolId,
                        JoinedDate = membership.JoinedDate,
                        IsPatrolLeader = membership.IsPatrolLeader
                    }).ToList()
                }).ToList(),
                Scouts = group.Scouts.Select(scout => new ScoutSummaryDTO
                {
                    Id = scout.Id,
                    Name = scout.Name,
                    Birthday = scout.Birthday,
                    Gender = scout.Gender,
                    GroupId = scout.GroupId,
                    Memberships = scout.Memberships.Select(membership => new PatrolMembershipSummaryDTO
                    {
                        Id = membership.Id,
                        ScoutId = membership.ScoutId,
                        PatrolId = membership.PatrolId,
                        JoinedDate = membership.JoinedDate,
                        IsPatrolLeader = membership.IsPatrolLeader
                    }).ToList()
                }).ToList()
            })
            .ToListAsync();

        var users = await userManager.Users
            .Include(user => user.Group)
            .Where(user => user.Group != null)
            .ToListAsync();

        var retval = new GroupDTO(groups)
        {
            Users = users
                .GroupBy(user => user.Group.Id!)
                .ToDictionary(
                    group => group.Key.ToString(),
                    group => group.Select(user => new UserSummaryDTO
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        Roles = []
                    }).ToList())
        };

        return Ok(retval);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] Group data)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

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

    [HttpDelete("patrol/{id}")]
    [Authorize(Roles = nameof(AppRoles.GroupDelete))]
    public async Task<IActionResult> DeletePatrol(int id)
    {
        var patrol = await dataDb.Patrols.FindAsync(id);
        if (patrol == null)
        {
            return NotFound($"Patrol with ID {id} not found.");
        }

        dataDb.Patrols.Remove(patrol);
        await dataDb.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("scout/{id}")]
    public async Task<IActionResult> DeleteScout(int id)
    {
        var scout = await dataDb.Scouts.FindAsync(id);
        if (scout == null)
        {
            return NotFound($"Scout with ID {id} not found.");
        }

        dataDb.Scouts.Remove(scout);
        await dataDb.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup([FromBody] Group data, int id)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        if (data.Id != id)
        {
            return BadRequest("ID mismatch");
        }

        dataDb.Groups.Update(data);
        await dataDb.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(AppRoles.GroupDelete))]
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
