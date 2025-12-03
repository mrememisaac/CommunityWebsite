using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Authentication API controller for user registration and login.
/// Demonstrates JWT token-based authentication.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Request cannot be null" });

        var result = await _authService.RegisterAsync(request);

        if (!result.IsSuccess)
        {
            var errors = result.Errors ?? new List<string> { result.ErrorMessage ?? "Unknown error" };
            return BadRequest(new { errors });
        }

        return CreatedAtAction(nameof(Register), new { id = result.Data?.UserId }, result.Data);
    }

    /// <summary>
    /// Login user with email and password
    /// Returns JWT token for subsequent authenticated requests
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Request cannot be null" });

        var result = await _authService.LoginAsync(request);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Verify token validity
    /// Returns user info if token is valid
    /// </summary>
    [HttpGet("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> VerifyToken([FromHeader] string Authorization)
    {
        if (string.IsNullOrWhiteSpace(Authorization))
            return BadRequest(new { error = "Authorization header is required" });

        // Extract token from "Bearer {token}"
        var token = Authorization.StartsWith("Bearer ")
            ? Authorization.Substring("Bearer ".Length).Trim()
            : Authorization;

        var result = await _authService.GetUserFromTokenAsync(token);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.ErrorMessage });

        var user = result.Data;
        return Ok(new { userId = user!.Id, username = user.Username, email = user.Email });
    }
}
