using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    public TeamRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Team>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(team => team.Members)
            .Where(team => team.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
