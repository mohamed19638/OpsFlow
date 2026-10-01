namespace OpsFlow.Application.DTOs.Incident;

public record CommentDto
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public Guid UserId { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
