using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OpsFlow.Application.DTOs.Auth;

namespace OpsFlow.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFactory>
{
    protected IntegrationTestBase(IntegrationTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected IntegrationTestFactory Factory { get; }
    protected HttpClient Client { get; }

    protected async Task<TokenDto> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<TokenDto>())!;
    }

    protected void UseBearerToken(string token) => Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    protected static Guid GetOrganizationId(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
        return document.RootElement.GetProperty("orgId").GetGuid();
    }
}
