using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Repositories;

public class DeploymentRepository : GenericRepository<Deployment>, IDeploymentRepository
{
    public DeploymentRepository(OpsFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Deployment>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(deployment => deployment.ServiceId == serviceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Deployment>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(deployment => deployment.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
