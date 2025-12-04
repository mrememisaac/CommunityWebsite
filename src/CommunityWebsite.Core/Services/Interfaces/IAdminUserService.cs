using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Admin user service interface - Handles administrative user management operations
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Gets all users with pagination and optional filtering
    /// </summary>
    Task<Result<IEnumerable<AdminUserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null);

    /// <summary>
    /// Gets a specific user with all their roles and details
    /// </summary>
    Task<Result<AdminUserDetailDto>> GetUserDetailAsync(int userId);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    Task<Result<string>> AssignRoleToUserAsync(int userId, string roleName);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    Task<Result<string>> RemoveRoleFromUserAsync(int userId, string roleName);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    Task<Result<string>> DeactivateUserAsync(int userId);

    /// <summary>
    /// Reactivates a user account
    /// </summary>
    Task<Result<string>> ReactivateUserAsync(int userId);

    /// <summary>
    /// Gets all available roles
    /// </summary>
    Task<Result<IEnumerable<RoleDto>>> GetAllRolesAsync();
}
