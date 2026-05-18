using EAPD7111_PART2.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EAPD7111_PART2.Tests.Services
{
    public class CurrencyConversionServiceTests
    {
        private readonly CurrencyConversionService _service;

        public CurrencyConversionServiceTests()
        {
            var httpClient = new HttpClient();
            _service = new CurrencyConversionService(httpClient, NullLogger<CurrencyConversionService>.Instance);
        }

        [Theory]
        [InlineData(100, 18.50, 1850.00)]
        [InlineData(50.25, 19.00, 954.75)]
        [InlineData(0, 18.50, 0)]
        [InlineData(1.50, 20.333, 30.50)]
        public void CalculateZARFromUSD_WithKnownRate_ReturnsCorrectAmount(decimal usd, decimal rate, decimal expectedZar)
        {
            var result = _service.CalculateZARFromUSD(usd, rate);

            Assert.Equal(expectedZar, result);
        }

        [Fact]
        public void CalculateZARFromUSD_WithNegativeUsd_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.CalculateZARFromUSD(-10, 18.50m));
        }

        [Fact]
        public void CalculateZARFromUSD_WithZeroOrNegativeRate_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.CalculateZARFromUSD(100, 0));
            Assert.Throws<ArgumentException>(() => _service.CalculateZARFromUSD(100, -5));
        }

        [Fact]
        public void CalculateZARFromUSD_RoundsToTwoDecimalPlaces()
        {
            var result = _service.CalculateZARFromUSD(10, 18.567m);

            Assert.Equal(185.67m, result);
        }
    }
}
