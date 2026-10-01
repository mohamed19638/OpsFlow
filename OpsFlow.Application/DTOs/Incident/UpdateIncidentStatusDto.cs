using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Incident;

public record UpdateIncidentStatusDto
{
    [Required]
    public string Status { get; init; } = string.Empty;
}
