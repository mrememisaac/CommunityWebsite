using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CommunityWebsite.Web.Controllers;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Common;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Unit tests for HomeController
/// Tests MVC view controller functionality
/// </summary>
public class HomeControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockEventService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(
            _mockPostService.Object,
            _mockEventService.Object,
            _mockLogger.Object);
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsViewResult_WithRecentPostsAndUpcomingEvents()
    {
        // Arrange
        var posts = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Test Post 1", Preview = "Preview 1" },
            new() { Id = 2, Title = "Test Post 2", Preview = "Preview 2" }
        };
        var events = new List<EventDto>
        {
            new() { Id = 1, Title = "Test Event 1", Date = DateTime.UtcNow.AddDays(7) },
            new() { Id = 2, Title = "Test Event 2", Date = DateTime.UtcNow.AddDays(14) }
        };

        var pagedPosts = new PagedResult<PostSummaryDto>
        {
            Items = posts,
            PageNumber = 1,
            PageSize = 1,
            TotalCount = posts.Count
        };
        var pagedEvents = new PagedResult<EventDto>
        {
            Items = events,
            PageNumber = 1,
            PageSize = 1,
            TotalCount = events.Count
        };

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<PostSummaryDto>>.Success(pagedPosts));
        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<EventDto>>.Success(pagedEvents));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        ((object)_controller.ViewBag.RecentPosts).Should().NotBeNull();
        ((object)_controller.ViewBag.UpcomingEvents).Should().NotBeNull();
    }

    [Fact]
    public async Task Index_WhenPostServiceFails_StillReturnsView()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<PostSummaryDto>>.Failure("Service unavailable"));
        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<EventDto>>.Success(new PagedResult<EventDto>
            {
                Items = new List<EventDto>(),
                PageNumber = 1,
                PageSize = 1,
                TotalCount = 0
            }));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WhenEventServiceFails_StillReturnsView()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<PostSummaryDto>>.Success(new PagedResult<PostSummaryDto>
            {
                Items = new List<PostSummaryDto>(),
                PageNumber = 1,
                PageSize = 1,
                TotalCount = 0
            }));
        _mockEventService.Setup(s => s.GetUpcomingEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<PagedResult<EventDto>>.Failure("Service unavailable"));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WhenExceptionOccurs_ReturnsViewWithEmptyCollections()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        ((IEnumerable<PostSummaryDto>)_controller.ViewBag.RecentPosts).Should().BeEmpty();
        ((IEnumerable<EventDto>)_controller.ViewBag.UpcomingEvents).Should().BeEmpty();
    }

    #endregion

    #region About Action Tests

    [Fact]
    public void About_ReturnsViewResult()
    {
        // Act
        var result = _controller.About();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    #endregion

    #region Contact Action Tests

    [Fact]
    public void Contact_ReturnsViewResult()
    {
        // Act
        var result = _controller.Contact();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    #endregion

    #region Error Action Tests

    [Fact]
    public void Error_ReturnsViewResult()
    {
        // Act
        var result = _controller.Error();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    #endregion
}
