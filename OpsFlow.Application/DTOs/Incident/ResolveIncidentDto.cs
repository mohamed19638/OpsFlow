using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Incident;

public record ResolveIncidentDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string ResolutionNotes { get; init; } = string.Empty;
}
