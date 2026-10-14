using System.Security.Authentication;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using OpsFlow.Application.DTOs.Auth;
using OpsFlow.Application.DTOs.User;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Application.Settings;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<UserDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var email = dto.Email.Trim().ToLowerInvariant();
        var fullName = dto.FullName.Trim();
        if (email.Length == 0 || dto.Password.Length == 0 || fullName.Length == 0)
        {
            throw new InvalidOperationException("Email, password, and full name are required.");
        }

        if (await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role) ||
            !Enum.IsDefined(role))
        {
            throw new InvalidOperationException("The specified user role is invalid.");
        }

        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(dto.OrganizationId, cancellationToken);
        if (!organizationExists)
        {
            throw new KeyNotFoundException($"Organization {dto.OrganizationId} was not found.");
        }

        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Role = role,
            OrganizationId = dto.OrganizationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task<UserDto> RegisterSelfAsync(
        SelfRegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var organizations = await _unitOfWork.Organizations.GetAllAsync(cancellationToken);
        if (organizations.Count != 1)
        {
            throw new InvalidOperationException(
                "Self-registration is available only when exactly one organization exists. Ask an Admin to create your account.");
        }

        return await RegisterAsync(
            new RegisterDto
            {
                Email = dto.Email,
                Password = dto.Password,
                FullName = dto.FullName,
                Role = UserRole.Engineer.ToString(),
                OrganizationId = organizations[0].Id
            },
            cancellationToken);
    }

    public async Task<TokenDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new AuthenticationException("The email or password is incorrect.");
        }

        var now = DateTime.UtcNow;
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<TokenDto> RefreshTokenAsync(
        RefreshTokenDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var storedRefreshToken = await _unitOfWork.RefreshTokens
            .GetByTokenAsync(dto.RefreshToken, cancellationToken);

        if (storedRefreshToken is null || !storedRefreshToken.IsActive)
        {
            throw new AuthenticationException("The refresh token is invalid or expired.");
        }

        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(dto.AccessToken);
        var userIdClaim = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                          principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) || userId != storedRefreshToken.UserId)
        {
            throw new AuthenticationException("The access token does not match the refresh token.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationException("The user for this token is unavailable.");
        }

        var now = DateTime.UtcNow;
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        storedRefreshToken.RevokedAt = now;
        storedRefreshToken.ReplacedByToken = newRefreshTokenValue;
        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task RevokeTokenAsync(
        string refreshToken,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await _unitOfWork.RefreshTokens
            .GetByTokenAsync(refreshToken, cancellationToken);

        if (storedRefreshToken is null ||
            storedRefreshToken.UserId != userId ||
            !storedRefreshToken.IsActive)
        {
            throw new AuthenticationException("The refresh token is invalid or already revoked.");
        }

        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
