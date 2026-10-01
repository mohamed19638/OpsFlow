using OpsFlow.Application.DTOs.AuditLog;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? details,
        CancellationToken cancellationToken = default)
    {
        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(organizationId, cancellationToken);

        if (!organizationExists)
        {
            throw new KeyNotFoundException(
                $"Organization {organizationId} not found.");
        }

        if (userId.HasValue)
        {
            var userExists = await _unitOfWork.Users
                .ExistsAsync(userId.Value, cancellationToken);

            if (!userExists)
            {
                throw new KeyNotFoundException(
                    $"User {userId.Value} not found.");
            }
        }

        var auditLog = new AuditLog
        {
            OrganizationId = organizationId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs
            .AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(organizationId, cancellationToken);

        if (!organizationExists)
        {
            throw new KeyNotFoundException(
                $"Organization {organizationId} not found.");
        }

        var auditLogs = await _unitOfWork.AuditLogs
            .GetByOrganizationAsync(
                organizationId,
                cancellationToken);

        return auditLogs
            .Select(auditLog => auditLog.ToDto())
            .ToList();
    }
}
