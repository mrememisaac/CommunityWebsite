using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CommunityWebsite.Core.DTOs;

namespace CommunityWebsite.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CommunityDbContext> _options;
    private readonly CommunityDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly Mock<ILogger<EventService>> _loggerMock;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CommunityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CommunityDbContext(_options);
        _context.Database.EnsureCreated();

        _eventRepository = new EventRepository(_context);
        _userRepository = new UserRepository(_context);
        _loggerMock = new Mock<ILogger<EventService>>();
        _eventService = new EventService(_eventRepository, _userRepository, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<User> CreateTestUserAsync(string username = "testuser")
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Event> CreateTestEventAsync(int organizerId, string title = "Test Event", bool isCancelled = false)
    {
        var evt = new Event
        {
            Title = title,
            Description = "Test event description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            OrganizerId = organizerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AttendeeCount = 0,
            IsCancelled = isCancelled
        };
        await _context.Events.AddAsync(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    #region GetEventByIdAsync Tests

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsSuccessWithEvent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id, "Test Event");

        // Act
        var result = await _eventService.GetEventByIdAsync(evt.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _eventService.GetEventByIdAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    #endregion

    #region GetUpcomingEventsAsync Tests

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsOnlyFutureNonCancelledEvents()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Create future event
        var futureEvent = new Event
        {
            Title = "Future Event",
            Description = "Future",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location",
            OrganizerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsCancelled = false
        };
        await _context.Events.AddAsync(futureEvent);

        // Create past event
        var pastEvent = new Event
        {
            Title = "Past Event",
            Description = "Past",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-7).AddHours(2),
            Location = "Location",
            OrganizerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsCancelled = false
        };
        await _context.Events.AddAsync(pastEvent);

        // Create cancelled future event
        var cancelledEvent = new Event
        {
            Title = "Cancelled Event",
            Description = "Cancelled",
            StartDate = DateTime.UtcNow.AddDays(14),
            EndDate = DateTime.UtcNow.AddDays(14).AddHours(2),
            Location = "Location",
            OrganizerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsCancelled = true
        };
        await _context.Events.AddAsync(cancelledEvent);

        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.GetUpcomingEventsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Future Event");
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        for (int i = 0; i < 5; i++)
        {
            await CreateTestEventAsync(user.Id, $"Event {i}");
        }

        // Act
        var result = await _eventService.GetUpcomingEventsAsync(limit: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
    }

    #endregion

    #region GetPastEventsAsync Tests

    [Fact]
    public async Task GetPastEventsAsync_ReturnsOnlyPastEvents()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Create future event
        var futureEvent = new Event
        {
            Title = "Future Event",
            Description = "Future",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location",
            OrganizerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Events.AddAsync(futureEvent);

        // Create past event
        var pastEvent = new Event
        {
            Title = "Past Event",
            Description = "Past",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-7).AddHours(2),
            Location = "Location",
            OrganizerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Events.AddAsync(pastEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.GetPastEventsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Past Event");
    }

    #endregion

    #region GetEventsByOrganizerAsync Tests

    [Fact]
    public async Task GetEventsByOrganizerAsync_WithValidOrganizer_ReturnsOrganizerEvents()
    {
        // Arrange
        var organizer1 = await CreateTestUserAsync("organizer1");
        var organizer2 = await CreateTestUserAsync("organizer2");

        await CreateTestEventAsync(organizer1.Id, "Organizer 1 Event");
        await CreateTestEventAsync(organizer2.Id, "Organizer 2 Event");

        // Act
        var result = await _eventService.GetEventsByOrganizerAsync(organizer1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Organizer 1 Event");
    }

    [Fact]
    public async Task GetEventsByOrganizerAsync_WithInvalidOrganizer_ReturnsFailure()
    {
        // Act
        var result = await _eventService.GetEventsByOrganizerAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found");
    }

    #endregion

    #region CreateEventAsync Tests

    [Fact]
    public async Task CreateEventAsync_WithValidData_ReturnsSuccessWithEvent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new CreateEventRequest
        {
            Title = "New Event",
            Description = "New event description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "New Location"
        };

        // Act
        var result = await _eventService.CreateEventAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("New Event");
        result.Data.AttendeeCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateEventAsync_WithEmptyTitle_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new CreateEventRequest
        {
            Title = "",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.CreateEventAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("title").And.Contain("required");
    }

    [Fact]
    public async Task CreateEventAsync_WithEndDateBeforeStartDate_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(6), // Before start date
            Location = "Location"
        };

        // Act
        var result = await _eventService.CreateEventAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("End date must be after start date");
    }

    [Fact]
    public async Task CreateEventAsync_WithStartDateInPast_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(-1), // In the past
            EndDate = DateTime.UtcNow.AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.CreateEventAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Start date must be in the future");
    }

    [Fact]
    public async Task CreateEventAsync_WithInvalidOrganizer_ReturnsFailure()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.CreateEventAsync(999, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Organizer").And.Contain("not found");
    }

    #endregion

    #region UpdateEventAsync Tests

    [Fact]
    public async Task UpdateEventAsync_WithValidData_ReturnsSuccessWithUpdatedEvent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id, "Original Title");
        var request = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated description",
            StartDate = DateTime.UtcNow.AddDays(14),
            EndDate = DateTime.UtcNow.AddDays(14).AddHours(3),
            Location = "Updated Location"
        };

        // Act
        var result = await _eventService.UpdateEventAsync(evt.Id, user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Updated Title");
        result.Data.Description.Should().Be("Updated description");
        result.Data.Location.Should().Be("Updated Location");
    }

    [Fact]
    public async Task UpdateEventAsync_WithInvalidEventId_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.UpdateEventAsync(999, user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenNotOrganizer_ReturnsFailure()
    {
        // Arrange
        var organizer = await CreateTestUserAsync("organizer");
        var otherUser = await CreateTestUserAsync("otheruser");
        var evt = await CreateTestEventAsync(organizer.Id);
        var request = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.UpdateEventAsync(evt.Id, otherUser.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only the organizer can update this event");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenCancelled_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id, "Test Event", isCancelled: true);
        var request = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Location"
        };

        // Act
        var result = await _eventService.UpdateEventAsync(evt.Id, user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot update a cancelled event");
    }

    #endregion

    #region CancelEventAsync Tests

    [Fact]
    public async Task CancelEventAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id);

        // Act
        var result = await _eventService.CancelEventAsync(evt.Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var cancelledEvent = await _context.Events.FindAsync(evt.Id);
        cancelledEvent!.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task CancelEventAsync_WithInvalidEventId_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _eventService.CancelEventAsync(999, user.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    [Fact]
    public async Task CancelEventAsync_WhenNotOrganizer_ReturnsFailure()
    {
        // Arrange
        var organizer = await CreateTestUserAsync("organizer");
        var otherUser = await CreateTestUserAsync("otheruser");
        var evt = await CreateTestEventAsync(organizer.Id);

        // Act
        var result = await _eventService.CancelEventAsync(evt.Id, otherUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only the organizer can cancel this event");
    }

    [Fact]
    public async Task CancelEventAsync_WhenAlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id, "Test Event", isCancelled: true);

        // Act
        var result = await _eventService.CancelEventAsync(evt.Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event is already cancelled");
    }

    #endregion

    #region DeleteEventAsync Tests

    [Fact]
    public async Task DeleteEventAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id);

        // Act
        var result = await _eventService.DeleteEventAsync(evt.Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedEvent = await _context.Events.FindAsync(evt.Id);
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventAsync_WithInvalidEventId_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _eventService.DeleteEventAsync(999, user.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    [Fact]
    public async Task DeleteEventAsync_WhenNotOrganizer_ReturnsFailure()
    {
        // Arrange
        var organizer = await CreateTestUserAsync("organizer");
        var otherUser = await CreateTestUserAsync("otheruser");
        var evt = await CreateTestEventAsync(organizer.Id);

        // Act
        var result = await _eventService.DeleteEventAsync(evt.Id, otherUser.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only the organizer can delete this event");
    }

    #endregion

    #region IncrementAttendeeCountAsync Tests

    [Fact]
    public async Task IncrementAttendeeCountAsync_WithValidEventId_IncrementsCount()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id);
        evt.AttendeeCount = 5;
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.IncrementAttendeeCountAsync(evt.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Detach and reload to get fresh data
        _context.Entry(evt).State = EntityState.Detached;
        var updatedEvent = await _context.Events.FindAsync(evt.Id);
        updatedEvent!.AttendeeCount.Should().Be(6);
    }

    [Fact]
    public async Task IncrementAttendeeCountAsync_WithInvalidEventId_ReturnsFailure()
    {
        // Act
        var result = await _eventService.IncrementAttendeeCountAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    [Fact]
    public async Task IncrementAttendeeCountAsync_WhenCancelled_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id, "Test Event", isCancelled: true);

        // Act
        var result = await _eventService.IncrementAttendeeCountAsync(evt.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot register for a cancelled event");
    }

    #endregion

    #region DecrementAttendeeCountAsync Tests

    [Fact]
    public async Task DecrementAttendeeCountAsync_WithValidEventId_DecrementsCount()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id);
        evt.AttendeeCount = 5;
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.DecrementAttendeeCountAsync(evt.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Detach and reload to get fresh data
        _context.Entry(evt).State = EntityState.Detached;
        var updatedEvent = await _context.Events.FindAsync(evt.Id);
        updatedEvent!.AttendeeCount.Should().Be(4);
    }

    [Fact]
    public async Task DecrementAttendeeCountAsync_WithInvalidEventId_ReturnsFailure()
    {
        // Act
        var result = await _eventService.DecrementAttendeeCountAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Event not found");
    }

    [Fact]
    public async Task DecrementAttendeeCountAsync_WhenCountIsZero_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var evt = await CreateTestEventAsync(user.Id);
        evt.AttendeeCount = 0;
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.DecrementAttendeeCountAsync(evt.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Attendee count cannot be negative");
    }

    #endregion
}
