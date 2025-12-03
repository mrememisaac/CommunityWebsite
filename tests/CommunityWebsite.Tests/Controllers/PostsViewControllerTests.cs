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
/// Unit tests for PostsViewController
/// Tests MVC view controller functionality for posts
/// </summary>
public class PostsViewControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<ICommentService> _mockCommentService;
    private readonly Mock<ILogger<PostsViewController>> _mockLogger;
    private readonly PostsViewController _controller;

    public PostsViewControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockCommentService = new Mock<ICommentService>();
        _mockLogger = new Mock<ILogger<PostsViewController>>();
        _controller = new PostsViewController(
            _mockPostService.Object,
            _mockCommentService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPostService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostsViewController(null!, _mockCommentService.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullCommentService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostsViewController(_mockPostService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostsViewController(_mockPostService.Object, _mockCommentService.Object, null!));
    }

    #endregion

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsViewResult_WithPostsList()
    {
        // Arrange
        var posts = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Test Post 1", Preview = "Preview 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Test Post 2", Preview = "Preview 2", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(posts));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Posts/Index.cshtml");
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_WithSearchParameter_UsesSearchService()
    {
        // Arrange
        var searchResults = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Matching Post", Preview = "Preview", CreatedAt = DateTime.UtcNow }
        };

        // Need to setup featured posts as fallback (controller calls it first)
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(new List<PostSummaryDto>()));
        _mockPostService.Setup(s => s.SearchPostsAsync("test"))
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(searchResults));

        // Act
        var result = await _controller.Index(search: "test");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.Should().HaveCount(1);
        model.First().Title.Should().Be("Matching Post");
        _mockPostService.Verify(s => s.SearchPostsAsync("test"), Times.Once);
    }

    [Fact]
    public async Task Index_WithNewestSort_ReturnsSortedByCreatedAtDesc()
    {
        // Arrange
        var posts = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Older Post", Preview = "Preview 1", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 2, Title = "Newer Post", Preview = "Preview 2", CreatedAt = DateTime.UtcNow }
        };

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(posts));

        // Act
        var result = await _controller.Index(sortBy: "newest");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.First().Title.Should().Be("Newer Post");
    }

    [Fact]
    public async Task Index_WithOldestSort_ReturnsSortedByCreatedAtAsc()
    {
        // Arrange
        var posts = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Newer Post", Preview = "Preview 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Older Post", Preview = "Preview 2", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(posts));

        // Act
        var result = await _controller.Index(sortBy: "oldest");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.First().Title.Should().Be("Older Post");
    }

    [Fact]
    public async Task Index_WithPopularSort_ReturnsSortedByCommentCount()
    {
        // Arrange
        var posts = new List<PostSummaryDto>
        {
            new() { Id = 1, Title = "Less Popular", Preview = "Preview 1", CommentCount = 5, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "More Popular", Preview = "Preview 2", CommentCount = 20, CreatedAt = DateTime.UtcNow }
        };

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(posts));

        // Act
        var result = await _controller.Index(sortBy: "popular");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.First().Title.Should().Be("More Popular");
    }

    [Fact]
    public async Task Index_SetsViewBagProperties_Correctly()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(new List<PostSummaryDto>()));
        _mockPostService.Setup(s => s.SearchPostsAsync("test"))
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(new List<PostSummaryDto>()));

        // Act
        await _controller.Index(search: "test", sortBy: "oldest", page: 2);

        // Assert
        ((string)_controller.ViewBag.Search).Should().Be("test");
        ((string)_controller.ViewBag.SortBy).Should().Be("oldest");
        ((int)_controller.ViewBag.CurrentPage).Should().Be(2);
    }

    [Fact]
    public async Task Index_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var posts = Enumerable.Range(1, 25).Select(i =>
            new PostSummaryDto { Id = i, Title = $"Post {i}", Preview = $"Preview {i}", CreatedAt = DateTime.UtcNow }).ToList();

        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Success(posts));

        // Act
        var result = await _controller.Index(page: 2);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.Should().HaveCount(10); // Page size is 10
        ((int)_controller.ViewBag.TotalPages).Should().Be(3);
    }

    [Fact]
    public async Task Index_WhenServiceFails_ReturnsEmptyList()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetFeaturedPostsAsync())
            .ReturnsAsync(Result<IEnumerable<PostSummaryDto>>.Failure("Service unavailable"));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PostDto>>().Subject;
        model.Should().BeEmpty();
    }

    #endregion

    #region Details Action Tests

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithPost()
    {
        // Arrange
        var postDetail = new PostDetailDto
        {
            Id = 1,
            Title = "Test Post",
            Content = "Test content",
            CreatedAt = DateTime.UtcNow
        };

        _mockPostService.Setup(s => s.GetPostDetailAsync(1))
            .ReturnsAsync(Result<PostDetailDto>.Success(postDetail));
        _mockCommentService.Setup(s => s.GetPostCommentsAsync(1))
            .ReturnsAsync(Result<IEnumerable<CommentDto>>.Success(new List<CommentDto>()));

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Posts/Details.cshtml");
        viewResult.Model.Should().BeOfType<PostDto>();
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsViewWithNullModel()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetPostDetailAsync(999))
            .ReturnsAsync(Result<PostDetailDto>.Failure("Not found"));

        // Act
        var result = await _controller.Details(999);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
    }

    [Fact]
    public async Task Details_LoadsCommentsForPost()
    {
        // Arrange
        var postDetail = new PostDetailDto { Id = 1, Title = "Test Post", Content = "Content", CreatedAt = DateTime.UtcNow };
        var comments = new List<CommentDto>
        {
            new() { Id = 1, Content = "Comment 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Content = "Comment 2", CreatedAt = DateTime.UtcNow }
        };

        _mockPostService.Setup(s => s.GetPostDetailAsync(1))
            .ReturnsAsync(Result<PostDetailDto>.Success(postDetail));
        _mockCommentService.Setup(s => s.GetPostCommentsAsync(1))
            .ReturnsAsync(Result<IEnumerable<CommentDto>>.Success(comments));

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewBagComments = _controller.ViewBag.Comments as IEnumerable<CommentDto>;
        viewBagComments.Should().NotBeNull();
        viewBagComments.Should().HaveCount(2);
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
        viewResult.ViewName.Should().Be("~/Views/Posts/Create.cshtml");
    }

    #endregion

    #region Edit Action Tests

    [Fact]
    public async Task Edit_WithValidId_ReturnsViewWithPost()
    {
        // Arrange
        var postDetail = new PostDetailDto
        {
            Id = 1,
            Title = "Test Post",
            Content = "Test content",
            CreatedAt = DateTime.UtcNow
        };

        _mockPostService.Setup(s => s.GetPostDetailAsync(1))
            .ReturnsAsync(Result<PostDetailDto>.Success(postDetail));

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("~/Views/Posts/Edit.cshtml");
        viewResult.Model.Should().BeOfType<PostDto>();
    }

    [Fact]
    public async Task Edit_WithInvalidId_ReturnsViewWithNullModel()
    {
        // Arrange
        _mockPostService.Setup(s => s.GetPostDetailAsync(999))
            .ReturnsAsync(Result<PostDetailDto>.Failure("Not found"));

        // Act
        var result = await _controller.Edit(999);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
    }

    #endregion
}
