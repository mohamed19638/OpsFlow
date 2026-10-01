using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                refreshToken => refreshToken.Token == token,
                cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(refreshToken =>
                refreshToken.UserId == userId &&
                !refreshToken.IsRevoked &&
                refreshToken.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
