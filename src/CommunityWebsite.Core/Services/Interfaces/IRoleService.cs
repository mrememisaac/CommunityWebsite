using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Role service interface - Dependency Inversion Principle
/// Defines contracts for role management operations
/// </summary>
public interface IRoleService
{
    Task<Result<RoleDto>> GetRoleByIdAsync(int roleId);
    Task<Result<RoleDto>> GetRoleByNameAsync(string name);
    Task<Result<IEnumerable<RoleDto>>> GetAllRolesAsync();
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
    Task<Result> DeleteRoleAsync(int roleId);
    Task<Result<IEnumerable<UserSummaryDto>>> GetUsersInRoleAsync(int roleId);
    Task<Result<IEnumerable<UserSummaryDto>>> GetUsersInRoleByNameAsync(string roleName);
}
