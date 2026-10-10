using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsFlow.Application.DTOs.Auth;

namespace OpsFlow.IntegrationTests;

public class AuthControllerTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_refresh_and_revoke_follow_the_token_lifecycle()
    {
        var tokens = await LoginAsync("admin@acmetech.io", "Admin@1234!");
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshedResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", new RefreshTokenDto
        {
            AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken
        });
        refreshedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await refreshedResponse.Content.ReadFromJsonAsync<TokenDto>())!;
        refreshed.RefreshToken.Should().NotBe(tokens.RefreshToken);

        UseBearerToken(refreshed.AccessToken);
        var revokeResponse = await Client.PostAsJsonAsync("/api/auth/revoke-token", new RevokeTokenDto { RefreshToken = refreshed.RefreshToken });
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reusedResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", new RefreshTokenDto
        {
            AccessToken = refreshed.AccessToken, RefreshToken = refreshed.RefreshToken
        });
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_returns_unauthorized_for_wrong_password()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "admin@acmetech.io", Password = "incorrect"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
