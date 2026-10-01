namespace OpsFlow.Application.DTOs.Service;

public record UpdateServiceDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? TeamId { get; init; }
}
