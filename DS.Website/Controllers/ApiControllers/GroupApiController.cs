using DS.Application.Services.Groups;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Route("/api/v1/group")]
    [Authorize]
    public class GroupApiController(GroupService service, UserManager<User> userManager) : Controller
    {
        [HttpGet("pre-signup")]
        public async Task<IActionResult> GetPreSignup()
        {
            var result = await service.GetPreSignup(userManager.GetUserId(User));

            return Respond(result);
        }

        [HttpPut("pre-signup")]
        public async Task<IActionResult> UpdatePreSignup([FromBody] UpdateGroupPreSignupDto data)
        {
            if (data == null || !ModelState.IsValid) return BadRequest("Udfyld alle deltagerantal med hele tal på 0 eller derover.");

            var result = await service.UpdatePreSignup(userManager.GetUserId(User), data);

            return Respond(result, true);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await service.GetGroup(userManager.GetUserId(User));

            return Respond(result);
        }

        [HttpPost("scouts")]
        public async Task<IActionResult> CreateScout([FromBody] CreateScoutDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name)) return BadRequest("Udfyld spejderens navn.");

            var result = await service.CreateScout(userManager.GetUserId(User), data);

            return Respond(result);
        }

        [HttpDelete("scouts/{scoutId:int}")]
        public async Task<IActionResult> DeleteScout(int scoutId)
        {
            var result = await service.DeleteScout(userManager.GetUserId(User), scoutId);

            return Respond(result, true);
        }

        [HttpPost("patrols")]
        public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name)) return BadRequest("Udfyld patruljens navn.");

            var result = await service.CreatePatrol(userManager.GetUserId(User), data);

            return Respond(result);
        }

        [HttpDelete("patrols/{patrolId:int}")]
        public async Task<IActionResult> DeletePatrol(int patrolId)
        {
            var result = await service.DeletePatrol(userManager.GetUserId(User), patrolId);

            return Respond(result, true);
        }

        [HttpPost("scouts/add-patrol")]
        public async Task<IActionResult> AddScoutToPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Ugyldig anmodning.");

            var result = await service.AddScoutToPatrol(userManager.GetUserId(User), data);

            return Respond(result);
        }

        [HttpPost("scouts/remove-patrol")]
        public async Task<IActionResult> RemoveScoutFromPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Ugyldig anmodning.");

            var result = await service.RemoveScoutFromPatrol(userManager.GetUserId(User), data);

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
