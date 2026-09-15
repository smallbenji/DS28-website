using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [Route("/api/v1/group-pre-signup")]
    public class GroupPreSignupApiController(DataDbContext dataDb, UserManager<User> userManager, SignInManager<User> signInManager) : Controller
    {
        [HttpGet("{groupId:int}")]
        public async Task<IActionResult> Lookup(int groupId)
        {
            if (!await dataDb.RegistrationSettings.AnyAsync(s => s.Id == 1 && s.IsPreSignupOpen))
            {
                return Conflict("Forhåndstilmeldingen er lukket.");
            }

            var group = await dataDb.Groups
                .AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(g => new { g.Id, g.Name, g.District, AlreadySignedUp = g.PreSignup != null })
                .SingleOrDefaultAsync();

            if (group == null)
            {
                return NotFound("Gruppen blev ikke fundet. Kontrollér gruppenummeret.");
            }

            if (group.AlreadySignedUp)
            {
                return Conflict("Gruppen er allerede forhåndstilmeldt. Log ind eller kontakt gruppens kontaktperson.");
            }

            return Ok(new { group.Id, group.Name, group.District });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GroupPreSignupDto data)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return BadRequest("Log ud, før du opretter en ny bruger til forhåndstilmeldingen.");
            }

            if (data == null || !ModelState.IsValid)
            {
                return BadRequest("Udfyld navn, gyldig email og adgangskode på mindst 4 tegn. Deltagerantal skal være hele tal på 0 eller derover.");
            }

            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            // Keep the setting stable until this registration has been committed.
            await dataDb.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"RegistrationSettings\" WHERE \"Id\" = 1 FOR SHARE");
            if (!await dataDb.RegistrationSettings.AnyAsync(s => s.Id == 1 && s.IsPreSignupOpen))
            {
                return Conflict("Forhåndstilmeldingen er lukket.");
            }

            var group = await dataDb.Groups
                .Include(g => g.PreSignup)
                .SingleOrDefaultAsync(g => g.Id == data.GroupId);
            if (group == null)
            {
                return NotFound("Gruppen blev ikke fundet. Kontrollér gruppenummeret.");
            }

            if (group.PreSignup != null)
            {
                return Conflict("Gruppen er allerede forhåndstilmeldt. Log ind eller kontakt gruppens kontaktperson.");
            }

            var user = new User
            {
                FirstName = data.FirstName.Trim(),
                LastName = data.LastName.Trim(),
                UserName = data.Email.Trim(),
                Email = data.Email.Trim(),
                EmailConfirmed = true,
                Group = group
            };

            try
            {
                var result = await userManager.CreateAsync(user, data.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
                }

                dataDb.GroupPreSignups.Add(new GroupPreSignup
                {
                    GroupId = group.Id,
                    Beaver = data.Beaver,
                    Wolf = data.Wolf,
                    Junior = data.Junior,
                    Trop = data.Trop,
                    Senior = data.Senior,
                    Rover = data.Rover,
                    Leader = data.Leader
                });

                await dataDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync();

                return Conflict("Gruppen eller emailadressen er allerede registreret. Log ind, eller kontrollér oplysningerne.");
            }

            await signInManager.SignInAsync(user, isPersistent: true);

            return Ok(new { ReturnUrl = "/group" });
        }
    }
}
