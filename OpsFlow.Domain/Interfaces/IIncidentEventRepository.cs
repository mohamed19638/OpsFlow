using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IIncidentEventRepository : IGenericRepository<IncidentEvent>
{
    Task<IReadOnlyList<IncidentEvent>> GetByIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
