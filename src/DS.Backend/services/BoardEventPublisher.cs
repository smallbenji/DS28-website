using DS.Application.Services.Boards;
using Microsoft.AspNetCore.SignalR;

namespace DS.Backend.Services
{
    public class BoardEventPublisher(IHubContext<BoardHub> hub) : IBoardEventPublisher
    {
        public Task BoardChanged(int boardId)
        {
            return hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("boardChanged", boardId);
        }
    }
}
