using System.Text.Json;

namespace GLMS.Api.Services;

public class CurrencyConversionService : ICurrencyConversionService
{
    public const decimal FallbackUsdToZarRate = 18.50m;

    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(HttpClient httpClient, ILogger<CurrencyConversionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "GLMS/1.0");
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

    public async Task<decimal> GetUsdToZarRateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
            if (!response.IsSuccessStatusCode)
            {
                return FallbackUsdToZarRate;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var rates = jsonDoc.RootElement.GetProperty("rates");

            if (rates.TryGetProperty("ZAR", out var zarRate))
            {
                return zarRate.GetDecimal();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching USD to ZAR exchange rate");
        }

        return FallbackUsdToZarRate;
    }

    public async Task<decimal> ConvertUSDToZARAsync(decimal usdAmount)
    {
        var exchangeRate = await GetUsdToZarRateAsync();
        return CalculateZARFromUSD(usdAmount, exchangeRate);
    }
}
