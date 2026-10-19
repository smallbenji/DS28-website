using DS.Application.Services.Groups;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Website.Extensions;
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

            return this.ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] GroupDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreateGroup(data);

            return this.ToActionResult(result);
        }

        [HttpPost("patrol")]
        public async Task<IActionResult> CreatePatrol([FromBody] CreatePatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreatePatrol(data);

            return this.ToActionResult(result);
        }

        [HttpPost("scout")]
        public async Task<IActionResult> CreateScout([FromBody] CreateScoutDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.CreateScout(data);

            return this.ToActionResult(result);
        }

        [HttpPost("scout/add-patrol")]
        public async Task<IActionResult> AddPatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.AddPatrol(data);

            return this.ToActionResult(result);
        }

        [HttpPost("scout/remove-patrol")]
        public async Task<IActionResult> RemovePatrol([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.RemovePatrol(data);

            return this.ToActionResult(result);
        }

        [HttpPost("scout/toggle-leader")]
        public async Task<IActionResult> ToggleLeader([FromBody] ScoutPatrolDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.ToggleLeader(data);

            return this.ToActionResult(result);
        }

        [HttpDelete("patrol/{id}")]
        [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
        public async Task<IActionResult> DeletePatrol(int id)
        {
            var result = await service.DeletePatrol(id);

            return this.ToActionResult(result);
        }

        [HttpDelete("scout/{id}")]
        public async Task<IActionResult> DeleteScout(int id)
        {
            var result = await service.DeleteScout(id);

            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGroup([FromBody] GroupDto data, int id)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.UpdateGroup(data, id);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AppRoles.GroupsDelete))]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var result = await service.DeleteGroup(id);

            return this.ToActionResult(result);
        }
    }
}
