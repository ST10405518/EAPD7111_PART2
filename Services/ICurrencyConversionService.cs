namespace EAPD7111_PART2.Services
{
    public interface ICurrencyConversionService
    {
        Task<decimal> ConvertUSDToZARAsync(decimal usdAmount);
        decimal CalculateZARFromUSD(decimal usdAmount, decimal exchangeRate);
    }
}
