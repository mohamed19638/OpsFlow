namespace OpsFlow.Application.DTOs.User;

public record UpdateUserRoleDto
{
    public string Role { get; init; } = string.Empty;
}
