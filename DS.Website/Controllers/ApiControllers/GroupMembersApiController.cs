using DS.Application.Services.Groups;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Website.Extensions;
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

            return this.ToActionResult(result);
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> GetInvitationsAsync()
        {
            var result = await service.GetInvitationsAsync(userManager.GetUserId(User));

            return this.ToActionResult(result);
        }

        [HttpDelete("invitations/{id:guid}")]
        public async Task<IActionResult> RevokeAsync(Guid id)
        {
            var result = await service.RevokeAsync(userManager.GetUserId(User), id);

            return this.ToActionResult(result, noContent: true);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAsync(string id)
        {
            var result = await service.RemoveAsync(userManager.GetUserId(User), id);

            return this.ToActionResult(result, noContent: true);
        }
    }
}
