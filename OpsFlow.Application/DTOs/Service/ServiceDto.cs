namespace OpsFlow.Application.DTOs.Service;

public record ServiceDto
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid? TeamId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
