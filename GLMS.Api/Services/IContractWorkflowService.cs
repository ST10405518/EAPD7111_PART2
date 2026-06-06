using GLMS.Shared.Models;

namespace GLMS.Api.Services;

public interface IContractWorkflowService
{
    bool CanCreateServiceRequest(ContractStatus status);
    string? GetServiceRequestBlockedReason(ContractStatus status);
}
