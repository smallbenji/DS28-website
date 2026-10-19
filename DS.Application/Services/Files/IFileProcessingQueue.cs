namespace DS.Application.Services.Files
{
    public interface IFileProcessingQueue
    {
        void Enqueue(int fileId);
    }
}
