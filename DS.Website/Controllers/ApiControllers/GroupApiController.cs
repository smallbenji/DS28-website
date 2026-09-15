using DS.DTOs;
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
                .AsNoTracking()
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user?.Group == null)
            {
                return NotFound("Din bruger er ikke tilknyttet en gruppe.");
            }

            var signup = user.Group.PreSignup;
            if (signup == null)
            {
                return NotFound("Gruppen har endnu ingen forhåndstilmelding.");
            }

            return Ok(new
            {
                GroupName = user.Group.Name,
                GroupId = user.Group.Id,
                Counts = new UpdateGroupPreSignupDto
                {
                    Beaver = signup.Beaver,
                    Wolf = signup.Wolf,
                    Junior = signup.Junior,
                    Trop = signup.Trop,
                    Senior = signup.Senior,
                    Rover = signup.Rover,
                    Leader = signup.Leader
                }
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

            await dataDb.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"RegistrationSettings\" WHERE \"Id\" = 1 FOR SHARE");
            if (!await dataDb.RegistrationSettings.AnyAsync(s => s.Id == 1 && s.IsPreSignupOpen))
            {
                return Conflict("Forhåndstilmeldingen er lukket. Deltagerantallene kan ikke ændres.");
            }

            var userId = userManager.GetUserId(User);
            var user = await userManager.Users
                .Include(u => u.Group).ThenInclude(g => g.PreSignup)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user?.Group == null)
            {
                return NotFound("Din bruger er ikke tilknyttet en gruppe.");
            }

            var signup = user.Group.PreSignup;
            if (signup == null)
            {
                return NotFound("Gruppen har endnu ingen forhåndstilmelding.");
            }

            signup.Beaver = data.Beaver;
            signup.Wolf = data.Wolf;
            signup.Junior = data.Junior;
            signup.Trop = data.Trop;
            signup.Senior = data.Senior;
            signup.Rover = data.Rover;
            signup.Leader = data.Leader;
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
                    .ThenInclude(x => x.Patrols)
                .Include(x => x.Group)
                    .ThenInclude(x => x.Scouts)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Group == null)
            {
                return NotFound();
            }

            var users = await userManager.Users
                .Where(u => u.Group != null && u.Group.Id == user.Group.Id)
                .ToListAsync();

            return Ok(new GroupDto(user.Group, users));
        }
    }
}
