namespace OpsFlow.Application.DTOs.Auth;

public record RevokeTokenDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
