using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
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

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int limit = 20)
    {
        return await _dbSet
            .Where(e => e.StartDate > DateTime.UtcNow && !e.IsCancelled)
            .OrderBy(e => e.StartDate)
            .Take(limit)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetPastEventsAsync(int limit = 20)
    {
        return await _dbSet
            .Where(e => e.StartDate <= DateTime.UtcNow)
            .OrderByDescending(e => e.StartDate)
            .Take(limit)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int userId)
    {
        return await _dbSet
            .Where(e => e.OrganizerId == userId)
            .OrderByDescending(e => e.StartDate)
            .AsNoTracking()
            .ToListAsync();
    }
}
