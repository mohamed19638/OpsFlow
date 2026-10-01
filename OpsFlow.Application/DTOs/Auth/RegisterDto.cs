namespace OpsFlow.Application.DTOs.Auth;

public record RegisterDto
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public Guid OrganizationId { get; init; }
}
