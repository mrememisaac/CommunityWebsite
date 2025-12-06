using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.DTOs;
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

    public async Task<PagedResult<Role>> GetAllRolesWithUsersAsync(int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = 20;
        var skip = (pageNumber - 1) * pageSize;
        var query = _dbSet.Include(r => r.UserRoles).ThenInclude(ur => ur.User);
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Role>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> RoleExistsAsync(string name)
    {
        return await _dbSet.AnyAsync(r => r.Name == name);
    }

    public async Task<PagedResult<User>> GetUsersInRoleAsync(int roleId, int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = 20;
        var skip = (pageNumber - 1) * pageSize;
        var role = await _dbSet
            .Where(r => r.Id == roleId)
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync();
        var users = role?.UserRoles.Select(ur => ur.User).OrderByDescending(u => u.CreatedAt) ?? Enumerable.Empty<User>();
        var totalCount = users.Count();
        var items = users.Skip(skip).Take(pageSize).ToList();
        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
