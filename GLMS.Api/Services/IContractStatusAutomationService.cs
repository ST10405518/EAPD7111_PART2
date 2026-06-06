using GLMS.Shared.Models;

namespace GLMS.Api.Services;

public interface IContractStatusAutomationService
{
    bool ShouldAutoExpire(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null);
    ContractStatus ResolveEffectiveStatus(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null);
    Task<int> ApplyAutomaticStatusUpdatesAsync(CancellationToken cancellationToken = default);
}
