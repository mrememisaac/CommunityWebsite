using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Event repository interface.
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetUpcomingEventsAsync(int limit = 20);
    Task<IEnumerable<Event>> GetPastEventsAsync(int limit = 20);
    Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int userId);
}
