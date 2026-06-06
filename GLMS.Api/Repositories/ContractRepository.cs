using GLMS.Api.Data;
using GLMS.Api.Services;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly GLMSDbContext _context;

    public ContractRepository(GLMSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Contract>> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        ContractStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = ContractQueryService.ApplyFilters(
            _context.Contracts.Include(c => c.Client),
            startDate,
            endDate,
            status);

        return await query
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Contract?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Contracts.FindAsync([id], cancellationToken);
    }

    public async Task<Contract?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.ServiceRequests)
            .FirstOrDefaultAsync(c => c.ContractId == id, cancellationToken);
    }

    public async Task<Contract> AddAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Contracts.Update(contract);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Contracts.AnyAsync(c => c.ContractId == id, cancellationToken);
    }
}
