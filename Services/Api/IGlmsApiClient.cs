using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace EAPD7111_PART2.Services.Api;

public interface IGlmsApiClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken = default);

    Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken = default);

    Task<Client> CreateClientAsync(Client client, CancellationToken cancellationToken = default);

    Task<Client> UpdateClientAsync(int id, Client client, CancellationToken cancellationToken = default);

    Task DeleteClientAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Contract>> GetContractsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        ContractStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Contract?> GetContractAsync(int id, CancellationToken cancellationToken = default);

    Task<Contract> CreateContractAsync(
        CreateContractDto contract,
        IFormFile? signedAgreement,
        CancellationToken cancellationToken = default);

    Task<Contract> UpdateContractAsync(
        int id,
        Contract contract,
        IFormFile? signedAgreement,
        CancellationToken cancellationToken = default);

    Task DeleteContractAsync(int id, CancellationToken cancellationToken = default);

    Task<(byte[] Content, string FileName, string ContentType)> DownloadContractAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetServiceRequestsAsync(CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetServiceRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceRequest> CreateServiceRequestAsync(
        CreateServiceRequestDto serviceRequest,
        CancellationToken cancellationToken = default);

    Task<ServiceRequest> UpdateServiceRequestAsync(
        int id,
        ServiceRequest serviceRequest,
        CancellationToken cancellationToken = default);

    Task DeleteServiceRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<ExchangeRateResponse> GetExchangeRateAsync(CancellationToken cancellationToken = default);
}
