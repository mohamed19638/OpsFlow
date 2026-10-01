using OpsFlow.Application.DTOs.Service;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class ServiceMapping
{
    public static ServiceDto ToDto(this Service entity)
    {
        return new ServiceDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            TeamId = entity.TeamId,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt
        };
    }

    public static Service ToEntity(
        this CreateServiceDto dto,
        Guid organizationId)
    {
        return new Service
        {
            OrganizationId = organizationId,
            TeamId = dto.TeamId,
            Name = dto.Name,
            Description = dto.Description
        };
    }

    public static void UpdateEntity(
        this Service entity,
        UpdateServiceDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.TeamId = dto.TeamId;
    }
}
