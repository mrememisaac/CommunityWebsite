using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Role repository interface for role management operations.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetRoleByNameAsync(string name);
    Task<PagedResult<Role>> GetAllRolesWithUsersAsync(int pageNumber = 1, int pageSize = 20);
    Task<bool> RoleExistsAsync(string name);
    Task<PagedResult<User>> GetUsersInRoleAsync(int roleId, int pageNumber = 1, int pageSize = 20);
}
