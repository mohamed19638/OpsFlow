using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsFlow.Application.DTOs.Incident;
using OpsFlow.Application.DTOs.User;

namespace OpsFlow.IntegrationTests;

public class AuthorizationTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Requests_without_a_token_receive_401()
    {
        var response = await Client.GetAsync($"/api/organizations/{Guid.NewGuid()}/incidents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_cannot_create_incidents()
    {
        var tokens = await LoginAsync("mariam.farid@acmetech.io", "View@1234!");
        UseBearerToken(tokens.AccessToken);
        var organizationId = GetOrganizationId(tokens.AccessToken);
        var services = await Client.GetFromJsonAsync<List<ServiceDto>>($"/api/organizations/{organizationId}/services");

        var response = await Client.PostAsJsonAsync($"/api/organizations/{organizationId}/incidents", new CreateIncidentDto
        {
            Title = "Forbidden by role", Description = "A viewer should not be able to create this.",
            Severity = "Low", ServiceId = services!.First().Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Engineer_cannot_register_users_or_change_roles()
    {
        var tokens = await LoginAsync("nour.ibrahim@acmetech.io", "Eng@1234!");
        UseBearerToken(tokens.AccessToken);
        var organizationId = GetOrganizationId(tokens.AccessToken);
        var registration = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"test-{Guid.NewGuid():N}@example.com", Password = "Password123!", FullName = "Test User", Role = "Viewer", OrganizationId = organizationId
        });
        registration.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var changeRole = await Client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", new UpdateUserRoleDto { Role = "Admin" });
        changeRole.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ServiceDto(Guid Id, string Name, string? Description, string Status, Guid OrganizationId, Guid? TeamId);
}
