using CommunityWebsite.Core.DTOs;
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
    Task<PagedResult<User>> GetUsersByRoleAsync(string roleName, int pageNumber = 1, int pageSize = 20);
    Task<bool> UserExistsAsync(string email);
    Task<PagedResult<User>> GetActiveUsersAsync(int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// Gets users with pagination and optional search with post/comment counts.
    /// Optimized to avoid N+1 queries.
    /// </summary>
    Task<PagedResult<User>> GetUsersWithStatsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null);
}
