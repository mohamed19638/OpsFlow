using OpsFlow.Application.DTOs.Organization;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class OrganizationMapping
{
    public static OrganizationDto ToDto(this Organization entity)
    {
        return new OrganizationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt
        };
    }

    public static Organization ToEntity(this CreateOrganizationDto dto)
    {
        return new Organization
        {
            Name = dto.Name,
            Description = dto.Description
        };
    }

    public static void UpdateEntity(
        this Organization entity,
        UpdateOrganizationDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
    }
}
