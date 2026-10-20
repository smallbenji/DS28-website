using DS.Application.Services.Invitations;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("api/v1/group-invitations")]
    public class GroupInvitationApiController(GroupInvitationService invitationService, UserManager<User> userManager, SignInManager<User> signInManager) : Controller
    {
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetInvitationAsync(Guid id)
        {
            var invitation = await invitationService.GetInvitationAsync(id);
            if (invitation == null) return NotFound("Invitationen findes ikke eller er allerede brugt eller annulleret.");

            return Ok(invitation);
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> AcceptAsync(Guid id, [FromBody] UserInvitationCreationDto data)
        {
            var result = await invitationService.AcceptAsync(id, data, userManager.GetUserId(User), User.Identity?.IsAuthenticated == true);
            if (result.Outcome == InvitationOutcome.NotFound) return NotFound(result.Error);

            if (result.Outcome == InvitationOutcome.Unauthorized) return Unauthorized(result.Error);

            if (result.Outcome == InvitationOutcome.Invalid) return BadRequest(result.Error);

            if (result.NewUser != null) await signInManager.SignInAsync(result.NewUser, isPersistent: true);

            return Ok();
        }
    }
}
