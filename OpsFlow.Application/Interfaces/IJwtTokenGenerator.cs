using System.Security.Claims;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
