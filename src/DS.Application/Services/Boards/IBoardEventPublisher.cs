namespace DS.Application.Services.Boards
{
    public interface IBoardEventPublisher
    {
        Task BoardChanged(int boardId);
    }
}
