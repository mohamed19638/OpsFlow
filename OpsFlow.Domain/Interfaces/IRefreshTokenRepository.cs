using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
