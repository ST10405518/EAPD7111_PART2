using GLMS.Api.Data;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly GLMSDbContext _context;

    public ServiceRequestRepository(GLMSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests
            .Include(s => s.Contract)
            .ThenInclude(c => c!.Client)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests.FindAsync([id], cancellationToken);
    }

    public async Task<ServiceRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests
            .Include(s => s.Contract)
            .ThenInclude(c => c!.Client)
            .FirstOrDefaultAsync(s => s.ServiceRequestId == id, cancellationToken);
    }

    public async Task<ServiceRequest> AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
    {
        _context.ServiceRequests.Add(serviceRequest);
        await _context.SaveChangesAsync(cancellationToken);
        return serviceRequest;
    }

    public async Task UpdateAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
    {
        _context.ServiceRequests.Update(serviceRequest);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
    {
        _context.ServiceRequests.Remove(serviceRequest);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests.AnyAsync(s => s.ServiceRequestId == id, cancellationToken);
    }
}
