using EAPD7111_PART2.Models;

namespace EAPD7111_PART2.Services
{
    public interface IContractWorkflowService
    {
        bool CanCreateServiceRequest(ContractStatus status);
        string? GetServiceRequestBlockedReason(ContractStatus status);
    }

}
