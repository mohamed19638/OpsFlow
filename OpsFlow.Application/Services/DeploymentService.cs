using OpsFlow.Application.DTOs.Deployment;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class DeploymentService : IDeploymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeploymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeploymentDto> CreateAsync(
        Guid organizationId,
        CreateDeploymentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(organizationId, cancellationToken);

        if (!organizationExists)
        {
            throw new KeyNotFoundException(
                $"Organization {organizationId} not found.");
        }

        var service = await _unitOfWork.Services
            .GetByIdAsync(dto.ServiceId, cancellationToken);

        if (service is null)
        {
            throw new KeyNotFoundException(
                $"Service {dto.ServiceId} not found.");
        }

        if (service.OrganizationId != organizationId)
        {
            throw new InvalidOperationException(
                "Service does not belong to this organization.");
        }

        var deployment = dto.ToEntity(organizationId);
        deployment.DeployedByUserId = actorUserId;

        await _unitOfWork.Deployments
            .AddAsync(deployment, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deployment.ToDto();
    }

    public async Task<IReadOnlyList<DeploymentDto>> GetAllAsync(
        Guid organizationId,
        Guid? serviceId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var deployments = await _unitOfWork.Deployments
            .GetByOrganizationAsync(
                organizationId,
                cancellationToken);

        if (serviceId.HasValue)
        {
            deployments = deployments
                .Where(deployment =>
                    deployment.ServiceId == serviceId.Value)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            deployments = deployments
                .Where(deployment =>
                    string.Equals(
                        deployment.Environment.ToString(),
                        environment,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return deployments
            .Select(deployment => deployment.ToDto())
            .ToList();
    }

    public async Task<DeploymentDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _unitOfWork.Deployments
            .GetByIdAsync(id, cancellationToken);

        return deployment?.ToDto();
    }

    public async Task<DeploymentDto?> UpdateStatusAsync(
        Guid id,
        UpdateDeploymentStatusDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _unitOfWork.Deployments
            .GetByIdAsync(id, cancellationToken);

        if (deployment is null)
        {
            return null;
        }

        var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is null ||
            (actor.Role != UserRole.Admin &&
             actor.Role != UserRole.TeamLead &&
             actor.Role != UserRole.Engineer) ||
            (actor.Role == UserRole.Engineer && deployment.DeployedByUserId != actorUserId))
        {
            throw new UnauthorizedAccessException(
                "Engineers can only update deployments they created.");
        }

        if (!Enum.TryParse(
                dto.Status,
                true,
                out Domain.Enums.DeploymentStatus status))
        {
            throw new ArgumentException(
                $"Invalid deployment status: {dto.Status}");
        }

        deployment.Status = status;

        _unitOfWork.Deployments.Update(deployment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deployment.ToDto();
    }
}
