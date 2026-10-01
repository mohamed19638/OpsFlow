namespace OpsFlow.Application.DTOs.Incident;

public record IncidentDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ServiceId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public Guid? AssignedToTeamId { get; init; }
    public DateTime DetectedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
