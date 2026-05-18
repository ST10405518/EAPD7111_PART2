using EAPD7111_PART2.Models;

namespace EAPD7111_PART2.Services
{
    public interface IContractStatusAutomationService
    {
        bool ShouldAutoExpire(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null);
        ContractStatus ResolveEffectiveStatus(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null);
        Task<int> ApplyAutomaticStatusUpdatesAsync(CancellationToken cancellationToken = default);
    }
}
