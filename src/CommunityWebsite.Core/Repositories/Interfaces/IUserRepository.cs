using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// User repository interface showcasing advanced queries.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserWithRolesAsync(int userId);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<bool> UserExistsAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync(int pageNumber = 1, int pageSize = 20);
    Task<IEnumerable<User>> GetAllUsersAsync();

    /// <summary>
    /// Gets users with pagination and optional search with post/comment counts.
    /// Optimized to avoid N+1 queries.
    /// </summary>
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersWithStatsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null);
}
