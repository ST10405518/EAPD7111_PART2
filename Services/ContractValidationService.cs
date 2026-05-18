namespace EAPD7111_PART2.Services
{
    public static class ContractValidationService
    {
        public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
        {
            return endDate.Date >= startDate.Date;
        }

        public static string? GetDateRangeErrorMessage(DateTime startDate, DateTime endDate)
        {
            return IsValidDateRange(startDate, endDate)
                ? null
                : "End date must be on or after the start date.";
        }
    }
}
