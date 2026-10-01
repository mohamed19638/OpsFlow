namespace OpsFlow.Application.DTOs.Team;

public record TeamDto
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateTime CreatedAt { get; init; }
}
