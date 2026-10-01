namespace OpsFlow.Application.DTOs.Deployment;

public record CreateDeploymentDto
{
    public Guid ServiceId { get; init; }

    public string Version { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;
}
