using DS.Application.Services.Invitations;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/invitation")]
    public class InvitationApiController(InvitationService invitationService, IMemoryCache memoryCache) : Controller
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvitation(string id)
        {
            var invitation = await invitationService.GetInvitation(id);
            if (invitation == null) return NotFound("Invitation not found");

            return Ok(invitation);
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> CreateUser([FromBody] UserInvitationCreationDto data, Guid id)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await invitationService.CreateUser(data, id);
            if (result.Outcome == InvitationOutcome.NotFound) return NotFound(result.Error);

            if (result.Outcome == InvitationOutcome.Invalid) return BadRequest(result.Error);

            if (result.UserId != null) ClaimsTransformer.Evict(memoryCache, result.UserId);

            return Ok();
        }
    }
}
