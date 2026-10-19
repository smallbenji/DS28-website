using DS.Application.Services.Groups;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/group/members")]
    public class GroupMembersApiController(GroupMembersService service, UserManager<User> userManager) : Controller
    {
        [HttpPost("invitations")]
        public async Task<IActionResult> InviteAsync([FromBody] InviteGroupMemberDto data)
        {
            if (data == null || !ModelState.IsValid) return BadRequest("Indtast en gyldig email.");

            var result = await service.InviteAsync(userManager.GetUserId(User), data);

            return Respond(result);
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> GetInvitationsAsync()
        {
            var result = await service.GetInvitationsAsync(userManager.GetUserId(User));

            return Respond(result);
        }

        [HttpDelete("invitations/{id:guid}")]
        public async Task<IActionResult> RevokeAsync(Guid id)
        {
            var result = await service.RevokeAsync(userManager.GetUserId(User), id);

            return Respond(result, true);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAsync(string id)
        {
            var result = await service.RemoveAsync(userManager.GetUserId(User), id);

            return Respond(result, true);
        }

        private IActionResult Respond(GroupResult result, bool noContent = false)
        {
            switch (result.Outcome)
            {
                case GroupOutcome.Success:
                    if (noContent) return NoContent();

                    return result.Value == null ? Ok() : Ok(result.Value);
                case GroupOutcome.Invalid:
                    return BadRequest(result.Value);
                case GroupOutcome.NotFound:
                    return result.Value == null ? NotFound() : NotFound(result.Value);
                case GroupOutcome.Forbidden:
                    return Forbid();
                default:
                    return Conflict(result.Value);
            }
        }
    }
}
