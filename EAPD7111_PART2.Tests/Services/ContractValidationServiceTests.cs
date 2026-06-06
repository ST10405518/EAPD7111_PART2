using GLMS.Api.Services;

namespace EAPD7111_PART2.Tests.Services
{
    public class ContractValidationServiceTests
    {
        [Fact]
        public void IsValidDateRange_WhenEndBeforeStart_ReturnsFalse()
        {
            var start = new DateTime(2025, 6, 1);
            var end = new DateTime(2025, 1, 1);

            Assert.False(ContractValidationService.IsValidDateRange(start, end));
        }

        [Fact]
        public void IsValidDateRange_WhenEndEqualsStart_ReturnsTrue()
        {
            var date = new DateTime(2025, 6, 1);

            Assert.True(ContractValidationService.IsValidDateRange(date, date));
        }

        [Fact]
        public void GetDateRangeErrorMessage_WhenInvalid_ReturnsMessage()
        {
            var message = ContractValidationService.GetDateRangeErrorMessage(
                new DateTime(2025, 12, 1),
                new DateTime(2025, 1, 1));

            Assert.NotNull(message);
            Assert.Contains("End date", message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
