using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Aktibasen.Controllers;

[Authorize]
[Route("/api/v1/me")]
public class MeApiController(DataDbContext dataDb, TeamPermissions teamPermissions) : Controller
{
    public class MeTeamDTO
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string Role { get; set; }
    }

    public class MeDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActivityAdmin { get; set; }
        public List<MeTeamDTO> Teams { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roleMap = await teamPermissions.GetTeamRoleMapAsync(User);
        var teams = await dataDb.ActivityTeams.AsNoTracking().ToListAsync();

        var retval = new MeDTO
        {
            Id = TeamPermissions.GetUserId(User),
            Name = User.Identity?.Name,
            Email = FindClaim(ClaimTypes.Email) ?? FindClaim("email"),
            IsActivityAdmin = TeamPermissions.IsGlobalActivityAdmin(User),
            Teams = teams
                .Where(t => roleMap.ContainsKey(t.Id))
                .Select(t => new MeTeamDTO
                {
                    TeamId = t.Id,
                    TeamName = t.Name,
                    Role = roleMap[t.Id].ToString()
                })
                .ToList()
        };

        return Ok(retval);
    }

    private string FindClaim(string type)
    {
        return User.FindFirstValue(type);
    }
}
