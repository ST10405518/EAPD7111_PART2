namespace GLMS.Api.Services;

public interface ICurrencyConversionService
{
    Task<decimal> GetUsdToZarRateAsync();
    Task<decimal> ConvertUSDToZARAsync(decimal usdAmount);
    decimal CalculateZARFromUSD(decimal usdAmount, decimal exchangeRate);
}
