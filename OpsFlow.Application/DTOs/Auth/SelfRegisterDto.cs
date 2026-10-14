using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.DTOs.Auth;

public sealed record SelfRegisterDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), StringLength(72)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;
}
