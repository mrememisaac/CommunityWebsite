using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Event service interface - Dependency Inversion Principle
/// Defines contracts for event management operations
/// </summary>
public interface IEventService
{
    Task<Result<EventDto>> GetEventByIdAsync(int eventId);
    Task<Result<PagedResult<EventDto>>> GetUpcomingEventsAsync(int limit = 20, int pageNumber = 1, int pageSize = 20);
    Task<Result<PagedResult<EventDto>>> GetPastEventsAsync(int limit = 20, int pageNumber = 1, int pageSize = 20);
    Task<Result<PagedResult<EventDto>>> GetEventsByOrganizerAsync(int userId, int pageNumber = 1, int pageSize = 20);
    Task<Result<EventDto>> CreateEventAsync(int organizerId, CreateEventRequest request);
    Task<Result<EventDto>> UpdateEventAsync(int eventId, int userId, UpdateEventRequest request);
    Task<Result> CancelEventAsync(int eventId, int userId);
    Task<Result> DeleteEventAsync(int eventId, int userId);
    Task<Result> IncrementAttendeeCountAsync(int eventId);
    Task<Result> DecrementAttendeeCountAsync(int eventId);
}
