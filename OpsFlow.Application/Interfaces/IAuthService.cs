using OpsFlow.Application.DTOs.Auth;
using OpsFlow.Application.DTOs.User;

namespace OpsFlow.Application.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default);

    Task<UserDto> RegisterSelfAsync(
        SelfRegisterDto dto,
        CancellationToken cancellationToken = default);

    Task<TokenDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default);

    Task<TokenDto> RefreshTokenAsync(
        RefreshTokenDto dto,
        CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string refreshToken,
        Guid userId,
        CancellationToken cancellationToken = default);
}
