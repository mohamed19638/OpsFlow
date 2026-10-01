using OpsFlow.Application.DTOs.Team;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class TeamMapping
{
    public static TeamDto ToDto(this Team entity)
    {
        return new TeamDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt
        };
    }

    public static Team ToEntity(
        this CreateTeamDto dto,
        Guid organizationId)
    {
        return new Team
        {
            OrganizationId = organizationId,
            Name = dto.Name,
            Description = dto.Description
        };
    }

    public static void UpdateEntity(
        this Team entity,
        UpdateTeamDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
    }
}
