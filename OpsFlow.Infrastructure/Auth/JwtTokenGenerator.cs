using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Settings;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const string RoleClaimType = "role";
    private const string OrganizationIdClaimType = "orgId";
    private const int MinimumSigningKeyBytes = 32;

    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(RoleClaimType, user.Role.ToString()),
            new(OrganizationIdClaimType, user.OrganizationId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingCredentials = new SigningCredentials(
            CreateSigningKey(),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(),
            ValidateLifetime = false,
            RequireExpirationTime = true
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                tokenValidationParameters,
                out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private SymmetricSecurityKey CreateSigningKey()
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey must be configured before JWTs can be generated or validated.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
        if (keyBytes.Length < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"JwtSettings:SecretKey must be at least {MinimumSigningKeyBytes} UTF-8 bytes for HMAC-SHA256.");
        }

        return new SymmetricSecurityKey(keyBytes);
    }
}
