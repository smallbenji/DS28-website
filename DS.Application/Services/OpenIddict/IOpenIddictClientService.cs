using DS.Application.Services;
using DS.Domain.DTOs;

namespace DS.Application.Services.OpenIddict
{
    public interface IOpenIddictClientService
    {
        Task<ServiceResult<List<OpenIddictClientDto>>> GetClients(CancellationToken cancellationToken);

        Task<ServiceResult<OpenIddictClientSavedDto>> CreateClient(OpenIddictClientUpdateDto data, CancellationToken cancellationToken);

        Task<ServiceResult<OpenIddictClientSavedDto>> UpdateClient(string id, OpenIddictClientUpdateDto data, CancellationToken cancellationToken);

        Task<ServiceResult<OpenIddictClientSecretDto>> RotateSecret(string id, CancellationToken cancellationToken);

        Task<ServiceResult> DeleteClient(string id, CancellationToken cancellationToken);
    }
}
