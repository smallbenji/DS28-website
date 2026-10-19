using DS.Domain.DTOs;

namespace DS.Application.Services.Google
{
    public interface IGoogleAuthService
    {
        bool IsConfigured { get; }

        Task<GoogleConnectionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);

        string BuildAuthorizationUrl(string state);

        Task ConnectAsync(string code, string userId, CancellationToken cancellationToken = default);

        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }
}
