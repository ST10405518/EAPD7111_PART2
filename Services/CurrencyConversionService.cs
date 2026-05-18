using System.Net.Http.Json;

namespace EAPD7111_PART2.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CurrencyConversionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public decimal CalculateZARFromUSD(decimal usdAmount, decimal exchangeRate)
        {
            if (usdAmount < 0)
            {
                throw new ArgumentException("USD amount cannot be negative.", nameof(usdAmount));
            }

            if (exchangeRate <= 0)
            {
                throw new ArgumentException("Exchange rate must be greater than zero.", nameof(exchangeRate));
            }

            return Math.Round(usdAmount * exchangeRate, 2);
        }

        public async Task<decimal> ConvertUSDToZARAsync(decimal usdAmount)
        {
            try
            {
                // Using a free exchange rate API
                // In production, you would use a proper API key
                var response = await _httpClient.GetAsync("https://api.exchangerate-api.com/v4/latest/USD");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to get exchange rate. Status code: {response.StatusCode}");
                }

                var data = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                
                if (data == null || data.rates == null || !data.rates.ContainsKey("ZAR"))
                {
                    throw new InvalidOperationException("Invalid exchange rate response.");
                }

                decimal exchangeRate = data.rates["ZAR"];
                return CalculateZARFromUSD(usdAmount, exchangeRate);
            }
            catch (Exception ex)
            {
                // Fallback to a default rate if API fails
                // In production, you might want to log this and handle it differently
                const decimal fallbackRate = 18.50m;
                return CalculateZARFromUSD(usdAmount, fallbackRate);
            }
        }

        private class ExchangeRateResponse
        {
            public string? base_code { get; set; }
            public Dictionary<string, decimal>? rates { get; set; }
        }
    }
}
