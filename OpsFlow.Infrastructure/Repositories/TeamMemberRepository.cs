using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;
namespace OpsFlow.Infrastructure.Repositories;

public class TeamMemberRepository : GenericRepository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<TeamMember>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(member => member.TeamId == teamId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamMember>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByTeamAndUserAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(
                member => member.TeamId == teamId && member.UserId == userId,
                cancellationToken);
    }
}
