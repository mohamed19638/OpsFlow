using OpsFlow.Application.DTOs.User;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class UserMapping
{
    public static UserDto ToDto(this User entity)
    {
        return new UserDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Role = entity.Role.ToString(),
            CreatedAt = entity.CreatedAt
        };
    }
}
