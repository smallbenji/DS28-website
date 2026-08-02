using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.Aktibasen.Controllers;

[Authorize]
[Route("/api/v1/team")]
public class TeamApiController(DataDbContext dataDb, TeamPermissions teamPermissions, IKeycloakActivityHelper keycloakHelper) : Controller
{
    public class TeamMemberDTO
    {
        public string UserID { get; set; }
        public string Name { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class TeamActivityDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TeamDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public List<TeamMemberDTO> Members { get; set; }
        public List<TeamActivityDTO> Activities { get; set; }
    }

    public class CreateTeamDTO
    {
        public string Name { get; set; }
    }

    public class AddMemberDTO
    {
        public string UserID { get; set; }
    }

    public class UpdateMemberDTO
    {
        public bool IsAdmin { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var teams = await dataDb.ActivityTeams
            .AsNoTracking()
            .Include(t => t.Activities)
            .Include(t => t.Memberships)
            .ToListAsync();

        var roleMap = await teamPermissions.GetTeamRoleMapAsync(User);
        var users = await keycloakHelper.GetUsers();
        var nameLookup = users
            .GroupBy(u => u.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var retval = teams.Select(t => new TeamDTO
        {
            Id = t.Id,
            Name = t.Name,
            Role = roleMap.TryGetValue(t.Id, out var role) ? role.ToString() : TeamRole.None.ToString(),
            Members = t.Memberships.Select(m => new TeamMemberDTO
            {
                UserID = m.UserID,
                Name = ResolveName(nameLookup, m.UserID),
                IsAdmin = m.IsAdmin
            }).ToList(),
            Activities = t.Activities.Select(a => new TeamActivityDTO
            {
                Id = a.Id,
                Name = a.Name
            }).ToList()
        }).ToList();

        return Ok(retval);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Activity)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDTO data)
    {
        dataDb.ActivityTeams.Add(new ActivityTeam { Name = data.Name });
        await dataDb.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Activity)]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var team = await dataDb.ActivityTeams.FindAsync(id);

        if (team == null)
        {
            return NotFound($"Team with ID {id} not found.");
        }

        dataDb.ActivityTeams.Remove(team);
        await dataDb.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/member")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberDTO data)
    {
        if (!await IsTeamAdmin(id)) return Forbid();

        var teamExists = await dataDb.ActivityTeams.AnyAsync(t => t.Id == id);
        if (!teamExists)
        {
            return NotFound($"Team with ID {id} not found.");
        }

        var users = await keycloakHelper.GetUsers();
        if (!users.Any(u => u.Id == data.UserID))
        {
            return BadRequest("User does not exist.");
        }

        var alreadyMember = await dataDb.ActivityTeamMemberships
            .AnyAsync(m => m.UserID == data.UserID && m.ActivityTeamId == id);
        if (alreadyMember)
        {
            return BadRequest("User is already a member.");
        }

        dataDb.ActivityTeamMemberships.Add(new ActivityTeamMembership
        {
            UserID = data.UserID,
            ActivityTeamId = id,
            IsAdmin = false
        });
        await dataDb.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{id}/member/{userId}")]
    public async Task<IActionResult> UpdateMember(int id, string userId, [FromBody] UpdateMemberDTO data)
    {
        if (!await IsTeamAdmin(id)) return Forbid();

        var membership = await dataDb.ActivityTeamMemberships
            .FirstOrDefaultAsync(m => m.UserID == userId && m.ActivityTeamId == id);

        if (membership == null)
        {
            return NotFound("Membership not found.");
        }

        membership.IsAdmin = data.IsAdmin;
        await dataDb.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}/member/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        if (!await IsTeamAdmin(id)) return Forbid();

        var membership = await dataDb.ActivityTeamMemberships
            .FirstOrDefaultAsync(m => m.UserID == userId && m.ActivityTeamId == id);

        if (membership == null)
        {
            return NotFound("Membership not found.");
        }

        dataDb.ActivityTeamMemberships.Remove(membership);
        await dataDb.SaveChangesAsync();

        return Ok();
    }

    private async Task<bool> IsTeamAdmin(int teamId)
    {
        var role = await teamPermissions.GetTeamRoleAsync(User, teamId);
        return role == TeamRole.Admin;
    }

    private static string ResolveName(Dictionary<string, KcUser> nameLookup, string userId)
    {
        if (nameLookup.TryGetValue(userId, out var user))
        {
            var name = string.Join(" ", new[] { user.FirstName, user.LastName }).Trim();
            if (!string.IsNullOrEmpty(name)) return name;

            if (!string.IsNullOrEmpty(user.UserName)) return user.UserName;
            if (!string.IsNullOrEmpty(user.Email)) return user.Email;
        }

        return userId;
    }
}
