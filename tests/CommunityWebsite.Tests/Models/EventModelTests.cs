using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for Event domain model
/// </summary>
public class EventModelTests
{
    [Fact]
    public void Event_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            Title = "Community Meetup",
            Description = "A great community event",
            StartDate = DateTime.UtcNow.AddDays(7),
            OrganizerId = 1
        };

        // Assert
        eventEntity.IsCancelled.Should().BeFalse();
        eventEntity.AttendeeCount.Should().Be(0);
        eventEntity.EndDate.Should().BeNull();
        eventEntity.Location.Should().BeNull();
        eventEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        eventEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Event_CreationWorks()
    {
        // Arrange
        var organizer = new User { Id = 1, Username = "organizer", Email = "org@test.com", PasswordHash = "hash" };
        var eventEntity = new Event
        {
            Title = "Community Meetup",
            Description = "A great community event",
            StartDate = DateTime.UtcNow.AddDays(7),
            Location = "Downtown",
            Organizer = organizer,
            OrganizerId = organizer.Id
        };

        // Act & Assert
        eventEntity.Title.Should().Be("Community Meetup");
        eventEntity.Organizer.Username.Should().Be("organizer");
        eventEntity.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void Event_WithEndDate_SetsCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);

        // Act
        var eventEntity = new Event
        {
            Title = "Conference",
            Description = "Tech Conference",
            StartDate = startDate,
            EndDate = endDate,
            OrganizerId = 1
        };

        // Assert
        eventEntity.StartDate.Should().Be(startDate);
        eventEntity.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void Event_AttendeeCount_CanBeModified()
    {
        // Arrange
        var eventEntity = new Event
        {
            Title = "Popular Event",
            Description = "Very popular",
            StartDate = DateTime.UtcNow.AddDays(7),
            OrganizerId = 1
        };

        // Act
        eventEntity.AttendeeCount = 50;

        // Assert
        eventEntity.AttendeeCount.Should().Be(50);
    }

    [Fact]
    public void Event_Cancellation_WorksCorrectly()
    {
        // Arrange
        var eventEntity = new Event
        {
            Title = "Cancelled Event",
            Description = "This will be cancelled",
            StartDate = DateTime.UtcNow.AddDays(7),
            OrganizerId = 1
        };

        // Act
        eventEntity.IsCancelled = true;

        // Assert
        eventEntity.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Event_DefaultStrings_AreEmpty()
    {
        // Arrange & Act
        var eventEntity = new Event();

        // Assert
        eventEntity.Title.Should().BeEmpty();
        eventEntity.Description.Should().BeEmpty();
    }

    [Fact]
    public void Event_Location_IsOptional()
    {
        // Arrange & Act
        var virtualEvent = new Event
        {
            Title = "Virtual Meetup",
            Description = "Online event",
            StartDate = DateTime.UtcNow.AddDays(1),
            OrganizerId = 1,
            Location = null
        };

        // Assert
        virtualEvent.Location.Should().BeNull();
    }
}
