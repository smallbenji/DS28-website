using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.HQ.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/invitation")]
    public class InvitationApiController(DataDbContext dataDb, UserManager<User> userManager) : Controller
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvitation(string id)
        {
            Guid.TryParse(id, out var guidId);
            var result = await dataDb.Invitations.FirstOrDefaultAsync(x => x.InvitationId == guidId);

            if (result == null)
            {
                return NotFound("Invitation not found");
            }

            return Ok(new UserInvitationDto(result));
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> CreateUser([FromBody] UserInvitationCreationDto data, Guid id)
        {
            if (data == null)
            {
                return BadRequest("Invalid request body.");
            }

            var invitation = await dataDb.Invitations.FirstOrDefaultAsync(x => x.InvitationId == id);

            if (invitation == null)
            {
                return NotFound("Invitation not found");
            }

            if (invitation.Used)
            {
                return BadRequest("Invitation has already been used");
            }

            if (string.IsNullOrWhiteSpace(data.Password) || data.Password.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters long");
            }

            if (string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
            {
                return BadRequest("First name and last name are required");
            }

            var newUser = new User
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                UserName = (data.FirstName + data.LastName).Replace(" ", "").ToLower(),
                Email = invitation.Email,
            };

            var result = await userManager.CreateAsync(newUser, data.Password);

            if (!result.Succeeded)
            {
                return BadRequest("Fejl under oprettelse");
            }

            var user = await userManager.FindByEmailAsync(invitation.Email);

            if (user != null)
            {
                await userManager.AddToRolesAsync(user, invitation.Roles);
            }

            if (invitation.ActivityTeamId != null && user != null)
            {
                dataDb.ActivityTeamMemberships.Add(new ActivityTeamMembership
                {
                    ActivityTeamId = invitation.ActivityTeamId.Value,
                    User = user,
                    IsAdmin = invitation.IsAdmin
                });
            }

            invitation.Used = true;

            dataDb.Invitations.Update(invitation);
            await dataDb.SaveChangesAsync();

            return Ok();
        }
    }
}