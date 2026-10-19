using DS.Application.Services.Organization;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/org")]
    public class OrgApiController(OrgService orgService, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetPositions()
        {
            var result = await orgService.GetPositions(GetActor());

            return Respond(result);
        }

        [HttpGet("assignable-groups")]
        public async Task<IActionResult> GetAssignableGroups()
        {
            var result = await orgService.GetAssignableGroups(GetActor());

            return Respond(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePosition([FromBody] OrgPositionCreateDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var result = await orgService.CreatePosition(GetActor(), data);

            return Respond(result);
        }

        [HttpPost("{id:int}/pa")]
        public async Task<IActionResult> CreatePa(int id)
        {
            var result = await orgService.CreatePa(GetActor(), id);

            return Respond(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePosition([FromBody] OrgPositionUpdateDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var result = await orgService.UpdatePosition(GetActor(), data, id);

            return Respond(result);
        }

        [HttpPut("{id:int}/parent")]
        public async Task<IActionResult> MovePosition([FromBody] OrgPositionMoveDto data, int id)
        {
            var result = await orgService.MovePosition(GetActor(), data, id);

            return Respond(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            var result = await orgService.DeletePosition(GetActor(), id);

            return Respond(result);
        }

        [HttpPost("{id:int}/holders")]
        public async Task<IActionResult> AddHolder([FromBody] OrgPositionHolderAddDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.UserId)) return BadRequest("Bruger-id er påkrævet.");

            var result = await orgService.AddHolder(GetActor(), data, id);

            return Respond(result);
        }

        [HttpDelete("{id:int}/holders/{userId}")]
        public async Task<IActionResult> RemoveHolder(int id, string userId)
        {
            var result = await orgService.RemoveHolder(GetActor(), id, userId);

            return Respond(result);
        }

        [HttpPost("{id:int}/invite")]
        public async Task<IActionResult> Invite([FromBody] OrgInviteDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Email)) return BadRequest("Email er påkrævet.");

            var result = await orgService.Invite(GetActor(), data, id);

            return Respond(result);
        }

        private OrgActor GetActor()
        {
            return new OrgActor(userManager.GetUserId(User), User.IsInRole(nameof(AppGroups.SysAdmin)), User.IsInRole(nameof(AppRoles.OrgView)));
        }

        private IActionResult Respond(OrgResult result)
        {
            switch (result.Outcome)
            {
                case OrgOutcome.Success:
                    return result.Value == null ? Ok() : Ok(result.Value);
                case OrgOutcome.Invalid:
                    return BadRequest(result.Value);
                case OrgOutcome.NotFound:
                    return result.Value == null ? NotFound() : NotFound(result.Value);
                default:
                    return Forbid();
            }
        }
    }
}
