using OpsFlow.Application.DTOs.AuditLog;

namespace OpsFlow.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? details,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}