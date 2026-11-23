using DS.Application.Services.Boards;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/v1/boards")]
    public class BoardApiController(BoardService service, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetBoards()
        {
            var result = await service.GetBoards(GetActor());

            return this.ToActionResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            var result = await service.GetBoard(GetActor(), id);

            return this.ToActionResult(result);
        }

        [HttpGet("{id:int}/columns")]
        public async Task<IActionResult> GetColumns(int id)
        {
            var result = await service.GetColumns(GetActor(), id);

            return this.ToActionResult(result);
        }

        [HttpGet("assignees")]
        public async Task<IActionResult> SearchAssignees([FromQuery] string search, [FromQuery] int boardId)
        {
            var result = await service.SearchAssignees(GetActor(), search, boardId);

            return this.ToActionResult(result);
        }

        [HttpGet("cards/search")]
        public async Task<IActionResult> SearchCards([FromQuery] string search)
        {
            var result = await service.SearchCards(GetActor(), search);

            return this.ToActionResult(result);
        }

        [HttpGet("{id:int}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var result = await service.GetMembers(GetActor(), id);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMember([FromBody] BoardMemberCreateDto data, int id)
        {
            var result = await service.AddMember(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, string userId)
        {
            var result = await service.RemoveMember(GetActor(), id, userId);

            return this.ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] BoardCreateDto data)
        {
            var result = await service.CreateBoard(GetActor(), data);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/tags")]
        public async Task<IActionResult> CreateTag([FromBody] BoardTagUpdateDto data, int id)
        {
            var result = await service.CreateTag(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/tags/{tagId:int}")]
        public async Task<IActionResult> UpdateTag([FromBody] BoardTagUpdateDto data, int id, int tagId)
        {
            var result = await service.UpdateTag(GetActor(), data, id, tagId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/tags/{tagId:int}")]
        public async Task<IActionResult> DeleteTag(int id, int tagId)
        {
            var result = await service.DeleteTag(GetActor(), id, tagId);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/colors")]
        public async Task<IActionResult> CreateColor([FromBody] BoardColorUpdateDto data, int id)
        {
            var result = await service.CreateColor(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/colors/{colorId:int}")]
        public async Task<IActionResult> UpdateColor([FromBody] BoardColorUpdateDto data, int id, int colorId)
        {
            var result = await service.UpdateColor(GetActor(), data, id, colorId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/colors/{colorId:int}")]
        public async Task<IActionResult> DeleteColor(int id, int colorId)
        {
            var result = await service.DeleteColor(GetActor(), id, colorId);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBoard([FromBody] BoardUpdateDto data, int id)
        {
            var result = await service.UpdateBoard(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var result = await service.DeleteBoard(GetActor(), id);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/columns")]
        public async Task<IActionResult> CreateColumn([FromBody] BoardColumnCreateDto data, int id)
        {
            var result = await service.CreateColumn(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> UpdateColumn([FromBody] BoardColumnUpdateDto data, int id, int columnId)
        {
            var result = await service.UpdateColumn(GetActor(), data, id, columnId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> DeleteColumn(int id, int columnId)
        {
            var result = await service.DeleteColumn(GetActor(), id, columnId);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/columns/reorder")]
        public async Task<IActionResult> ReorderColumns([FromBody] List<int> columnOrder, int id)
        {
            var result = await service.ReorderColumns(GetActor(), columnOrder, id);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/cards")]
        public async Task<IActionResult> CreateCard([FromBody] WorkItemCreateDto data, int id)
        {
            var result = await service.CreateCard(GetActor(), data, id);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> UpdateCard([FromBody] WorkItemUpdateDto data, int id, int workItemId)
        {
            var result = await service.UpdateCard(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/move")]
        public async Task<IActionResult> MoveCard([FromBody] WorkItemMoveDto data, int id, int workItemId)
        {
            var result = await service.MoveCard(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> RemoveCard(int id, int workItemId)
        {
            var result = await service.RemoveCard(GetActor(), id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/placements")]
        public async Task<IActionResult> AddCardPlacement([FromBody] WorkItemPlacementCreateDto data, int id, int workItemId)
        {
            var result = await service.AddCardPlacement(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/assignees")]
        public async Task<IActionResult> AddAssignee([FromBody] WorkItemAssigneeCreateDto data, int id, int workItemId)
        {
            var result = await service.AddAssignee(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/assignees/{userId}")]
        public async Task<IActionResult> RemoveAssignee(int id, int workItemId, string userId)
        {
            var result = await service.RemoveAssignee(GetActor(), id, workItemId, userId);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/completion")]
        public async Task<IActionResult> CompleteCard(int id, int workItemId)
        {
            var result = await service.SetCompletion(GetActor(), id, workItemId, completed: true);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/completion")]
        public async Task<IActionResult> ReopenCard(int id, int workItemId)
        {
            var result = await service.SetCompletion(GetActor(), id, workItemId, completed: false);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/dependencies")]
        public async Task<IActionResult> AddDependency([FromBody] WorkItemDependencyCreateDto data, int id, int workItemId)
        {
            var result = await service.AddDependency(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/dependencies/{otherWorkItemId:int}")]
        public async Task<IActionResult> RemoveDependency(int id, int workItemId, int otherWorkItemId)
        {
            var result = await service.RemoveDependency(GetActor(), id, workItemId, otherWorkItemId);

            return this.ToActionResult(result);
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/pull-requests")]
        public async Task<IActionResult> AddPullRequest([FromBody] PullRequestLinkDto data, int id, int workItemId)
        {
            var result = await service.UpdatePullRequestLink(GetActor(), id, workItemId, data?.Url, remove: false);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/pull-requests")]
        public async Task<IActionResult> RemovePullRequest([FromQuery] string url, int id, int workItemId)
        {
            var result = await service.UpdatePullRequestLink(GetActor(), id, workItemId, url, remove: true);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/epic")]
        public async Task<IActionResult> MarkAsEpic(int id, int workItemId)
        {
            var result = await service.SetEpicMarking(GetActor(), id, workItemId, isEpic: true);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/epic")]
        public async Task<IActionResult> UnmarkAsEpic(int id, int workItemId)
        {
            var result = await service.SetEpicMarking(GetActor(), id, workItemId, isEpic: false);

            return this.ToActionResult(result);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/epic-link")]
        public async Task<IActionResult> SetCardEpic([FromBody] WorkItemEpicLinkDto data, int id, int workItemId)
        {
            var result = await service.SetCardEpic(GetActor(), data, id, workItemId);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}/epic-link")]
        public async Task<IActionResult> RemoveCardEpic(int id, int workItemId)
        {
            var result = await service.RemoveCardEpic(GetActor(), id, workItemId);

            return this.ToActionResult(result);
        }

        private BoardActor GetActor()
        {
            var isSysAdmin = User.IsInRole(nameof(AppGroups.SysAdmin));
            return new BoardActor(userManager.GetUserId(User), isSysAdmin || User.IsInRole(nameof(AppGroups.CampAdmin)), isSysAdmin);
        }
    }
}
