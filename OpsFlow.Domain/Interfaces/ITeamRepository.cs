using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<IReadOnlyList<Team>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
