namespace OpsFlow.Application.DTOs.Auth;

public record RefreshTokenDto
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;
}
