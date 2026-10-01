namespace OpsFlow.Application.DTOs.Team;

public record CreateTeamDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
