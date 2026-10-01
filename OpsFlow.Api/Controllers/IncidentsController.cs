using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.Incident;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetByOrganization(
        Guid orgId,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] Guid? serviceId,
        CancellationToken cancellationToken)
    {
        var incidents = await _incidentService.GetByOrganizationAsync(
            orgId, severity, status, serviceId, cancellationToken);
        return Ok(incidents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncidentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDetailDto>> GetById(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.GetByIdAsync(orgId, id, cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> Create(
        Guid orgId,
        CreateIncidentDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.CreateAsync(orgId, dto, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { orgId, id = incident.Id }, incident);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> UpdateStatus(
        Guid orgId,
        Guid id,
        UpdateIncidentStatusDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.UpdateStatusAsync(orgId, id, dto, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPatch("{id:guid}/severity")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> UpdateSeverity(
        Guid orgId,
        Guid id,
        UpdateIncidentSeverityDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.UpdateSeverityAsync(orgId, id, dto, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> Assign(
        Guid orgId,
        Guid id,
        AssignIncidentDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.AssignAsync(orgId, id, dto, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid orgId,
        Guid id,
        CreateCommentDto dto,
        CancellationToken cancellationToken)
    {
        var comment = await _incidentService.AddCommentAsync(orgId, id, dto, GetUserId(), cancellationToken);
        if (comment is null) return NotFound();
        return StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<IncidentEventDto>>> GetTimeline(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var timeline = await _incidentService.GetTimelineAsync(orgId, id, cancellationToken);
        return timeline is null ? NotFound() : Ok(timeline);
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> Resolve(
        Guid orgId,
        Guid id,
        ResolveIncidentDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.ResolveAsync(orgId, id, dto, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> Reopen(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.ReopenAsync(orgId, id, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost("{id:guid}/postmortem")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> AddPostmortem(
        Guid orgId,
        Guid id,
        PostmortemDto dto,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentService.AddPostmortemAsync(orgId, id, dto, GetUserId(), cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var parsedUserId))
            throw new UnauthorizedAccessException("User ID was not found in the access token.");

        return parsedUserId;
    }
}
