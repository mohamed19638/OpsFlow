namespace OpsFlow.Application.DTOs.Incident;

public record AssignIncidentDto
{
    public Guid? UserId { get; init; }
    public Guid? TeamId { get; init; }
}
