using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsFlow.Application.Interfaces;
using OpsFlow.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace OpsFlow.IntegrationTests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("opsflow_tests")
        .WithUsername("opsflow")
        .WithPassword("opsflow_test_password")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "integration-test-secret-key-at-least-32-bytes");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "OpsFlow");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "OpsFlowClients");
        await _postgres.StartAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeFactoryAsync();

    private async Task DisposeFactoryAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "integration-test-secret-key-at-least-32-bytes",
                ["JwtSettings:Issuer"] = "OpsFlow",
                ["JwtSettings:Audience"] = "OpsFlowClients",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OpsFlowDbContext>>();
            services.RemoveAll<OpsFlowDbContext>();
            services.AddDbContext<OpsFlowDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));

            services.RemoveAll<IMessagePublisher>();
            services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
        });
    }
}

internal sealed class NoOpMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class => Task.CompletedTask;
}
