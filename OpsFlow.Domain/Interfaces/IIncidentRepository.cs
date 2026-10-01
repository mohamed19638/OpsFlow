using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IIncidentRepository : IGenericRepository<Incident>
{
    Task<IReadOnlyList<Incident>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Incident>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default);

    Task<Incident?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
