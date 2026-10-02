using DS.Data;
using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Route("/api/v1/group")]
    [Authorize]
    // [Authorize(Roles = nameof(AppRoles.AuditLogView))]
    public class GroupApiController(UserManager<User> userManager, DataDbContext dataDb) : Controller
    {
        [HttpGet("pre-signup")]
        public async Task<IActionResult> GetPreSignup()
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

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

            return Ok(new
            {
                GroupName = user.Group.Name,
                GroupId = user.Group.Id,
                Counts = new UpdateGroupPreSignupDto(signup)
            });
        }

        [HttpPut("pre-signup")]
        public async Task<IActionResult> UpdatePreSignup([FromBody] UpdateGroupPreSignupDto data)
        {
            if (data == null || !ModelState.IsValid)
            {
                return BadRequest("Udfyld alle deltagerantal med hele tal på 0 eller derover.");
            }

            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            await dataDb.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM \"RegistrationSettings\" WHERE \"Id\" = 1 FOR SHARE");
            if (!await dataDb.RegistrationSettings.AnyAsync(
                s => s.Id == 1 && s.IsPreSignupOpen))
            {
                return Conflict("Forhåndstilmeldingen er lukket. Deltagerantallene kan ikke ændres.");
            }

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

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

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(HttpContext.User);
            var user = await userManager.Users
                .Include(x => x.Group)
                .ThenInclude(x => x.Patrols).ThenInclude(p => p.Memberships)
                .Include(x => x.Group)
                .ThenInclude(x => x.Scouts).ThenInclude(s => s.Memberships)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound();

            var users = await userManager.Users
                .Where(u => u.Group != null && u.Group.Id == user.Group.Id)
                .ToListAsync();

            return Ok(new GroupDto(user.Group, users));
        }

        [HttpPost("scouts")]
        public async Task<IActionResult> CreateScout([FromBody] CreateScoutDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name)) return BadRequest("Udfyld spejderens navn.");

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.Scouts)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            data.GroupId = user.Group.Id;
            var scout = user.Group.CreateScout(new Scout(data));

            await dataDb.SaveChangesAsync();

            return Ok(new ScoutDto(scout));
        }

        [HttpDelete("scouts/{scoutId:int}")]
        public async Task<IActionResult> DeleteScout(int scoutId)
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == scoutId && s.GroupId == user.Group.Id);
            if (scout == null) return NotFound("Spejderen blev ikke fundet.");

            dataDb.Scouts.Remove(scout);
            await dataDb.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("patrols")]
        public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name)) return BadRequest("Udfyld patruljens navn.");

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.Patrols)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            data.GroupId = user.Group.Id;
            var patrol = user.Group.CreatePatrol(new Patrol(data));

            await dataDb.SaveChangesAsync();

            return Ok(new PatrolDto(patrol));
        }

        [HttpDelete("patrols/{patrolId:int}")]
        public async Task<IActionResult> DeletePatrol(int patrolId)
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            var patrol = await dataDb.Patrols
                .FirstOrDefaultAsync(p => p.Id == patrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return NotFound("Patruljen blev ikke fundet.");

            dataDb.Patrols.Remove(patrol);
            await dataDb.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("scouts/add-patrol")]
        public async Task<IActionResult> AddScoutToPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Ugyldig anmodning.");

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId && s.GroupId == user.Group.Id);
            if (scout == null) return NotFound("Spejderen blev ikke fundet.");

            var patrol = await dataDb.Patrols
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return NotFound("Patruljen blev ikke fundet.");

            patrol.AssignScout(scout);
            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("scouts/remove-patrol")]
        public async Task<IActionResult> RemoveScoutFromPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Ugyldig anmodning.");

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Group == null) return NotFound("Din bruger er ikke tilknyttet en gruppe.");

            var scout = await dataDb.Scouts
                .FirstOrDefaultAsync(s => s.Id == data.ScoutId && s.GroupId == user.Group.Id);
            if (scout == null) return NotFound("Spejderen blev ikke fundet.");

            var patrol = await dataDb.Patrols
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.Id == data.PatrolId && p.GroupId == user.Group.Id);
            if (patrol == null) return NotFound("Patruljen blev ikke fundet.");

            patrol.RemoveScout(scout);
            await dataDb.SaveChangesAsync();

            return NoContent();
        }
    }
}
