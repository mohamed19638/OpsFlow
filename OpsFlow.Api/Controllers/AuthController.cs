using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Constants;
using OpsFlow.Application.DTOs.Auth;
using OpsFlow.Application.DTOs.User;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _authService = authService;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> Register(
        RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _authService.RegisterAsync(dto, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(UsersController.GetById),
            controllerName: "Users",
            routeValues: new { id = user.Id },
            value: user);
    }

    [HttpPost("self-register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SelfRegister(
        SelfRegisterDto dto,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() ||
            !_configuration.GetValue<bool>("PublicRegistration:Enabled"))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Self-registration is disabled. Ask an Admin to create your account."
            });
        }

        var user = await _authService.RegisterSelfAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(
        LoginDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(dto, cancellationToken));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> RefreshToken(
        RefreshTokenDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshTokenAsync(dto, cancellationToken));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeToken(
        RevokeTokenDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _authService.RevokeTokenAsync(dto.RefreshToken, userId, cancellationToken);
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
