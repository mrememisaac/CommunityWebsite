using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Role repository interface for role management operations.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetRoleByNameAsync(string name);
    Task<IEnumerable<Role>> GetAllRolesWithUsersAsync();
    Task<bool> RoleExistsAsync(string name);
    Task<IEnumerable<User>> GetUsersInRoleAsync(int roleId);
}
