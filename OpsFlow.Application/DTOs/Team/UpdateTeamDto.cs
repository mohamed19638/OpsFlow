namespace OpsFlow.Application.DTOs.Team;

public record UpdateTeamDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
