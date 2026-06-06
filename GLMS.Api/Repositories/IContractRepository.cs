using GLMS.Shared.Models;

namespace GLMS.Api.Repositories;

public interface IContractRepository
{
    Task<IReadOnlyList<Contract>> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        ContractStatus? status,
        CancellationToken cancellationToken = default);

    Task<Contract?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Contract?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Contract> AddAsync(Contract contract, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default);
    Task DeleteAsync(Contract contract, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
