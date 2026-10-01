using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class IncidentEventRepository : GenericRepository<IncidentEvent>, IIncidentEventRepository
{
    public IncidentEventRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<IncidentEvent>> GetByIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(eventItem => eventItem.IncidentId == incidentId)
            .ToListAsync(cancellationToken);
    }
}
