using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CommunityWebsite.Web.Controllers;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Common;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Unit tests for EventsViewController
/// Tests MVC view controller functionality for events
/// </summary>
public class EventsViewControllerTests
{
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILogger<EventsViewController>> _mockLogger;
    private readonly EventsViewController _controller;

    public EventsViewControllerTests()
    {
        _mockEventService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<EventsViewController>>();
        _controller = new EventsViewController(
            _mockEventService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullEventService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EventsViewController(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EventsViewController(_mockEventService.Object, null!));
    }

    #endregion

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsViewResult_WithEventsList()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Event 1", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Event 2", Date = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Events/Index.cshtml");
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_WithUpcomingPeriod_CallsGetUpcomingEvents()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(new List<EventDto>()));

        // Act
        await _controller.Index(period: "upcoming");

        // Assert
        _mockEventService.Verify(s => s.GetUpcomingEventsAsync(100), Times.Once);
        _mockEventService.Verify(s => s.GetPastEventsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Index_WithPastPeriod_CallsGetPastEvents()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetPastEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(new List<EventDto>()));

        // Act
        await _controller.Index(period: "past");

        // Assert
        _mockEventService.Verify(s => s.GetPastEventsAsync(100), Times.Once);
    }

    [Fact]
    public async Task Index_WithSearchParameter_FiltersResults()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Conference", Description = "Tech conference", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Meetup", Description = "Community meetup", Date = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(search: "conference");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.Should().HaveCount(1);
        model.First().Title.Should().Be("Conference");
    }

    [Fact]
    public async Task Index_WithLocationSearch_FiltersResults()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Event 1", Location = "New York", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Event 2", Location = "Los Angeles", Date = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(search: "New York");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.Should().HaveCount(1);
        model.First().Location.Should().Be("New York");
    }

    [Fact]
    public async Task Index_WithDateAscSort_ReturnsSortedByDateAsc()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Later Event", Date = DateTime.UtcNow.AddDays(14) },
            new() { Id = 2, Title = "Sooner Event", Date = DateTime.UtcNow.AddDays(7) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(sortBy: "date-asc");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.First().Title.Should().Be("Sooner Event");
    }

    [Fact]
    public async Task Index_WithDateDescSort_ReturnsSortedByDateDesc()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Sooner Event", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Later Event", Date = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(sortBy: "date-desc");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.First().Title.Should().Be("Later Event");
    }

    [Fact]
    public async Task Index_WithTitleSort_ReturnsSortedAlphabetically()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Zulu Event", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Alpha Event", Date = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(sortBy: "title");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.First().Title.Should().Be("Alpha Event");
    }

    [Fact]
    public async Task Index_SetsViewBagProperties_Correctly()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetPastEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(new List<EventDto>()));

        // Act
        await _controller.Index(search: "test", period: "past", sortBy: "title", page: 2);

        // Assert
        ((string)_controller.ViewBag.Search).Should().Be("test");
        ((string)_controller.ViewBag.Period).Should().Be("past");
        ((string)_controller.ViewBag.SortBy).Should().Be("title");
        ((int)_controller.ViewBag.CurrentPage).Should().Be(2);
    }

    [Fact]
    public async Task Index_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var events = Enumerable.Range(1, 20).Select(i =>
            new EventDto { Id = i, Title = $"Event {i}", Date = DateTime.UtcNow.AddDays(i) }).ToList();

        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Success(events));

        // Act
        var result = await _controller.Index(page: 2);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.Should().HaveCount(9); // Page size is 9, 20 - 9 = 11 remaining, but page 2 gets next 9
        ((int)_controller.ViewBag.TotalPages).Should().Be(3);
    }

    [Fact]
    public async Task Index_WhenServiceFails_ReturnsEmptyList()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(100))
            .ReturnsAsync(Result<IEnumerable<EventDto>>.Failure("Service unavailable"));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<EventDto>>().Subject;
        model.Should().BeEmpty();
    }

    #endregion

    #region Details Action Tests

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithEvent()
    {
        // Arrange
        var eventDto = new EventDto
        {
            Id = 1,
            Title = "Test Event",
            Description = "Test description",
            Date = DateTime.UtcNow.AddDays(7)
        };

        _mockEventService.Setup(s => s.GetEventByIdAsync(1))
            .ReturnsAsync(Result<EventDto>.Success(eventDto));

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Events/Details.cshtml");
        viewResult.Model.Should().BeOfType<EventDto>();
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsViewWithNullModel()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetEventByIdAsync(999))
            .ReturnsAsync(Result<EventDto>.Failure("Not found"));

        // Act
        var result = await _controller.Details(999);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
    }

    #endregion

    #region Create Action Tests

    [Fact]
    public void Create_ReturnsViewResult()
    {
        // Act
        var result = _controller.Create();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Events/Create.cshtml");
    }

    #endregion

    #region Edit Action Tests

    [Fact]
    public async Task Edit_WithValidId_ReturnsViewWithEvent()
    {
        // Arrange
        var eventDto = new EventDto
        {
            Id = 1,
            Title = "Test Event",
            Description = "Test description",
            Date = DateTime.UtcNow.AddDays(7)
        };

        _mockEventService.Setup(s => s.GetEventByIdAsync(1))
            .ReturnsAsync(Result<EventDto>.Success(eventDto));

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Events/Edit.cshtml");
        viewResult.Model.Should().BeOfType<EventDto>();
    }

    [Fact]
    public async Task Edit_WithInvalidId_ReturnsViewWithNullModel()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetEventByIdAsync(999))
            .ReturnsAsync(Result<EventDto>.Failure("Not found"));

        // Act
        var result = await _controller.Edit(999);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
    }

    #endregion
}
