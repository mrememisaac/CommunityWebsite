using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// Event repository implementation.
/// </summary>
public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(CommunityDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Event>> GetUpcomingEventsAsync(int pageNumber = 1, int pageSize = 20)
    {
        var query = _dbSet.Where(e => e.StartDate > DateTime.UtcNow && !e.IsCancelled);
        var totalCount = await query.CountAsync();
        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .OrderBy(e => e.StartDate)
            .Skip(skip)
            .Take(pageSize)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Event>> GetPastEventsAsync(int pageNumber = 1, int pageSize = 20)
    {
        var query = _dbSet.Where(e => e.StartDate <= DateTime.UtcNow);
        var totalCount = await query.CountAsync();
        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .OrderByDescending(e => e.StartDate)
            .Skip(skip)
            .Take(pageSize)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Event>> GetEventsByOrganizerAsync(int userId, int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = 20;
        var skip = (pageNumber - 1) * pageSize;
        var query = _dbSet.Where(e => e.OrganizerId == userId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.StartDate)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
