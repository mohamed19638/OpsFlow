using OpsFlow.Application.DTOs.Team;
using OpsFlow.Application.DTOs.User;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class TeamService : ITeamService
{
    private readonly IUnitOfWork _unitOfWork;

    public TeamService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TeamDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var teams = await _unitOfWork.Teams
            .GetByOrganizationAsync(organizationId, cancellationToken);

        return teams
            .Select(team => team.ToDto())
            .ToList();
    }

    public async Task<TeamDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var team = await _unitOfWork.Teams
            .GetByIdAsync(id, cancellationToken);

        return team?.ToDto();
    }

    public async Task<TeamDto> CreateAsync(
        Guid organizationId,
        CreateTeamDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanCreateTeamAsync(actorUserId, organizationId, cancellationToken);

        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(organizationId, cancellationToken);

        if (!organizationExists)
        {
            throw new KeyNotFoundException(
                $"Organization {organizationId} not found.");
        }

        var team = dto.ToEntity(organizationId);

        await _unitOfWork.Teams
            .AddAsync(team, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }

    public async Task<TeamDto?> UpdateAsync(
        Guid id,
        UpdateTeamDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await _unitOfWork.Teams
            .GetByIdAsync(id, cancellationToken);

        if (team is null)
        {
            return null;
        }

        await EnsureCanManageTeamAsync(actorUserId, team.Id, cancellationToken);

        team.UpdateEntity(dto);

        _unitOfWork.Teams.Update(team);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var team = await _unitOfWork.Teams
            .GetByIdAsync(id, cancellationToken);

        if (team is null)
        {
            return false;
        }

        _unitOfWork.Teams.Delete(team);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task AddMemberAsync(
        Guid teamId,
        AddTeamMemberDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null)
        {
            throw new KeyNotFoundException(
                $"Team {teamId} not found.");
        }

        await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);

        var userExists = await _unitOfWork.Users
            .ExistsAsync(dto.UserId, cancellationToken);

        if (!userExists)
        {
            throw new KeyNotFoundException(
                $"User {dto.UserId} not found.");
        }

        var alreadyMember = await _unitOfWork.TeamMembers
            .ExistsByTeamAndUserAsync(
                teamId,
                dto.UserId,
                cancellationToken);

        if (alreadyMember)
        {
            throw new InvalidOperationException(
                "User is already a member of this team.");
        }

        var teamMember = new TeamMember
        {
            TeamId = teamId,
            UserId = dto.UserId
        };

        await _unitOfWork.TeamMembers
            .AddAsync(teamMember, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveMemberAsync(
     Guid teamId,
     Guid userId,
     Guid actorUserId,
     CancellationToken cancellationToken = default)
    {
        var team = await _unitOfWork.Teams.GetByIdAsync(
            teamId,
            cancellationToken);

        if (team is null)
            return false;

        await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);

        var teamMember = await _unitOfWork.TeamMembers
            .GetByTeamAsync(teamId, cancellationToken);

        var member = teamMember.FirstOrDefault(
            x => x.UserId == userId);

        if (member is null)
            return false;

        _unitOfWork.TeamMembers.Delete(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<UserDto>> GetMembersAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var teamExists = await _unitOfWork.Teams
            .ExistsAsync(teamId, cancellationToken);

        if (!teamExists)
        {
            throw new KeyNotFoundException(
                $"Team {teamId} not found.");
        }

        var members = await _unitOfWork.TeamMembers
            .GetByTeamAsync(teamId, cancellationToken);

        var users = new List<UserDto>();

        foreach (var member in members)
        {
            var user = await _unitOfWork.Users
                .GetByIdAsync(member.UserId, cancellationToken);

            if (user is not null)
            {
                users.Add(user.ToDto());
            }
        }

        return users;
    }

    private async Task EnsureCanCreateTeamAsync(
        Guid actorUserId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is null ||
            (actor.Role != UserRole.Admin &&
             (actor.Role != UserRole.TeamLead || actor.OrganizationId != organizationId)))
        {
            throw new UnauthorizedAccessException(
                "You can only create teams in your organization.");
        }
    }

    private async Task EnsureCanManageTeamAsync(
        Guid actorUserId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("The current user could not be found.");
        }

        if (actor.Role == UserRole.Admin)
        {
            return;
        }

        var isTeamLeadMember = actor.Role == UserRole.TeamLead &&
            await _unitOfWork.TeamMembers.ExistsByTeamAndUserAsync(
                teamId,
                actorUserId,
                cancellationToken);

        if (!isTeamLeadMember)
        {
            throw new UnauthorizedAccessException(
                "You must be a member of this team to manage it.");
        }
    }
}
