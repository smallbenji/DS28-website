using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers;

[Authorize(Roles = nameof(AppRoles.GroupsView))]
[Route("/api/v1/groups")]
public class GroupsApiController : Controller
{
    private readonly DataDbContext dataDb;
    private readonly UserManager<User> userManager;

    public GroupsApiController(DataDbContext dataDb, UserManager<User> userManager)
    {
        this.dataDb = dataDb;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
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

        var retval = new GroupsDto(groups)
        {
            Users = users
                .GroupBy(user => user.Group.Id)
                .ToDictionary(
                    group => group.Key.ToString(),
                    group => group.Select(user => new UserDto(user)).ToList()
                )
        };

        return Ok(retval);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] GroupDto data)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        dataDb.Groups.Add(new Group
        {
            Id = data.Id,
            Name = data.Name,
            District = data.District
        });
        await dataDb.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("patrol")]
    public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDto data)
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

        return Ok(new PatrolDto(patrol));
    }

    [HttpPost("scout")]
    public async Task<IActionResult> CreateScout([FromBody] CreateScoutDto data)
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

        return Ok(new ScoutDto(scout));
    }

    [HttpPost("scout/add-patrol")]
    public async Task<IActionResult> AddPatrol([FromBody] ScoutPatrolDto data)
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
    public async Task<IActionResult> RemovePatrol([FromBody] ScoutPatrolDto data)
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

    [HttpPost("scout/toggle-leader")]
    public async Task<IActionResult> ToggleLeader([FromBody] ScoutPatrolDto data)
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

        return Ok(new PatrolMembershipDto(membership));
    }

    [HttpDelete("patrol/{id}")]
    [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
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
    public async Task<IActionResult> UpdateGroup([FromBody] GroupDto data, int id)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        if (data.Id != id)
        {
            return BadRequest("ID mismatch");
        }

        var group = await dataDb.Groups.FindAsync(id);
        if (group == null)
        {
            return NotFound($"Group with ID {id} not found.");
        }

        group.Name = data.Name;
        group.District = data.District;
        await dataDb.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
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
