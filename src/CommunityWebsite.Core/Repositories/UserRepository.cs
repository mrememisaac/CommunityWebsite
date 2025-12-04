using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Constants;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// User repository implementation.
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(CommunityDbContext context) : base(context)
    {
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetUserWithRolesAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.Id == userId && u.IsActive)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
        var normalizedRole = roleName.ToLower();
        return await _dbSet
            .Where(u => u.IsActive &&
                   u.UserRoles.Any(ur => ur.Role.Name.ToLower() == normalizedRole))
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = PaginationDefaults.DefaultPageSize;

        var skip = (pageNumber - 1) * pageSize;

        return await _dbSet
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Username)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets users with pagination and optional search.
    /// Includes post and comment counts via efficient SQL queries.
    /// </summary>
    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersWithStatsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null)
    {
        var query = _dbSet.AsQueryable();

        // Apply search filter at database level
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearch = searchTerm.ToLower();
            query = query.Where(u =>
                EF.Functions.Like(u.Username.ToLower(), $"%{lowerSearch}%") ||
                EF.Functions.Like(u.Email.ToLower(), $"%{lowerSearch}%"));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination with includes
        var skip = (pageNumber - 1) * pageSize;
        var users = await query
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (users, totalCount);
    }
}
