using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Event repository interface.
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    Task<PagedResult<Event>> GetUpcomingEventsAsync(int pageNumber = 1, int pageSize = 20);
    Task<PagedResult<Event>> GetPastEventsAsync(int pageNumber = 1, int pageSize = 20);
    Task<PagedResult<Event>> GetEventsByOrganizerAsync(int userId, int pageNumber = 1, int pageSize = 20);
}
