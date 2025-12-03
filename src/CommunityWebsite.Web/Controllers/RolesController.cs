using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Roles API controller - RESTful endpoints for role management.
/// Follows REST conventions and proper HTTP semantics.
/// Admin-only endpoints for role administration.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api/[controller]")]
public class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all roles with user counts
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        _logger.LogInformation("GET /api/roles");

        var result = await _roleService.GetAllRolesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a specific role by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetRole(int id)
    {
        _logger.LogInformation("GET /api/roles/{RoleId}", id);

        var result = await _roleService.GetRoleByIdAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a role by name
    /// </summary>
    [HttpGet("name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetRoleByName(string name)
    {
        _logger.LogInformation("GET /api/roles/name/{RoleName}", name);

        var result = await _roleService.GetRoleByNameAsync(name);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all users in a specific role by role ID
    /// </summary>
    [HttpGet("{id}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsersInRole(int id)
    {
        _logger.LogInformation("GET /api/roles/{RoleId}/users", id);

        var result = await _roleService.GetUsersInRoleAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all users in a specific role by role name
    /// </summary>
    [HttpGet("name/{name}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsersInRoleByName(string name)
    {
        _logger.LogInformation("GET /api/roles/name/{RoleName}/users", name);

        var result = await _roleService.GetUsersInRoleByNameAsync(name);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new role (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        _logger.LogInformation("POST /api/roles - Creating role {RoleName}", request.Name);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _roleService.CreateRoleAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetRole), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing role (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        _logger.LogInformation("PUT /api/roles/{RoleId} - Updating role", id);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _roleService.UpdateRoleAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a role (Admin only)
    /// Protected roles (Admin, User, Moderator) cannot be deleted
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteRole(int id)
    {
        _logger.LogInformation("DELETE /api/roles/{RoleId} - Deleting role", id);

        var result = await _roleService.DeleteRoleAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return NoContent();
    }
}
