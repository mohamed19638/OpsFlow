namespace OpsFlow.Application.DTOs.Team;

public record AddTeamMemberDto
{
    public Guid UserId { get; init; }
}
