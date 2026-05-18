using EAPD7111_PART2.Models;

namespace EAPD7111_PART2.Services
{
    public static class ContractQueryService
    {
        public static IQueryable<Contract> ApplyFilters(
            IQueryable<Contract> contracts,
            DateTime? startDate,
            DateTime? endDate,
            ContractStatus? status)
        {
            if (startDate.HasValue)
            {
                contracts = contracts.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                contracts = contracts.Where(c => c.EndDate <= endDate.Value);
            }

            if (status.HasValue)
            {
                contracts = contracts.Where(c => c.Status == status.Value);
            }

            return contracts;
        }
    }
}
