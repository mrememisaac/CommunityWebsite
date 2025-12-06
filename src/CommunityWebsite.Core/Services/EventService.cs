using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Event service implementation - Single Responsibility Principle
/// Handles event business logic only
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets an event by ID
    /// </summary>
    public async Task<Result<EventDto>> GetEventByIdAsync(int eventId)
    {
        try
        {
            _logger.LogInformation("Retrieving event {EventId}", eventId);

            if (eventId <= 0)
            {
                return Result<EventDto>.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result<EventDto>.Failure("Event not found");
            }

            // Load organizer
            var organizer = await _userRepository.GetByIdAsync(evt.OrganizerId);

            var dto = MapToDto(evt, organizer);
            return Result<EventDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", eventId);
            return Result<EventDto>.Failure("An error occurred while retrieving the event.");
        }
    }

    /// <summary>
    /// Gets upcoming events
    /// </summary>
    public async Task<Result<PagedResult<EventDto>>> GetUpcomingEventsAsync(int limit = 20, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving upcoming events, limit {Limit}", limit);

            if (limit < 1 || limit > 100)
            {
                return Result<PagedResult<EventDto>>.Failure("Limit must be between 1 and 100");
            }

            var pagedResult = await _eventRepository.GetUpcomingEventsAsync(pageNumber, pageSize);
            var items = pagedResult.Items.Select(e => MapToDto(e, e.Organizer)).ToList();
            // Apply limit if specified and less than total items
            var limitedItems = items.Take(limit).ToList();
            var dtoResult = new PagedResult<EventDto>
            {
                Items = limitedItems,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<EventDto>>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            return Result<PagedResult<EventDto>>.Failure("An error occurred while retrieving events.");
        }
    }

    /// <summary>
    /// Gets past events
    /// </summary>
    public async Task<Result<PagedResult<EventDto>>> GetPastEventsAsync(int limit = 20, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving past events, limit {Limit}", limit);

            if (limit < 1 || limit > 100)
            {
                return Result<PagedResult<EventDto>>.Failure("Limit must be between 1 and 100");
            }

            var pagedResult = await _eventRepository.GetPastEventsAsync(pageNumber, pageSize);
            var items = pagedResult.Items.Select(e => MapToDto(e, e.Organizer)).ToList();
            // Apply limit if specified and less than total items
            var limitedItems = items.Take(limit).ToList();
            var dtoResult = new PagedResult<EventDto>
            {
                Items = limitedItems,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<EventDto>>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving past events");
            return Result<PagedResult<EventDto>>.Failure("An error occurred while retrieving events.");
        }
    }

    /// <summary>
    /// Gets events by organizer
    /// </summary>
    public async Task<Result<PagedResult<EventDto>>> GetEventsByOrganizerAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving events for organizer {UserId}", userId);

            if (userId <= 0)
            {
                return Result<PagedResult<EventDto>>.Failure("Invalid user ID");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<PagedResult<EventDto>>.Failure("User not found");
            }

            var pagedEvents = await _eventRepository.GetEventsByOrganizerAsync(userId, pageNumber, pageSize);
            var dtoResult = new PagedResult<EventDto>
            {
                Items = pagedEvents.Items.Select(e => MapToDto(e, user)).ToList(),
                TotalCount = pagedEvents.TotalCount,
                PageNumber = pagedEvents.PageNumber,
                PageSize = pagedEvents.PageSize
            };
            return Result<PagedResult<EventDto>>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for organizer {UserId}", userId);
            return Result<PagedResult<EventDto>>.Failure("An error occurred while retrieving events.");
        }
    }

    /// <summary>
    /// Creates a new event
    /// </summary>
    public async Task<Result<EventDto>> CreateEventAsync(int organizerId, CreateEventRequest request)
    {
        try
        {
            _logger.LogInformation("Creating event for organizer {OrganizerId}", organizerId);

            // Validate organizer
            if (organizerId <= 0)
            {
                return Result<EventDto>.Failure("Invalid organizer ID");
            }

            var organizer = await _userRepository.GetByIdAsync(organizerId);
            if (organizer == null || !organizer.IsActive)
            {
                return Result<EventDto>.Failure("Organizer not found or inactive");
            }

            // Validate title
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<EventDto>.Failure("Event title is required");
            }

            if (request.Title.Length < 3 || request.Title.Length > 200)
            {
                return Result<EventDto>.Failure("Event title must be between 3 and 200 characters");
            }

            // Validate dates
            if (request.StartDate <= DateTime.UtcNow)
            {
                return Result<EventDto>.Failure("Start date must be in the future");
            }

            if (request.EndDate.HasValue && request.EndDate <= request.StartDate)
            {
                return Result<EventDto>.Failure("End date must be after start date");
            }

            var evt = new Event
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location,
                OrganizerId = organizerId,
                IsCancelled = false,
                AttendeeCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _eventRepository.AddAsync(evt);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Event {EventId} created for organizer {OrganizerId}", evt.Id, organizerId);

            var dto = MapToDto(evt, organizer);
            return Result<EventDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event for organizer {OrganizerId}", organizerId);
            return Result<EventDto>.Failure("An error occurred while creating the event.");
        }
    }

    /// <summary>
    /// Updates an existing event
    /// </summary>
    public async Task<Result<EventDto>> UpdateEventAsync(int eventId, int userId, UpdateEventRequest request)
    {
        try
        {
            _logger.LogInformation("Updating event {EventId} by user {UserId}", eventId, userId);

            if (eventId <= 0)
            {
                return Result<EventDto>.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result<EventDto>.Failure("Event not found");
            }

            // Check authorization (only organizer can update)
            if (evt.OrganizerId != userId)
            {
                return Result<EventDto>.Failure("Only the organizer can update this event");
            }

            // Cannot update cancelled events
            if (evt.IsCancelled)
            {
                return Result<EventDto>.Failure("Cannot update a cancelled event");
            }

            // Update title if provided
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                if (request.Title.Length < 3 || request.Title.Length > 200)
                {
                    return Result<EventDto>.Failure("Event title must be between 3 and 200 characters");
                }
                evt.Title = request.Title;
            }

            // Update description if provided
            if (request.Description != null)
            {
                evt.Description = request.Description;
            }

            // Update dates if provided
            if (request.StartDate.HasValue)
            {
                if (request.StartDate <= DateTime.UtcNow)
                {
                    return Result<EventDto>.Failure("Start date must be in the future");
                }
                evt.StartDate = request.StartDate.Value;
            }

            if (request.EndDate.HasValue)
            {
                if (request.EndDate <= evt.StartDate)
                {
                    return Result<EventDto>.Failure("End date must be after start date");
                }
                evt.EndDate = request.EndDate.Value;
            }

            // Update location if provided
            if (request.Location != null)
            {
                evt.Location = request.Location;
            }

            evt.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.UpdateAsync(evt);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Event {EventId} updated successfully", eventId);

            var organizer = await _userRepository.GetByIdAsync(evt.OrganizerId);
            var dto = MapToDto(evt, organizer);
            return Result<EventDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", eventId);
            return Result<EventDto>.Failure("An error occurred while updating the event.");
        }
    }

    /// <summary>
    /// Cancels an event
    /// </summary>
    public async Task<Result> CancelEventAsync(int eventId, int userId)
    {
        try
        {
            _logger.LogInformation("Cancelling event {EventId} by user {UserId}", eventId, userId);

            if (eventId <= 0)
            {
                return Result.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result.Failure("Event not found");
            }

            // Check authorization
            if (evt.OrganizerId != userId)
            {
                return Result.Failure("Only the organizer can cancel this event");
            }

            if (evt.IsCancelled)
            {
                return Result.Failure("Event is already cancelled");
            }

            evt.IsCancelled = true;
            evt.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.UpdateAsync(evt);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Event {EventId} cancelled successfully", eventId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", eventId);
            return Result.Failure("An error occurred while cancelling the event.");
        }
    }

    /// <summary>
    /// Deletes an event (hard delete)
    /// </summary>
    public async Task<Result> DeleteEventAsync(int eventId, int userId)
    {
        try
        {
            _logger.LogInformation("Deleting event {EventId} by user {UserId}", eventId, userId);

            if (eventId <= 0)
            {
                return Result.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result.Failure("Event not found");
            }

            // Check authorization
            if (evt.OrganizerId != userId)
            {
                return Result.Failure("Only the organizer can delete this event");
            }

            await _eventRepository.DeleteAsync(eventId);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Event {EventId} deleted successfully", eventId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            return Result.Failure("An error occurred while deleting the event.");
        }
    }

    private static EventDto MapToDto(Event evt, User? organizer)
    {
        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            Date = evt.StartDate,
            EndDate = evt.EndDate,
            Location = evt.Location,
            IsCancelled = evt.IsCancelled,
            OrganizerId = evt.OrganizerId,
            OrganizerUsername = organizer?.Username,
            Organizer = organizer != null
                ? new UserSummaryDto { Id = organizer.Id, Username = organizer.Username }
                : null,
            AttendeeCount = evt.AttendeeCount,
            CreatedAt = evt.CreatedAt
        };
    }

    /// <summary>
    /// Increments the attendee count for an event
    /// </summary>
    public async Task<Result> IncrementAttendeeCountAsync(int eventId)
    {
        try
        {
            _logger.LogInformation("Incrementing attendee count for event {EventId}", eventId);

            if (eventId <= 0)
            {
                return Result.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result.Failure("Event not found");
            }

            if (evt.IsCancelled)
            {
                return Result.Failure("Cannot register for a cancelled event");
            }

            evt.AttendeeCount++;
            evt.UpdatedAt = DateTime.UtcNow;
            await _eventRepository.UpdateAsync(evt);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Attendee count incremented for event {EventId}", eventId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing attendee count for event {EventId}", eventId);
            return Result.Failure("An error occurred while updating the attendee count.");
        }
    }

    /// <summary>
    /// Decrements the attendee count for an event
    /// </summary>
    public async Task<Result> DecrementAttendeeCountAsync(int eventId)
    {
        try
        {
            _logger.LogInformation("Decrementing attendee count for event {EventId}", eventId);

            if (eventId <= 0)
            {
                return Result.Failure("Invalid event ID");
            }

            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return Result.Failure("Event not found");
            }

            if (evt.AttendeeCount <= 0)
            {
                return Result.Failure("Attendee count cannot be negative");
            }

            evt.AttendeeCount--;
            evt.UpdatedAt = DateTime.UtcNow;
            await _eventRepository.UpdateAsync(evt);
            await _eventRepository.SaveChangesAsync();

            _logger.LogInformation("Attendee count decremented for event {EventId}", eventId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing attendee count for event {EventId}", eventId);
            return Result.Failure("An error occurred while updating the attendee count.");
        }
    }
}
