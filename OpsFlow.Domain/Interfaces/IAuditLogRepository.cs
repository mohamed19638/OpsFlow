using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
