using GLMS.Shared.Models;
using GLMS.Api.Services;

namespace EAPD7111_PART2.Tests.Services
{
    public class ContractQueryServiceTests
    {
        private static readonly List<Contract> SampleContracts =
        [
            new Contract
            {
                ContractId = 1,
                ContractNumber = "C-001",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                Status = ContractStatus.Active,
                ServiceLevel = "Gold"
            },
            new Contract
            {
                ContractId = 2,
                ContractNumber = "C-002",
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 12, 31),
                Status = ContractStatus.Expired,
                ServiceLevel = "Silver"
            },
            new Contract
            {
                ContractId = 3,
                ContractNumber = "C-003",
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 9, 30),
                Status = ContractStatus.OnHold,
                ServiceLevel = "Bronze"
            }
        ];

        [Fact]
        public void ApplyFilters_ByStatus_ReturnsMatchingContracts()
        {
            var result = ContractQueryService.ApplyFilters(
                SampleContracts.AsQueryable(),
                null,
                null,
                ContractStatus.Active).ToList();

            Assert.Single(result);
            Assert.Equal("C-001", result[0].ContractNumber);
        }

        [Fact]
        public void ApplyFilters_ByDateRange_ReturnsContractsWithinRange()
        {
            var result = ContractQueryService.ApplyFilters(
                SampleContracts.AsQueryable(),
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31),
                null).ToList();

            Assert.Single(result);
            Assert.Equal("C-001", result[0].ContractNumber);
        }

        [Fact]
        public void ApplyFilters_WithNoFilters_ReturnsAllContracts()
        {
            var result = ContractQueryService.ApplyFilters(
                SampleContracts.AsQueryable(),
                null,
                null,
                null).ToList();

            Assert.Equal(3, result.Count);
        }
    }
}
