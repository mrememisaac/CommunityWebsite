using CommunityWebsite.Core.Common;
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
    Task<Result<IEnumerable<EventDto>>> GetUpcomingEventsAsync(int limit = 20);
    Task<Result<IEnumerable<EventDto>>> GetPastEventsAsync(int limit = 20);
    Task<Result<IEnumerable<EventDto>>> GetEventsByOrganizerAsync(int userId);
    Task<Result<EventDto>> CreateEventAsync(int organizerId, CreateEventRequest request);
    Task<Result<EventDto>> UpdateEventAsync(int eventId, int userId, UpdateEventRequest request);
    Task<Result> CancelEventAsync(int eventId, int userId);
    Task<Result> DeleteEventAsync(int eventId, int userId);
    Task<Result> IncrementAttendeeCountAsync(int eventId);
    Task<Result> DecrementAttendeeCountAsync(int eventId);
}
