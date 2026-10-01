using OpsFlow.Application.DTOs.User;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        return users.Select(user => user.ToDto()).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        return user?.ToDto();
    }

    public async Task<UserDto?> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(userId, cancellationToken);
    }

    public async Task<bool> UpdateRoleAsync(
        Guid id,
        UpdateUserRoleDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role) ||
            !Enum.IsDefined(role))
        {
            throw new InvalidOperationException("The specified user role is invalid.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Role = role;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
