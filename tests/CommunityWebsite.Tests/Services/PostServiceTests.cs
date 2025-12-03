using Xunit;
using FluentAssertions;
using Moq;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Unit tests for PostService following SOLID principles
/// - Single Responsibility: Each test verifies one behavior
/// - Open/Closed: Tests are closed for modification, open for extension
/// - Liskov Substitution: Mocks implement same interface as real implementations
/// - Interface Segregation: Uses only required interfaces
/// - Dependency Inversion: Depends on abstractions, not concrete classes
/// </summary>
public class PostServiceTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPostValidator> _mockPostValidator;
    private readonly Mock<ILogger<PostService>> _mockLogger;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPostValidator = new Mock<IPostValidator>();
        _mockLogger = new Mock<ILogger<PostService>>();

        _postService = new PostService(
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            _mockPostValidator.Object,
            _mockLogger.Object);
    }

    #region GetPostDetailAsync Tests

    [Fact]
    public async Task GetPostDetailAsync_WithValidId_ReturnsPostDetail()
    {
        // Arrange
        var postId = 1;
        var post = CreateSamplePost(postId);

        _mockPostRepository
            .Setup(r => r.GetPostWithCommentsAsync(postId))
            .ReturnsAsync(post);

        _mockPostRepository
            .Setup(r => r.IncrementViewCountAsync(postId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.GetPostDetailAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(postId);
        result.Data.Title.Should().Be(post.Title);
        result.ErrorMessage.Should().BeNull();

        _mockPostRepository.Verify(
            r => r.GetPostWithCommentsAsync(postId),
            Times.Once);
    }

    [Fact]
    public async Task GetPostDetailAsync_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var postId = -1;

        // Act
        var result = await _postService.GetPostDetailAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetPostDetailAsync_WithNonExistentId_ReturnsSuccessWithNullData()
    {
        // Arrange
        var postId = 999;

        _mockPostRepository
            .Setup(r => r.GetPostWithCommentsAsync(postId))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _postService.GetPostDetailAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    #endregion

    #region CreatePostAsync Tests

    [Fact]
    public async Task CreatePostAsync_WithValidRequest_CreatesPost()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);
        var request = new CreatePostRequest
        {
            Title = "Test Post",
            Content = "This is test content for a valid post",
            AuthorId = userId,
            Category = "General"
        };

        _mockPostValidator
            .Setup(v => v.ValidateCreateRequestAsync(request))
            .ReturnsAsync(Result.Success());

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var createdPost = new Post
        {
            Id = 1,
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId,
            Category = request.Category,
            Author = user
        };

        _mockPostRepository
            .Setup(r => r.AddAsync(It.IsAny<Post>()))
            .ReturnsAsync(createdPost);

        _mockPostRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _postService.CreatePostAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(request.Title);
        result.Data.Content.Should().Be(request.Content);
        result.ErrorMessage.Should().BeNull();

        _mockPostValidator.Verify(
            v => v.ValidateCreateRequestAsync(request),
            Times.Once);

        _mockPostRepository.Verify(
            r => r.AddAsync(It.IsAny<Post>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePostAsync_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Title = "",
            Content = "",
            AuthorId = 1
        };

        var validationError = "Post title is required";
        _mockPostValidator
            .Setup(v => v.ValidateCreateRequestAsync(request))
            .ReturnsAsync(Result.Failure(validationError));

        // Act
        var result = await _postService.CreatePostAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(validationError);
        result.Data.Should().BeNull();

        _mockPostRepository.Verify(
            r => r.AddAsync(It.IsAny<Post>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePostAsync_WithNonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Title = "Test",
            Content = "Valid content here",
            AuthorId = 999
        };

        _mockPostValidator
            .Setup(v => v.ValidateCreateRequestAsync(request))
            .ReturnsAsync(Result.Success());

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(request.AuthorId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _postService.CreatePostAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region DeletePostAsync Tests

    [Fact]
    public async Task DeletePostAsync_WithValidId_SoftDeletesPost()
    {
        // Arrange
        var postId = 1;
        var post = CreateSamplePost(postId);

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _mockPostRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Post>()))
            .ReturnsAsync(post);

        _mockPostRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _postService.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        _mockPostRepository.Verify(
            r => r.UpdateAsync(It.Is<Post>(p => p.IsDeleted)),
            Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_WithNonExistentId_ReturnsBadRequest()
    {
        // Arrange
        var postId = 999;

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _postService.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region SearchPostsAsync Tests

    [Fact]
    public async Task SearchPostsAsync_WithValidSearchTerm_ReturnsResults()
    {
        // Arrange
        var searchTerm = "test";
        var posts = new List<Post>
        {
            CreateSamplePost(1, "Test Post 1"),
            CreateSamplePost(2, "Test Post 2")
        };

        _mockPostRepository
            .Setup(r => r.SearchPostsAsync(searchTerm))
            .ReturnsAsync(posts);

        // Act
        var result = await _postService.SearchPostsAsync(searchTerm);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SearchPostsAsync_WithEmptySearchTerm_ReturnsBadRequest()
    {
        // Act
        var result = await _postService.SearchPostsAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private Post CreateSamplePost(int id = 1, string? title = null)
    {
        return new Post
        {
            Id = id,
            Title = title ?? $"Sample Post {id}",
            Content = $"Sample content for post {id}",
            Category = "General",
            Author = CreateSampleUser(),
            AuthorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 10,
            Comments = new List<Comment>()
        };
    }

    private User CreateSampleUser(int id = 1)
    {
        return new User
        {
            Id = id,
            Username = $"testuser{id}",
            Email = $"test{id}@example.com",
            PasswordHash = "hashed_password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
