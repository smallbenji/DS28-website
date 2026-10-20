using Google.Apis.Drive.v3;

namespace DS.Infrastructure.Google
{
    public interface IGoogleDriveClientFactory
    {
        Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken = default);
    }
}
