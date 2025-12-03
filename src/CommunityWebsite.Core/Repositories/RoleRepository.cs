using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// Role repository implementation for role management operations.
/// </summary>
public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(CommunityDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetRoleByNameAsync(string name)
    {
        return await _dbSet
            .Where(r => r.Name == name)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Role>> GetAllRolesWithUsersAsync()
    {
        return await _dbSet
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> RoleExistsAsync(string name)
    {
        return await _dbSet.AnyAsync(r => r.Name == name);
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(int roleId)
    {
        var role = await _dbSet
            .Where(r => r.Id == roleId)
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync();

        return role?.UserRoles.Select(ur => ur.User) ?? Enumerable.Empty<User>();
    }
}
