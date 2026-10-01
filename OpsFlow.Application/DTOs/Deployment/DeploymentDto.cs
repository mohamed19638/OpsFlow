namespace OpsFlow.Application.DTOs.Deployment;

public record DeploymentDto
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ServiceId { get; init; }

    public string Version { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime DeployedAt { get; init; }
}
