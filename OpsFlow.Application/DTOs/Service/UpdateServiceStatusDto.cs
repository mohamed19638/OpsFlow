namespace OpsFlow.Application.DTOs.Service;

public record UpdateServiceStatusDto
{
    public string Status { get; init; } = string.Empty;
}
