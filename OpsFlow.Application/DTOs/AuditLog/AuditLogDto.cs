namespace OpsFlow.Application.DTOs.AuditLog;

public record AuditLogDto
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid? UserId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public Guid? EntityId { get; init; }

    public string? Details { get; init; }

    public DateTime CreatedAt { get; init; }
}
