using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Incident;

public record UpdateIncidentSeverityDto
{
    [Required]
    public string Severity { get; init; } = string.Empty;
}
