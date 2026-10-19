using DS.Domain.DTOs;
using Google.Apis.Drive.v3;

namespace DS.Website.Services
{
    public interface IGoogleAuthService
    {
        bool IsConfigured { get; }

        Task<GoogleConnectionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);

        string BuildAuthorizationUrl(string state);

        Task ConnectAsync(string code, string userId, CancellationToken cancellationToken = default);

        Task DisconnectAsync(CancellationToken cancellationToken = default);

        Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken = default);
    }
}
