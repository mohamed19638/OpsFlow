using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Incident;

public record CreateIncidentDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
    [Required]
    public string Severity { get; init; } = string.Empty;
    [Required]
    public Guid ServiceId { get; init; }
}
