using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IServiceRepository : IGenericRepository<Service>
{
    Task<IReadOnlyList<Service>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Service>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);
}
