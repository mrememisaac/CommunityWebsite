using Xunit;
using CommunityWebsite.Core.DTOs;
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
/// Unit tests for CommentService following SOLID principles
/// Tests comment business logic in isolation using mocks
/// </summary>
public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _mockCommentRepository;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICommentValidator> _mockCommentValidator;
    private readonly Mock<ILogger<CommentService>> _mockLogger;
    private readonly CommentService _commentService;

    public CommentServiceTests()
    {
        _mockCommentRepository = new Mock<ICommentRepository>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCommentValidator = new Mock<ICommentValidator>();
        _mockLogger = new Mock<ILogger<CommentService>>();

        _commentService = new CommentService(
            _mockCommentRepository.Object,
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            _mockCommentValidator.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCommentRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommentService(
            null!,
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            _mockCommentValidator.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("commentRepository");
    }

    [Fact]
    public void Constructor_WithNullPostRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommentService(
            _mockCommentRepository.Object,
            null!,
            _mockUserRepository.Object,
            _mockCommentValidator.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("postRepository");
    }

    [Fact]
    public void Constructor_WithNullUserRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommentService(
            _mockCommentRepository.Object,
            _mockPostRepository.Object,
            null!,
            _mockCommentValidator.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("userRepository");
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommentService(
            _mockCommentRepository.Object,
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("commentValidator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommentService(
            _mockCommentRepository.Object,
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            _mockCommentValidator.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetPostCommentsAsync Tests

    [Fact]
    public async Task GetPostCommentsAsync_WithValidPostId_ReturnsComments()
    {
        // Arrange
        var postId = 1;
        var post = CreateSamplePost(postId);
        var comments = CreateSampleComments(postId);

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _mockCommentRepository
            .Setup(r => r.GetPostCommentsAsync(postId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Comment> { Items = comments, TotalCount = comments.Count, PageNumber = 1, PageSize = 20 });

        // Act
        var result = await _commentService.GetPostCommentsAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(3); // 2 top-level comments + 1 reply
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetPostCommentsAsync_WithNonExistentPost_ReturnsFailure()
    {
        // Arrange
        var postId = 999;

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _commentService.GetPostCommentsAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPostCommentsAsync_WithDeletedPost_ReturnsFailure()
    {
        // Arrange
        var postId = 1;
        var post = CreateSamplePost(postId);
        post.IsDeleted = true;

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        var result = await _commentService.GetPostCommentsAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetCommentDetailAsync Tests

    [Fact]
    public async Task GetCommentDetailAsync_WithValidId_ReturnsCommentDetail()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1);
        comment.Replies = new List<Comment>
        {
            CreateSampleComment(2, 1, parentId: commentId),
            CreateSampleComment(3, 1, parentId: commentId)
        };

        _mockCommentRepository
            .Setup(r => r.GetCommentWithRepliesAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.GetCommentDetailAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(commentId);
        result.Data.Replies.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCommentDetailAsync_WithNonExistentId_ReturnsFailure()
    {
        // Arrange
        var commentId = 999;

        _mockCommentRepository
            .Setup(r => r.GetCommentWithRepliesAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        var result = await _commentService.GetCommentDetailAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task GetCommentDetailAsync_FiltersDeletedReplies()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1);
        comment.Replies = new List<Comment>
        {
            CreateSampleComment(2, 1, parentId: commentId),
            CreateSampleComment(3, 1, parentId: commentId, isDeleted: true)
        };

        _mockCommentRepository
            .Setup(r => r.GetCommentWithRepliesAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.GetCommentDetailAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Replies.Should().HaveCount(1); // Deleted reply filtered out
    }

    #endregion

    #region CreateCommentAsync Tests

    [Fact]
    public async Task CreateCommentAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var postId = 1;
        var userId = 1;
        var post = CreateSamplePost(postId);
        var user = CreateSampleUser(userId);
        var request = new CreateCommentRequest
        {
            Content = "This is a valid comment.",
            AuthorId = userId
        };

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockCommentValidator
            .Setup(v => v.ValidateComment(It.IsAny<Comment>()))
            .Returns(Result.Success());

        _mockCommentRepository
            .Setup(r => r.AddAsync(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) => c);

        _mockCommentRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _commentService.CreateCommentAsync(postId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Content.Should().Be(request.Content);
    }

    [Fact]
    public async Task CreateCommentAsync_WithNonExistentPost_ReturnsFailure()
    {
        // Arrange
        var postId = 999;
        var request = new CreateCommentRequest
        {
            Content = "This comment should fail.",
            AuthorId = 1
        };

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _commentService.CreateCommentAsync(postId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateCommentAsync_OnLockedPost_ReturnsFailure()
    {
        // Arrange
        var postId = 1;
        var post = CreateSamplePost(postId);
        post.IsLocked = true;

        var request = new CreateCommentRequest
        {
            Content = "Trying to comment on locked post.",
            AuthorId = 1
        };

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        var result = await _commentService.CreateCommentAsync(postId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("locked");
    }

    [Fact]
    public async Task CreateCommentAsync_WithInvalidAuthor_ReturnsFailure()
    {
        // Arrange
        var postId = 1;
        var userId = 999;
        var post = CreateSamplePost(postId);

        var request = new CreateCommentRequest
        {
            Content = "Comment with invalid author.",
            AuthorId = userId
        };

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _commentService.CreateCommentAsync(postId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    [Fact]
    public async Task CreateCommentAsync_WithInvalidContent_ReturnsFailure()
    {
        // Arrange
        var postId = 1;
        var userId = 1;
        var post = CreateSamplePost(postId);
        var user = CreateSampleUser(userId);

        var request = new CreateCommentRequest
        {
            Content = "",
            AuthorId = userId
        };

        _mockPostRepository
            .Setup(r => r.GetByIdAsync(postId))
            .ReturnsAsync(post);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockCommentValidator
            .Setup(v => v.ValidateComment(It.IsAny<Comment>()))
            .Returns(Result.Failure("Content is required"));

        // Act
        var result = await _commentService.CreateCommentAsync(postId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Content");
    }

    #endregion

    #region UpdateCommentAsync Tests

    [Fact]
    public async Task UpdateCommentAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1);

        var request = new UpdateCommentRequest
        {
            Content = "Updated comment content."
        };

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        _mockCommentValidator
            .Setup(v => v.ValidateComment(It.IsAny<Comment>()))
            .Returns(Result.Success());

        _mockCommentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) => c);

        _mockCommentRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Mock reload with author for the response
        var updatedComment = CreateSampleComment(commentId, 1);
        updatedComment.Content = request.Content;
        _mockCommentRepository
            .Setup(r => r.GetCommentWithRepliesAsync(commentId))
            .ReturnsAsync(updatedComment);

        // Act
        var result = await _commentService.UpdateCommentAsync(commentId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Content.Should().Be(request.Content);
    }

    [Fact]
    public async Task UpdateCommentAsync_WithNonExistentComment_ReturnsFailure()
    {
        // Arrange
        var commentId = 999;
        var request = new UpdateCommentRequest
        {
            Content = "This update should fail."
        };

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        var result = await _commentService.UpdateCommentAsync(commentId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateCommentAsync_WithDeletedComment_ReturnsFailure()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1, isDeleted: true);

        var request = new UpdateCommentRequest
        {
            Content = "Trying to update deleted comment."
        };

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.UpdateCommentAsync(commentId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region DeleteCommentAsync Tests

    [Fact]
    public async Task DeleteCommentAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1);

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        _mockCommentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) => c);

        _mockCommentRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _commentService.DeleteCommentAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCommentAsync_WithNonExistentComment_ReturnsFailure()
    {
        // Arrange
        var commentId = 999;

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        var result = await _commentService.DeleteCommentAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteCommentAsync_SetsIsDeletedTrue()
    {
        // Arrange
        var commentId = 1;
        var comment = CreateSampleComment(commentId, 1);
        Comment? updatedComment = null;

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        _mockCommentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => updatedComment = c)
            .ReturnsAsync((Comment c) => c);

        _mockCommentRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _commentService.DeleteCommentAsync(commentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        updatedComment.Should().NotBeNull();
        updatedComment!.IsDeleted.Should().BeTrue();
    }

    #endregion

    #region VerifyOwnershipAsync Tests

    [Fact]
    public async Task VerifyOwnershipAsync_WithOwner_ReturnsTrue()
    {
        // Arrange
        var commentId = 1;
        var userId = 1;
        var comment = CreateSampleComment(commentId, 1);
        comment.AuthorId = userId;

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.VerifyOwnershipAsync(commentId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyOwnershipAsync_WithNonOwner_ReturnsFalse()
    {
        // Arrange
        var commentId = 1;
        var ownerId = 1;
        var otherUserId = 2;
        var comment = CreateSampleComment(commentId, 1);
        comment.AuthorId = ownerId;

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _commentService.VerifyOwnershipAsync(commentId, otherUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyOwnershipAsync_WithNonExistentComment_ReturnsFailure()
    {
        // Arrange
        var commentId = 999;
        var userId = 1;

        _mockCommentRepository
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        var result = await _commentService.VerifyOwnershipAsync(commentId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region Helper Methods

    private static Post CreateSamplePost(int id)
    {
        return new Post
        {
            Id = id,
            Title = $"Test Post {id}",
            Content = "Test content for the post.",
            AuthorId = 1,
            Author = CreateSampleUser(1),
            Category = "Testing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            IsPinned = false,
            IsLocked = false,
            IsDeleted = false
        };
    }

    private static User CreateSampleUser(int id)
    {
        return new User
        {
            Id = id,
            Username = $"testuser{id}",
            Email = $"testuser{id}@example.com",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    private static Comment CreateSampleComment(int id, int postId, int? parentId = null, bool isDeleted = false)
    {
        return new Comment
        {
            Id = id,
            Content = $"Test comment {id}",
            PostId = postId,
            AuthorId = 1,
            Author = CreateSampleUser(1),
            ParentCommentId = parentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = isDeleted
        };
    }

    private static List<Comment> CreateSampleComments(int postId)
    {
        return new List<Comment>
        {
            CreateSampleComment(1, postId),
            CreateSampleComment(2, postId),
            CreateSampleComment(3, postId, parentId: 1) // Reply to comment 1
        };
    }

    #endregion
}
