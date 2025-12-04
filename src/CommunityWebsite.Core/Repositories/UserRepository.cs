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
}
