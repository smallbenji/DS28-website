using DS.Application.Services.Boards;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DS.Backend.Services
{
    [Authorize]
    public class BoardHub(BoardService boardService, UserManager<User> userManager) : Hub
    {
        public async Task JoinBoard(int boardId)
        {
            var actor = new BoardActor(
                userManager.GetUserId(Context.User),
                Context.User.IsInRole(nameof(AppGroups.SysAdmin)) || Context.User.IsInRole(nameof(AppGroups.CampAdmin)));
            if (!await boardService.VisibleBoards(actor).AnyAsync(board => board.Id == boardId)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));
        }

        public static string GroupName(int boardId)
        {
            return $"board-{boardId}";
        }
    }
}
