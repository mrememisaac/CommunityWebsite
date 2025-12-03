using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Base controller for API controllers providing common functionality
/// Demonstrates proper inheritance and code reuse in controller hierarchy
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims
    /// </summary>
    protected int? CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Gets the current authenticated user's username from JWT claims
    /// </summary>
    protected string? CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Gets the current authenticated user's email from JWT claims
    /// </summary>
    protected string? CurrentEmail => User.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Gets the current authenticated user's roles from JWT claims
    /// </summary>
    protected IEnumerable<string> CurrentUserRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    protected bool IsInRole(string role) => User.IsInRole(role);

    /// <summary>
    /// Checks if the current user is an administrator
    /// </summary>
    protected bool IsAdmin => IsInRole("Admin");

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    protected ActionResult ErrorResponse(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new { error = message, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Creates a standardized success response with data
    /// </summary>
    protected ActionResult<T> SuccessResponse<T>(T data) => Ok(data);

    /// <summary>
    /// Creates a standardized created response
    /// </summary>
    protected ActionResult<T> CreatedResponse<T>(string actionName, object routeValues, T data)
    {
        return CreatedAtAction(actionName, routeValues, data);
    }
}
