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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Unit tests for UserService following SOLID principles
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // Configure mock cache - TryGetValue will always return false (cache miss)
        // This forces all operations to hit the repository
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Returns(false);

        // Configure mock cache - Set should accept any calls
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(new Mock<ICacheEntry>().Object);

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockPostRepository.Object,
            _mockMemoryCache.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullUserRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new UserService(
            null!,
            _mockPostRepository.Object,
            _mockMemoryCache.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("userRepository");
    }

    [Fact]
    public void Constructor_WithNullPostRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new UserService(
            _mockUserRepository.Object,
            null!,
            _mockMemoryCache.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("postRepository");
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new UserService(
            _mockUserRepository.Object,
            _mockPostRepository.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new UserService(
            _mockUserRepository.Object,
            _mockPostRepository.Object,
            _mockMemoryCache.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetUserProfileAsync Tests

    [Fact]
    public async Task GetUserProfileAsync_WithValidId_ReturnsProfile()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUserWithRoles(userId);
        var posts = new List<Post> { CreateSamplePost(1, userId), CreateSamplePost(2, userId) };

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(userId))
            .ReturnsAsync(user);

        var pagedPosts = new PagedResult<Post>
        {
            Items = posts,
            TotalCount = posts.Count,
            PageNumber = 1,
            PageSize = 20
        };
        _mockPostRepository
            .Setup(r => r.GetUserPostsAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var result = await _userService.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
        result.Data.Username.Should().Be(user.Username);
        result.Data.PostCount.Should().Be(2);
        result.Data.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetUserProfileAsync(-1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNonExistentId_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserProfileAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = CreateSampleUser(1);
        user.Email = email;
        var userWithRoles = CreateSampleUserWithRoles(1);

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(userWithRoles);

        var pagedPosts = new PagedResult<Post>
        {
            Items = new List<Post>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };
        _mockPostRepository
            .Setup(r => r.GetUserPostsAsync(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithEmptyEmail_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistentEmail_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync("notfound@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByEmailAsync("notfound@example.com");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetUserByUsernameAsync Tests

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
    {
        // Arrange
        var username = "testuser";
        var user = CreateSampleUser(1);
        user.Username = username;
        var userWithRoles = CreateSampleUserWithRoles(1);

        _mockUserRepository
            .Setup(r => r.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(userWithRoles);

        var pagedPosts = new PagedResult<Post>
        {
            Items = new List<Post>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };
        _mockPostRepository
            .Setup(r => r.GetUserPostsAsync(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var result = await _userService.GetUserByUsernameAsync(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Username.Should().Be(username);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithEmptyUsername_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetUserByUsernameAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    #endregion

    #region GetActiveUsersAsync Tests

    [Fact]
    public async Task GetActiveUsersAsync_WithValidParams_ReturnsUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateSampleUser(1),
            CreateSampleUser(2),
            CreateSampleUser(3)
        };

        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = users.Count,
            PageNumber = 1,
            PageSize = 20
        };

        _mockUserRepository
            .Setup(r => r.GetActiveUsersAsync(1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _userService.GetActiveUsersAsync(1, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithInvalidPageNumber_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetActiveUsersAsync(0, 20);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Page number");
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithInvalidPageSize_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetActiveUsersAsync(1, 101);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Page size");
    }

    #endregion

    #region GetUsersByRoleAsync Tests

    [Fact]
    public async Task GetUsersByRoleAsync_WithValidRole_ReturnsUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateSampleUser(1),
            CreateSampleUser(2)
        };

        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = users.Count,
            PageNumber = 1,
            PageSize = 20
        };

        _mockUserRepository
            .Setup(r => r.GetUsersByRoleAsync("Admin", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _userService.GetUsersByRoleAsync("Admin", 1, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersByRoleAsync_WithEmptyRole_ReturnsFailure()
    {
        // Act
        var result = await _userService.GetUsersByRoleAsync("", It.IsAny<int>(), It.IsAny<int>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    [Fact]
    public async Task UpdateUserProfileAsync_WithValidRequest_ReturnsUpdatedProfile()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);
        var userWithRoles = CreateSampleUserWithRoles(userId);

        var request = new UpdateUserProfileRequest
        {
            Bio = "Updated bio",
            ProfileImageUrl = "https://example.com/new-image.jpg"
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(userId))
            .ReturnsAsync(userWithRoles);

        _mockPostRepository
            .Setup(r => r.GetUserPostsAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<Post> { Items = new List<Post>(), PageNumber = 1, PageSize = 20, TotalCount = 0 });

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _userService.UpdateUserProfileAsync(-1, new UpdateUserProfileRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUserProfileAsync(999, new UpdateUserProfileRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WithValidUser_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateUserAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _userService.DeactivateUserAsync(-1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task DeactivateUserAsync_WithAlreadyDeactivated_ReturnsFailure()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);
        user.IsActive = false;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already deactivated");
    }

    #endregion

    #region ReactivateUserAsync Tests

    [Fact]
    public async Task ReactivateUserAsync_WithDeactivatedUser_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);
        user.IsActive = false;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _userService.ReactivateUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateUserAsync_WithAlreadyActive_ReturnsFailure()
    {
        // Arrange
        var userId = 1;
        var user = CreateSampleUser(userId);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ReactivateUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already active");
    }

    #endregion

    #region Helper Methods

    private static User CreateSampleUser(int id)
    {
        return new User
        {
            Id = id,
            Username = $"user{id}",
            Email = $"user{id}@example.com",
            PasswordHash = "hashedpassword",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static User CreateSampleUserWithRoles(int id)
    {
        var user = CreateSampleUser(id);
        var role = new Role { Id = 1, Name = "User" };
        var userRole = new UserRole { UserId = id, RoleId = 1, User = user, Role = role };
        user.UserRoles.Add(userRole);
        return user;
    }

    private static Post CreateSamplePost(int id, int authorId)
    {
        return new Post
        {
            Id = id,
            Title = $"Post {id}",
            Content = "Content",
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
