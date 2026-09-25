using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/group-invitations")]
    public class GroupInvitationApiController(
        DataDbContext dataDb,
        UserManager<User> userManager,
        SignInManager<User> signInManager) : Controller
    {
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetInvitationAsync(Guid id)
        {
            var invitation = await dataDb.Invitations
                .Include(i => i.Group)
                .SingleOrDefaultAsync(i =>
                    i.InvitationId == id &&
                    i.GroupId != null &&
                    !i.Used);
            if (invitation?.Group == null)
            {
                return NotFound("Invitationen findes ikke eller er allerede brugt eller annulleret.");
            }

            return Ok(new
            {
                invitation.Email,
                GroupName = invitation.Group.Name,
                ExistingUser = await userManager.FindByEmailAsync(invitation.Email) != null
            });
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> AcceptAsync(Guid id, [FromBody] UserInvitationCreationDto data)
        {
            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            var claimedCount = await dataDb.Invitations
                .Where(i => i.InvitationId == id && i.GroupId != null && !i.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Used, true));
            if (claimedCount != 1)
            {
                return NotFound("Invitationen findes ikke eller er allerede brugt eller annulleret.");
            }

            var invitation = await dataDb.Invitations
                .Include(i => i.Group)
                .SingleAsync(i => i.InvitationId == id);
            if (invitation.Group == null) return NotFound("Gruppen findes ikke længere.");

            var user = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u =>
                    u.NormalizedEmail == userManager.NormalizeEmail(invitation.Email));

            var isNewUser = user == null;
            if (!isNewUser)
            {
                if (userManager.GetUserId(User) != user.Id)
                {
                    return Unauthorized("Log ind med den inviterede email for at acceptere.");
                }

                var joinedCount = await userManager.Users
                    .Where(u => u.Id == user.Id && u.Group == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        u => EF.Property<int?>(u, "GroupId"),
                        invitation.GroupId));
                if (joinedCount != 1)
                {
                    return BadRequest("Du er allerede tilknyttet en gruppe.");
                }
            }
            else
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    return BadRequest("Log ud, før du opretter en konto med den inviterede email.");
                }

                if (data == null ||
                    string.IsNullOrWhiteSpace(data.FirstName) ||
                    string.IsNullOrWhiteSpace(data.LastName) ||
                    string.IsNullOrWhiteSpace(data.Password))
                {
                    return BadRequest("Udfyld navn og adgangskode.");
                }

                user = new User
                {
                    FirstName = data.FirstName.Trim(),
                    LastName = data.LastName.Trim(),
                    Email = invitation.Email,
                    UserName = invitation.Email,
                    EmailConfirmed = true,
                    Group = invitation.Group
                };

                var result = await userManager.CreateAsync(user, data.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
                }
            }

            await transaction.CommitAsync();

            if (isNewUser)
            {
                await signInManager.SignInAsync(user, isPersistent: true);
            }

            return Ok();
        }
    }
}
