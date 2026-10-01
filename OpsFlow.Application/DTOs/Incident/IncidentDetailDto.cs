namespace OpsFlow.Application.DTOs.Incident;

public record IncidentDetailDto
{
    public IncidentDto Incident { get; init; } = new();
    public IReadOnlyList<IncidentEventDto> Events { get; init; } = [];
    public IReadOnlyList<CommentDto> Comments { get; init; } = [];
}
