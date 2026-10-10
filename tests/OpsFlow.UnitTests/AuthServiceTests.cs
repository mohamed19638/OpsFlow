using System.Security.Authentication;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using OpsFlow.Application.DTOs.Auth;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Settings;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Auth;

namespace OpsFlow.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly AuthService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly User _user;

    public AuthServiceTests()
    {
        _user = new User
        {
            Id = _userId, OrganizationId = _orgId, Email = "person@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Test", LastName = "User", Role = UserRole.Engineer, IsActive = true
        };
        _unit.SetupGet(unit => unit.Users).Returns(_users.Object);
        _unit.SetupGet(unit => unit.Organizations).Returns(_organizations.Object);
        _unit.SetupGet(unit => unit.RefreshTokens).Returns(_refreshTokens.Object);
        _unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new AuthService(_unit.Object, _jwt.Object, Options.Create(new JwtSettings
        {
            SecretKey = "unit-test-signing-secret-at-least-32-bytes",
            Issuer = "OpsFlow", Audience = "OpsFlowClients",
            AccessTokenExpirationMinutes = 60, RefreshTokenExpirationDays = 7
        }));
    }

    [Fact]
    public async Task RegisterAsync_creates_user_with_normalized_email_and_hashed_password()
    {
        User? saved = null;
        _users.Setup(repo => repo.GetByEmailAsync("person@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _organizations.Setup(repo => repo.ExistsAsync(_orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(new RegisterDto
        {
            Email = " PERSON@EXAMPLE.COM ", Password = "Password123!", FullName = "Test User", Role = "Engineer", OrganizationId = _orgId
        });

        result.Email.Should().Be("person@example.com");
        saved.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("Password123!", saved!.PasswordHash).Should().BeTrue();
        saved.PasswordHash.Should().NotBe("Password123!");
        _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_rejects_duplicate_email()
    {
        _users.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        var act = () => _service.RegisterAsync(new RegisterDto { Email = _user.Email, Password = "Password123!", FullName = "Test User", Role = "Engineer", OrganizationId = _orgId });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("A user with this email already exists.");
    }

    [Fact]
    public async Task LoginAsync_returns_tokens_and_persists_refresh_token_for_valid_credentials()
    {
        _users.Setup(repo => repo.GetByEmailAsync(_user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _jwt.Setup(generator => generator.GenerateAccessToken(_user)).Returns("access-token");
        _jwt.Setup(generator => generator.GenerateRefreshToken()).Returns("refresh-token");
        _refreshTokens.Setup(repo => repo.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.LoginAsync(new LoginDto { Email = _user.Email, Password = "Password123!" });

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        _refreshTokens.Verify(repo => repo.AddAsync(It.Is<RefreshToken>(token => token.UserId == _userId && token.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_rejects_wrong_password()
    {
        _users.Setup(repo => repo.GetByEmailAsync(_user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        var act = () => _service.LoginAsync(new LoginDto { Email = _user.Email, Password = "WrongPassword" });

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_rotates_valid_token_and_revokes_previous_token()
    {
        var oldToken = new RefreshToken { UserId = _userId, Token = "old", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokens.Setup(repo => repo.GetByTokenAsync("old", It.IsAny<CancellationToken>())).ReturnsAsync(oldToken);
        _users.Setup(repo => repo.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _jwt.Setup(generator => generator.GetPrincipalFromExpiredToken("expired-access"))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", _userId.ToString())])));
        _jwt.Setup(generator => generator.GenerateRefreshToken()).Returns("new-refresh");
        _jwt.Setup(generator => generator.GenerateAccessToken(_user)).Returns("new-access");

        var result = await _service.RefreshTokenAsync(new RefreshTokenDto { AccessToken = "expired-access", RefreshToken = "old" });

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByToken.Should().Be("new-refresh");
        _refreshTokens.Verify(repo => repo.AddAsync(It.Is<RefreshToken>(token => token.Token == "new-refresh"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task RefreshTokenAsync_rejects_expired_or_revoked_token(bool expired, bool revoked)
    {
        var token = new RefreshToken
        {
            UserId = _userId, Token = "inactive", ExpiresAt = expired ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddDays(1),
            RevokedAt = revoked ? DateTime.UtcNow : null
        };
        _refreshTokens.Setup(repo => repo.GetByTokenAsync("inactive", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var act = () => _service.RefreshTokenAsync(new RefreshTokenDto { AccessToken = "expired-access", RefreshToken = "inactive" });

        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
