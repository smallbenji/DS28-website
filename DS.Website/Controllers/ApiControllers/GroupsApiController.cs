using DS.Application.Services.Groups;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers.ApiControllers
{
    [Authorize(Roles = nameof(AppRoles.GroupsView))]
    [Route("/api/v1/groups")]
    public class GroupsApiController(GroupAdministrationService service) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await service.GetGroups();

            return Respond(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] GroupDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreateGroup(data);

            return Respond(result);
        }

        [HttpPost("patrol")]
        public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreatePatrol(data);

            return Respond(result);
        }

        [HttpPost("scout")]
        public async Task<IActionResult> CreateScout([FromBody] CreateScoutDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreateScout(data);

            return Respond(result);
        }

        [HttpPost("scout/add-patrol")]
        public async Task<IActionResult> AddPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.AddPatrol(data);

            return Respond(result);
        }

        [HttpPost("scout/remove-patrol")]
        public async Task<IActionResult> RemovePatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.RemovePatrol(data);

            return Respond(result);
        }

        [HttpPost("scout/toggle-leader")]
        public async Task<IActionResult> ToggleLeader([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.ToggleLeader(data);

            return Respond(result);
        }

        [HttpDelete("patrol/{id}")]
        [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
        public async Task<IActionResult> DeletePatrol(int id)
        {
            var result = await service.DeletePatrol(id);

            return Respond(result);
        }

        [HttpDelete("scout/{id}")]
        public async Task<IActionResult> DeleteScout(int id)
        {
            var result = await service.DeleteScout(id);

            return Respond(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGroup([FromBody] GroupDto data, int id)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.UpdateGroup(data, id);

            return Respond(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var result = await service.DeleteGroup(id);

            return Respond(result);
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
