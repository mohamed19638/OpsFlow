using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.Deployment;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/deployments")]
public class DeploymentsController : ControllerBase
{
    private readonly IDeploymentService _deploymentService;

    public DeploymentsController(IDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<DeploymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DeploymentDto>>> GetAll(
        Guid orgId,
        [FromQuery] Guid? serviceId,
        [FromQuery] string? environment,
        CancellationToken cancellationToken)
    {
        return Ok(await _deploymentService.GetAllAsync(
            orgId,
            serviceId,
            environment,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(DeploymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentDto>> GetById(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var deployment = await _deploymentService.GetByIdAsync(
            id,
            cancellationToken);

        if (deployment is null || deployment.OrganizationId != orgId)
            return NotFound();

        return Ok(deployment);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(DeploymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentDto>> Create(
        Guid orgId,
        CreateDeploymentDto dto,
        CancellationToken cancellationToken)
    {
        var deployment = await _deploymentService.CreateAsync(
            orgId,
            dto,
            GetUserId(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { orgId, id = deployment.Id },
            deployment);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.AdminTeamLeadOrEngineer)]
    [ProducesResponseType(typeof(DeploymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentDto>> UpdateStatus(
        Guid orgId,
        Guid id,
        UpdateDeploymentStatusDto dto,
        CancellationToken cancellationToken)
    {
        var deployment = await _deploymentService.UpdateStatusAsync(
            id,
            dto,
            GetUserId(),
            cancellationToken);

        if (deployment is null || deployment.OrganizationId != orgId)
            return NotFound();

        return Ok(deployment);
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
