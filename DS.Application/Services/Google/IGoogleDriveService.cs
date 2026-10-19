namespace DS.Application.Services.Google
{
    public interface IGoogleDriveService
    {
        Task<List<DriveFileInfo>> GetActivityFilesAsync(int activityId, string folderId, CancellationToken cancellationToken = default);

        Task<string> CreateActivityFileAsync(int activityId, string activityName, string kind, string title, string folderId, CancellationToken cancellationToken = default);
    }

}
