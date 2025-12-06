using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
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
    Task<Result<PagedResult<RoleDto>>> GetAllRolesAsync(int pageNumber = 1, int pageSize = 20);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
    Task<Result> DeleteRoleAsync(int roleId);
    Task<Result<PagedResult<UserSummaryDto>>> GetUsersInRoleAsync(int roleId, int pageNumber = 1, int pageSize = 20);
    Task<Result<PagedResult<UserSummaryDto>>> GetUsersInRoleByNameAsync(string roleName, int pageNumber = 1, int pageSize = 20);
}
