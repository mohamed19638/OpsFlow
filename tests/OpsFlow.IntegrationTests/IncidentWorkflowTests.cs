using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OpsFlow.Application.DTOs.Incident;

namespace OpsFlow.IntegrationTests;

public class IncidentWorkflowTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Engineer_can_create_advance_and_resolve_an_incident()
    {
        var tokens = await LoginAsync("nour.ibrahim@acmetech.io", "Eng@1234!");
        UseBearerToken(tokens.AccessToken);
        var organizationId = GetOrganizationId(tokens.AccessToken);
        var servicesResponse = await Client.GetAsync($"/api/organizations/{organizationId}/services");
        servicesResponse.EnsureSuccessStatusCode();
        var services = await servicesResponse.Content.ReadFromJsonAsync<List<ServiceDto>>();
        var serviceId = services!.First().Id;

        var createResponse = await Client.PostAsJsonAsync($"/api/organizations/{organizationId}/incidents", new CreateIncidentDto
        {
            Title = $"Integration incident {Guid.NewGuid():N}",
            Description = "Created by the automated HTTP integration test.",
            Severity = "High", ServiceId = serviceId
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var incident = (await createResponse.Content.ReadFromJsonAsync<IncidentDto>())!;
        incident.Status.Should().Be("Detected");

        var advanceResponse = await Client.PatchAsJsonAsync(
            $"/api/organizations/{organizationId}/incidents/{incident.Id}/status",
            new UpdateIncidentStatusDto { Status = "Investigating" });
        advanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await Client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/incidents/{incident.Id}/resolve",
            new ResolveIncidentDto { ResolutionNotes = "Resolved in integration test." });
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = (await resolveResponse.Content.ReadFromJsonAsync<IncidentDto>())!;
        resolved.Status.Should().Be("Resolved");
        resolved.ResolvedAt.Should().NotBeNull();

        var detailResponse = await Client.GetAsync($"/api/organizations/{organizationId}/incidents/{incident.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = (await detailResponse.Content.ReadFromJsonAsync<IncidentDetailDto>())!;
        detail.Events.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    private sealed record ServiceDto(Guid Id, string Name, string? Description, string Status, Guid OrganizationId, Guid? TeamId);
}
