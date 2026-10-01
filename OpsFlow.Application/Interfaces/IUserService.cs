using OpsFlow.Application.DTOs.User;

namespace OpsFlow.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateRoleAsync(
        Guid id,
        UpdateUserRoleDto dto,
        CancellationToken cancellationToken = default);
}
