using GLMS.Shared.Models;
using GLMS.Api.Services;

namespace EAPD7111_PART2.Tests.Services
{
    public class ContractWorkflowServiceTests
    {
        private readonly ContractWorkflowService _service = new();

        [Fact]
        public void CanCreateServiceRequest_WithActiveContract_ReturnsTrue()
        {
            Assert.True(_service.CanCreateServiceRequest(ContractStatus.Active));
        }

        [Theory]
        [InlineData(ContractStatus.Expired)]
        [InlineData(ContractStatus.OnHold)]
        [InlineData(ContractStatus.Draft)]
        public void CanCreateServiceRequest_WithNonActiveContract_ReturnsFalse(ContractStatus status)
        {
            Assert.False(_service.CanCreateServiceRequest(status));
        }

        [Fact]
        public void GetServiceRequestBlockedReason_ForExpiredContract_ReturnsMessage()
        {
            var reason = _service.GetServiceRequestBlockedReason(ContractStatus.Expired);

            Assert.NotNull(reason);
            Assert.Contains("expired", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetServiceRequestBlockedReason_ForOnHoldContract_ReturnsMessage()
        {
            var reason = _service.GetServiceRequestBlockedReason(ContractStatus.OnHold);

            Assert.NotNull(reason);
            Assert.Contains("on hold", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetServiceRequestBlockedReason_ForActiveContract_ReturnsNull()
        {
            var reason = _service.GetServiceRequestBlockedReason(ContractStatus.Active);

            Assert.Null(reason);
        }
    }
}
