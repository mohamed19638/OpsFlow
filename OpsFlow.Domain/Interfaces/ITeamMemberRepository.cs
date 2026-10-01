using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface ITeamMemberRepository : IGenericRepository<TeamMember>
{
    Task<IReadOnlyList<TeamMember>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamMember>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByTeamAndUserAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
