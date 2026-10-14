using Microsoft.Extensions.Logging;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.RedisBenchmarkHost;

public sealed class BenchmarkBypassCacheService : ICacheService
{
    private readonly ILogger<BenchmarkBypassCacheService> _logger;

    public BenchmarkBypassCacheService(
        ILogger<BenchmarkBypassCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Benchmark cache BYPASS for key {CacheKey}",
            key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
