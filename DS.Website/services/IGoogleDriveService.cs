namespace DS.Website.Services
{
    public interface IGoogleDriveService
    {
        Task<List<DriveFileInfo>> GetActivityFilesAsync(int activityId, string folderId, CancellationToken cancellationToken = default);

        Task<string> CreateActivityFileAsync(int activityId, string activityName, string kind, string title, string folderId, CancellationToken cancellationToken = default);
    }

    public class DriveFileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MimeType { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
