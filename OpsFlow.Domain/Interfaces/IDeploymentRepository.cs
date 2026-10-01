using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IDeploymentRepository : IGenericRepository<Deployment>
{
    Task<IReadOnlyList<Deployment>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Deployment>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
