using EAPD7111_PART2.Data;
using EAPD7111_PART2.Models;
using Microsoft.EntityFrameworkCore;

namespace EAPD7111_PART2.Services
{
    public class ContractStatusAutomationService : IContractStatusAutomationService
    {
        private readonly GLMSDbContext _context;

        public ContractStatusAutomationService(GLMSDbContext context)
        {
            _context = context;
        }

        public bool ShouldAutoExpire(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null)
        {
            var today = (asOfUtc ?? DateTime.UtcNow).Date;
            return status == ContractStatus.Active && endDate.Date < today;
        }

        public ContractStatus ResolveEffectiveStatus(ContractStatus status, DateTime endDate, DateTime? asOfUtc = null)
        {
            return ShouldAutoExpire(status, endDate, asOfUtc)
                ? ContractStatus.Expired
                : status;
        }

        public async Task<int> ApplyAutomaticStatusUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var contractsToExpire = await _context.Contracts
                .Where(c => c.Status == ContractStatus.Active && c.EndDate.Date < today)
                .ToListAsync(cancellationToken);

            foreach (var contract in contractsToExpire)
            {
                contract.Status = ContractStatus.Expired;
                contract.ModifiedDate = DateTime.UtcNow;
            }

            if (contractsToExpire.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return contractsToExpire.Count;
        }
    }
}
