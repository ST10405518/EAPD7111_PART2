using EAPD7111_PART2.Models;

namespace EAPD7111_PART2.Services
{
    public class ContractWorkflowService : IContractWorkflowService
    {
        public bool CanCreateServiceRequest(ContractStatus status)
        {
            return status == ContractStatus.Active;
        }

        public string? GetServiceRequestBlockedReason(ContractStatus status)
        {
            if (CanCreateServiceRequest(status))
            {
                return null;
            }

            return status switch
            {
                ContractStatus.Expired => "Cannot create service request for an expired contract.",
                ContractStatus.OnHold => "Cannot create service request while the contract is on hold.",
                ContractStatus.Draft => "Cannot create service request for a draft contract. Activate the contract first.",
                _ => $"Cannot create service request for contract with status: {status}."
            };
        }
    }
}
