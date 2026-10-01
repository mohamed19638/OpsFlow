using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;


namespace OpsFlow.Infrastructure.Repositories;

public class IncidentRepository : GenericRepository<Incident>, IIncidentRepository
{
    public IncidentRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Incident>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(incident => incident.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Incident>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(incident => incident.ServiceId == serviceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Incident?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(incident => incident.Events)
            .Include(incident => incident.Comments)
            .Include(incident => incident.CreatedBy)
            .Include(incident => incident.AssignedTo)
            .FirstOrDefaultAsync(
                incident => incident.Id == id,
                cancellationToken);
    }
}
