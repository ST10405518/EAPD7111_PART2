using EAPD7111_PART2.Models;
using EAPD7111_PART2.Services;

namespace EAPD7111_PART2.Tests.Services
{
    public class ContractStatusAutomationServiceTests
    {
        [Fact]
        public void ShouldAutoExpire_ActiveContractPastEndDate_ReturnsTrue()
        {
            var service = new ContractStatusAutomationService(null!);
            var endDate = new DateTime(2024, 1, 1);
            var asOf = new DateTime(2025, 1, 1);

            Assert.True(service.ShouldAutoExpire(ContractStatus.Active, endDate, asOf));
        }

        [Fact]
        public void ShouldAutoExpire_ActiveContractBeforeEndDate_ReturnsFalse()
        {
            var service = new ContractStatusAutomationService(null!);
            var endDate = new DateTime(2026, 12, 31);
            var asOf = new DateTime(2025, 1, 1);

            Assert.False(service.ShouldAutoExpire(ContractStatus.Active, endDate, asOf));
        }

        [Fact]
        public void ResolveEffectiveStatus_ExpiredByDate_ReturnsExpired()
        {
            var service = new ContractStatusAutomationService(null!);
            var status = service.ResolveEffectiveStatus(
                ContractStatus.Active,
                new DateTime(2024, 6, 1),
                new DateTime(2025, 1, 1));

            Assert.Equal(ContractStatus.Expired, status);
        }

        [Fact]
        public void ResolveEffectiveStatus_OnHoldContract_RemainsOnHold()
        {
            var service = new ContractStatusAutomationService(null!);
            var status = service.ResolveEffectiveStatus(
                ContractStatus.OnHold,
                new DateTime(2020, 1, 1),
                new DateTime(2025, 1, 1));

            Assert.Equal(ContractStatus.OnHold, status);
        }
    }
}
