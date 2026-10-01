using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Incident;

public record PostmortemDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Body { get; init; } = string.Empty;
}
