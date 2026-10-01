using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(log => log.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
