using DS.Application.Services.Boards;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/boards")]
    public class BoardApiController(BoardService service, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetBoards()
        {
            var result = await service.GetBoards(GetActor());

            return Respond(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            var result = await service.GetBoard(GetActor(), id);

            return Respond(result);
        }

        [HttpGet("{id:int}/columns")]
        public async Task<IActionResult> GetColumns(int id)
        {
            var result = await service.GetColumns(GetActor(), id);

            return Respond(result);
        }

        [HttpGet("assignees")]
        public async Task<IActionResult> SearchAssignees([FromQuery] string search, [FromQuery] int boardId)
        {
            var result = await service.SearchAssignees(GetActor(), search, boardId);

            return Respond(result);
        }

        [HttpGet("{id:int}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var result = await service.GetMembers(GetActor(), id);

            return Respond(result);
        }

        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMember([FromBody] BoardMemberCreateDto data, int id)
        {
            var result = await service.AddMember(GetActor(), data, id);

            return Respond(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] BoardCreateDto data)
        {
            var result = await service.CreateBoard(GetActor(), data);

            return Respond(result);
        }

        [HttpPost("{id:int}/colors")]
        public async Task<IActionResult> CreateColor([FromBody] BoardColorUpdateDto data, int id)
        {
            var result = await service.CreateColor(GetActor(), data, id);

            return Respond(result);
        }

        [HttpPut("{id:int}/colors/{colorId:int}")]
        public async Task<IActionResult> UpdateColor([FromBody] BoardColorUpdateDto data, int id, int colorId)
        {
            var result = await service.UpdateColor(GetActor(), data, id, colorId);

            return Respond(result);
        }

        [HttpDelete("{id:int}/colors/{colorId:int}")]
        public async Task<IActionResult> DeleteColor(int id, int colorId)
        {
            var result = await service.DeleteColor(GetActor(), id, colorId);

            return Respond(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBoard([FromBody] BoardUpdateDto data, int id)
        {
            var result = await service.UpdateBoard(GetActor(), data, id);

            return Respond(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var result = await service.DeleteBoard(GetActor(), id);

            return Respond(result);
        }

        [HttpPost("{id:int}/columns")]
        public async Task<IActionResult> CreateColumn([FromBody] BoardColumnCreateDto data, int id)
        {
            var result = await service.CreateColumn(GetActor(), data, id);

            return Respond(result);
        }

        [HttpPut("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> UpdateColumn([FromBody] BoardColumnUpdateDto data, int id, int columnId)
        {
            var result = await service.UpdateColumn(GetActor(), data, id, columnId);

            return Respond(result);
        }

        [HttpDelete("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> DeleteColumn(int id, int columnId)
        {
            var result = await service.DeleteColumn(GetActor(), id, columnId);

            return Respond(result);
        }

        [HttpPut("{id:int}/columns/reorder")]
        public async Task<IActionResult> ReorderColumns([FromBody] List<int> columnOrder, int id)
        {
            var result = await service.ReorderColumns(GetActor(), columnOrder, id);

            return Respond(result);
        }

        [HttpPost("{id:int}/cards")]
        public async Task<IActionResult> CreateCard([FromBody] WorkItemCreateDto data, int id)
        {
            var result = await service.CreateCard(GetActor(), data, id);

            return Respond(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> UpdateCard([FromBody] WorkItemUpdateDto data, int id, int workItemId)
        {
            var result = await service.UpdateCard(GetActor(), data, id, workItemId);

            return Respond(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/move")]
        public async Task<IActionResult> MoveCard([FromBody] WorkItemMoveDto data, int id, int workItemId)
        {
            var result = await service.MoveCard(GetActor(), data, id, workItemId);

            return Respond(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> RemoveCard(int id, int workItemId)
        {
            var result = await service.RemoveCard(GetActor(), id, workItemId);

            return Respond(result);
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/placements")]
        public async Task<IActionResult> AddCardPlacement([FromBody] WorkItemPlacementCreateDto data, int id, int workItemId)
        {
            var result = await service.AddCardPlacement(GetActor(), data, id, workItemId);

            return Respond(result);
        }

        private BoardActor GetActor()
        {
            return new BoardActor(userManager.GetUserId(User), User.IsInRole(nameof(AppGroups.SysAdmin)) || User.IsInRole(nameof(AppGroups.CampAdmin)));
        }

        private IActionResult Respond(BoardResult result)
        {
            switch (result.Outcome)
            {
                case BoardOutcome.Success:
                    return result.Value == null ? Ok() : Ok(result.Value);
                case BoardOutcome.Invalid:
                    return result.Value == null ? BadRequest() : BadRequest(result.Value);
                case BoardOutcome.NotFound:
                    return NotFound();
                default:
                    return Forbid();
            }
        }
    }
}
