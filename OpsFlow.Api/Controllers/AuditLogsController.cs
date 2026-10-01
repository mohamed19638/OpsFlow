using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.AuditLog;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAll(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetByOrganizationAsync(
            orgId,
            cancellationToken));
    }
}
