using DS.Domain.DTOs;

namespace DS.Application.Services.OpenIddict
{
    public interface IOpenIddictClientService
    {
        Task<OpenIddictClientResult> GetClients(CancellationToken cancellationToken);

        Task<OpenIddictClientResult> CreateClient(OpenIddictClientUpdateDto data, CancellationToken cancellationToken);

        Task<OpenIddictClientResult> UpdateClient(string id, OpenIddictClientUpdateDto data, CancellationToken cancellationToken);

        Task<OpenIddictClientResult> RotateSecret(string id, CancellationToken cancellationToken);

        Task<OpenIddictClientResult> DeleteClient(string id, CancellationToken cancellationToken);
    }
}
