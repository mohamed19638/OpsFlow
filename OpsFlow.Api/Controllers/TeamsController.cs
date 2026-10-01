using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.Team;
using OpsFlow.Application.DTOs.User;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/teams")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetAll(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        return Ok(await _teamService.GetByOrganizationAsync(
            orgId,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> GetById(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.GetByIdAsync(
            id,
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        return Ok(team);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> Create(
        Guid orgId,
        CreateTeamDto dto,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.CreateAsync(
            orgId,
            dto,
            GetUserId(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { orgId, id = team.Id },
            team);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> Update(
        Guid orgId,
        Guid id,
        UpdateTeamDto dto,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.UpdateAsync(
            id,
            dto,
            GetUserId(),
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        return Ok(team);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.GetByIdAsync(
            id,
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        var deleted = await _teamService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetMembers(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.GetByIdAsync(
            id,
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        return Ok(await _teamService.GetMembersAsync(
            id,
            cancellationToken));
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(
        Guid orgId,
        Guid id,
        AddTeamMemberDto dto,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.GetByIdAsync(
            id,
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        await _teamService.AddMemberAsync(
            id,
            dto,
            GetUserId(),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        Guid orgId,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var team = await _teamService.GetByIdAsync(
            id,
            cancellationToken);

        if (team is null || team.OrganizationId != orgId)
            return NotFound();

        var removed = await _teamService.RemoveMemberAsync(
            id,
            userId,
            GetUserId(),
            cancellationToken);

        if (!removed)
            return NotFound();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException("User ID was not found in the access token.");
        }

        return parsedUserId;
    }
}
