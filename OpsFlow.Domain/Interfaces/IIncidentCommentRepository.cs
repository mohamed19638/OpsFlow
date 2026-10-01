using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IIncidentCommentRepository : IGenericRepository<IncidentComment>
{
    Task<IReadOnlyList<IncidentComment>> GetByIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
