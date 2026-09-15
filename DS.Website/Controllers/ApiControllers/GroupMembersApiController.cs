using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/group/members")]
    public class GroupMembersApiController(DataDbContext dataDb, UserManager<User> userManager) : Controller
    {
        private Task<User> GetCurrentUserAsync()
        {
            return userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u => u.Id == userManager.GetUserId(User));
        }

        [HttpPost("invitations")]
        public async Task<IActionResult> InviteAsync([FromBody] InviteGroupMemberDto data)
        {
            if (data == null || !ModelState.IsValid)
            {
                return BadRequest("Indtast en gyldig email.");
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Group == null)
            {
                return Forbid();
            }

            var email = data.Email.Trim();
            var existingUser = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u => u.NormalizedEmail == userManager.NormalizeEmail(email));
            if (existingUser?.Group != null)
            {
                return BadRequest("Brugeren er allerede tilknyttet en gruppe.");
            }

            var invitation = new UserInvitation
            {
                InvitationId = Guid.NewGuid(),
                Email = email,
                Roles = [],
                GroupId = currentUser.Group.Id
            };

            dataDb.Invitations.Add(invitation);
            await dataDb.SaveChangesAsync();

            return Ok(new { Path = $"/group-invitation/{invitation.InvitationId}" });
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> GetInvitationsAsync()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Group == null)
            {
                return Forbid();
            }

            return Ok(await dataDb.Invitations
                .Where(i => i.GroupId == currentUser.Group.Id && !i.Used)
                .Select(i => new { i.InvitationId, i.Email })
                .ToListAsync());
        }

        [HttpDelete("invitations/{id:guid}")]
        public async Task<IActionResult> RevokeAsync(Guid id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Group == null)
            {
                return Forbid();
            }

            var updatedCount = await dataDb.Invitations
                .Where(i => i.GroupId == currentUser.Group.Id && i.InvitationId == id && !i.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Used, true));

            return updatedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAsync(string id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Group == null)
            {
                return Forbid();
            }

            if (currentUser.Id == id)
            {
                return BadRequest("Du kan ikke fjerne dig selv fra gruppen.");
            }

            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            await dataDb.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Groups\" WHERE \"Id\" = {currentUser.Group.Id} FOR UPDATE");
            if (!await userManager.Users.AnyAsync(u => u.Id == currentUser.Id && u.Group.Id == currentUser.Group.Id))
            {
                return Forbid();
            }

            var member = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u => u.Id == id && u.Group.Id == currentUser.Group.Id);
            if (member == null)
            {
                return NotFound("Brugeren er ikke medlem af din gruppe.");
            }

            member.Group = null;
            dataDb.Entry(member).Property("GroupId").CurrentValue = null;
            await dataDb.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
    }
}
