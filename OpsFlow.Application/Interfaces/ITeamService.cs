using OpsFlow.Application.DTOs.Team;
using OpsFlow.Application.DTOs.User;

namespace OpsFlow.Application.Interfaces;

public interface ITeamService
{
    Task<IReadOnlyList<TeamDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<TeamDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TeamDto> CreateAsync(
        Guid organizationId,
        CreateTeamDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<TeamDto?> UpdateAsync(
        Guid id,
        UpdateTeamDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddMemberAsync(
        Guid teamId,
        AddTeamMemberDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveMemberAsync(
        Guid teamId,
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetMembersAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);
}
