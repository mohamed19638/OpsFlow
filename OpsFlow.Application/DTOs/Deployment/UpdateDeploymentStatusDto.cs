namespace OpsFlow.Application.DTOs.Deployment;

public record UpdateDeploymentStatusDto
{
    public string Status { get; init; } = string.Empty;
}
