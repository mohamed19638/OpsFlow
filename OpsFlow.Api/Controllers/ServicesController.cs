using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.Service;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/services")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _service;

    public ServicesController(IServiceService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetAll(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.GetByOrganizationAsync(
            orgId,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (service is null || service.OrganizationId != orgId)
            return NotFound();

        return Ok(service);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Create(
        Guid orgId,
        CreateServiceDto dto,
        CancellationToken cancellationToken)
    {
        var service = await _service.CreateAsync(
            orgId,
            dto,
            GetUserId(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { orgId, id = service.Id },
            service);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrTeamLead)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Update(
        Guid orgId,
        Guid id,
        UpdateServiceDto dto,
        CancellationToken cancellationToken)
    {
        var service = await _service.UpdateAsync(
            id,
            dto,
            GetUserId(),
            cancellationToken);

        if (service is null || service.OrganizationId != orgId)
            return NotFound();

        return Ok(service);
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
        var service = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (service is null || service.OrganizationId != orgId)
            return NotFound();

        var deleted = await _service.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> UpdateStatus(
        Guid orgId,
        Guid id,
        UpdateServiceStatusDto dto,
        CancellationToken cancellationToken)
    {
        var service = await _service.UpdateStatusAsync(
            id,
            dto,
            cancellationToken);

        if (service is null || service.OrganizationId != orgId)
            return NotFound();

        return Ok(service);
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
