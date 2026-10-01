namespace OpsFlow.Application.DTOs.Incident;

public record IncidentEventDto
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public DateTime CreatedAt { get; init; }
}
