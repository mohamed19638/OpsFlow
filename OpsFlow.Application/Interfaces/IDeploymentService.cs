using OpsFlow.Application.DTOs.Deployment;

namespace OpsFlow.Application.Interfaces;

public interface IDeploymentService
{
    Task<DeploymentDto> CreateAsync(
        Guid organizationId,
        CreateDeploymentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeploymentDto>> GetAllAsync(
        Guid organizationId,
        Guid? serviceId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    Task<DeploymentDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DeploymentDto?> UpdateStatusAsync(
        Guid id,
        UpdateDeploymentStatusDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
