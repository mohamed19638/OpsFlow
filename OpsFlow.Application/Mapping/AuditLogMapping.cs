using OpsFlow.Application.DTOs.AuditLog;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class AuditLogMapping
{
    public static AuditLogDto ToDto(this AuditLog entity)
    {
        return new AuditLogDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            UserId = entity.UserId,
            Action = entity.Action,
            EntityType = entity.EntityType,
            EntityId = entity.EntityId,
            Details = entity.Details,
            CreatedAt = entity.CreatedAt
        };
    }
}
