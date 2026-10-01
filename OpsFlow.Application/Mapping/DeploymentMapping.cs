using OpsFlow.Application.DTOs.Deployment;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class DeploymentMapping
{
    public static DeploymentDto ToDto(this Deployment entity)
    {
        return new DeploymentDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            ServiceId = entity.ServiceId,
            Version = entity.Version,
            Environment = entity.Environment.ToString(),
            Status = entity.Status.ToString(),
            DeployedAt = entity.DeployedAt
        };
    }

    public static Deployment ToEntity(
        this CreateDeploymentDto dto,
        Guid organizationId)
    {
        return new Deployment
        {
            OrganizationId = organizationId,
            ServiceId = dto.ServiceId,
            Version = dto.Version,
            Environment = Enum.Parse<OpsFlow.Domain.Enums.DeploymentEnvironment>(dto.Environment)
        };
    }
}
