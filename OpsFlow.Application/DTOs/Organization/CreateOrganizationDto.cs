namespace OpsFlow.Application.DTOs.Organization;

public record CreateOrganizationDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
