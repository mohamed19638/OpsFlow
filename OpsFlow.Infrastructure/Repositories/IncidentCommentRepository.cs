using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class IncidentCommentRepository : GenericRepository<IncidentComment>, IIncidentCommentRepository
{
    public IncidentCommentRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<IncidentComment>> GetByIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(comment => comment.IncidentId == incidentId)
            .ToListAsync(cancellationToken);
    }
}
