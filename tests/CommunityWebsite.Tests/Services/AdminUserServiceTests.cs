using Xunit;
using FluentAssertions;
using Moq;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Unit tests for AdminUserService following SOLID principles
/// </summary>
public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<ICommentRepository> _mockCommentRepository;
    private readonly Mock<ILogger<AdminUserService>> _mockLogger;
    private readonly AdminUserService _adminUserService;

    public AdminUserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockCommentRepository = new Mock<ICommentRepository>();
        _mockLogger = new Mock<ILogger<AdminUserService>>();

        _adminUserService = new AdminUserService(
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            _mockPostRepository.Object,
            _mockCommentRepository.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullUserRepository_ThrowsArgumentNullException()
    {
        var action = () => new AdminUserService(
            null!,
            _mockRoleRepository.Object,
            _mockPostRepository.Object,
            _mockCommentRepository.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("userRepository");
    }

    [Fact]
    public void Constructor_WithNullRoleRepository_ThrowsArgumentNullException()
    {
        var action = () => new AdminUserService(
            _mockUserRepository.Object,
            null!,
            _mockPostRepository.Object,
            _mockCommentRepository.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("roleRepository");
    }

    [Fact]
    public void Constructor_WithNullPostRepository_ThrowsArgumentNullException()
    {
        var action = () => new AdminUserService(
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            null!,
            _mockCommentRepository.Object,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("postRepository");
    }

    [Fact]
    public void Constructor_WithNullCommentRepository_ThrowsArgumentNullException()
    {
        var action = () => new AdminUserService(
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            _mockPostRepository.Object,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("commentRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new AdminUserService(
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            _mockPostRepository.Object,
            _mockCommentRepository.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_WithValidPagination_ReturnsUsers()
    {
        // Arrange
        var users = CreateSampleUsers(3);
        _mockUserRepository
            .Setup(r => r.GetUsersWithStatsAsync(1, 20, null))
            .ReturnsAsync(new PagedResult<User> { Items = users, TotalCount = 3, PageNumber = 1, PageSize = 20 });

        SetupPostAndCommentCounts(users);

        // Act
        var result = await _adminUserService.GetAllUsersAsync(1, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithInvalidPageNumber_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.GetAllUsersAsync(0, 20);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Page number");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithInvalidPageSize_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.GetAllUsersAsync(1, 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Page size");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPageSizeOver100_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.GetAllUsersAsync(1, 101);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Page size");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var users = CreateSampleUsers(2);
        _mockUserRepository
            .Setup(r => r.GetUsersWithStatsAsync(1, 20, "test"))
            .ReturnsAsync(new PagedResult<User> { Items = users, TotalCount = 2, PageNumber = 1, PageSize = 20 });

        SetupPostAndCommentCounts(users);

        // Act
        var result = await _adminUserService.GetAllUsersAsync(1, 20, "test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockUserRepository.Verify(r => r.GetUsersWithStatsAsync(1, 20, "test"), Times.Once);
    }

    #endregion

    #region GetUserDetailAsync Tests

    [Fact]
    public async Task GetUserDetailAsync_WithValidId_ReturnsUserDetail()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockPostRepository
            .Setup(r => r.GetPostCountAsync(1))
            .ReturnsAsync(5);
        _mockCommentRepository
            .Setup(r => r.GetCommentCountByUserAsync(1))
            .ReturnsAsync(10);

        // Act
        var result = await _adminUserService.GetUserDetailAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("testuser");
        result.Data.PostCount.Should().Be(5);
        result.Data.CommentCount.Should().Be(10);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.GetUserDetailAsync(0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid user ID");
    }

    [Fact]
    public async Task GetUserDetailAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _adminUserService.GetUserDetailAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    #endregion

    #region AssignRoleToUserAsync Tests

    [Fact]
    public async Task AssignRoleToUserAsync_WithValidUserAndRole_AssignsRole()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        var role = new Role { Id = 1, Name = "Admin" };

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(role);
        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.AssignRoleToUserAsync(1, "Admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Contain("Admin");
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WithInvalidUserId_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.AssignRoleToUserAsync(0, "Admin");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid user ID");
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WithEmptyRoleName_ReturnsFailure()
    {
        // Act
        var result = await _adminUserService.AssignRoleToUserAsync(1, "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Role name is required");
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WhenUserAlreadyHasRole_ReturnsFailure()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Admin" };
        var user = CreateSampleUser(1, "testuser");
        user.UserRoles.Add(new UserRole { RoleId = 1, Role = role });

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(role);

        // Act
        var result = await _adminUserService.AssignRoleToUserAsync(1, "Admin");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already has");
    }

    #endregion

    #region RemoveRoleFromUserAsync Tests

    [Fact]
    public async Task RemoveRoleFromUserAsync_WithValidUserAndRole_RemovesRole()
    {
        // Arrange
        var role = new Role { Id = 2, Name = "Moderator" };
        var user = CreateSampleUser(1, "testuser");
        user.UserRoles.Add(new UserRole { RoleId = 2, Role = role });

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Moderator"))
            .ReturnsAsync(role);
        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.RemoveRoleFromUserAsync(1, "Moderator");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_WhenUserDoesNotHaveRole_ReturnsFailure()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Admin" };
        var user = CreateSampleUser(1, "testuser");

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(role);

        // Act
        var result = await _adminUserService.RemoveRoleFromUserAsync(1, "Admin");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not have");
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_WhenRemovingLastAdmin_ReturnsFailure()
    {
        // Arrange
        var adminRole = new Role { Id = 1, Name = "Admin" };
        var user = CreateSampleUser(1, "admin");
        user.UserRoles.Add(new UserRole { RoleId = 1, Role = adminRole });

        _mockUserRepository
            .Setup(r => r.GetUserWithRolesAsync(1))
            .ReturnsAsync(user);
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(adminRole);
        _mockUserRepository
            .Setup(r => r.GetUsersByRoleAsync("Admin", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<User> { Items = new List<User> { user }, TotalCount = 1, PageNumber = 1, PageSize = 20 });

        // Act
        var result = await _adminUserService.RemoveRoleFromUserAsync(1, "Admin");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot remove the last Admin");
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WithActiveUser_DeactivatesUser()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        user.IsActive = true;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);
        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.DeactivateUserAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        user.IsActive = false;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.DeactivateUserAsync(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already inactive");
    }

    [Fact]
    public async Task DeactivateUserAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _adminUserService.DeactivateUserAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    #endregion

    #region ReactivateUserAsync Tests

    [Fact]
    public async Task ReactivateUserAsync_WithInactiveUser_ReactivatesUser()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        user.IsActive = false;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);
        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.ReactivateUserAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateUserAsync_WithActiveUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateSampleUser(1, "testuser");
        user.IsActive = true;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _adminUserService.ReactivateUserAsync(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already active");
    }

    #endregion

    #region GetAllRolesAsync Tests

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Admin" },
            new() { Id = 2, Name = "User" },
            new() { Id = 3, Name = "Moderator" }
        };

        _mockRoleRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(roles);

        // Act
        var result = await _adminUserService.GetAllRolesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    #endregion

    #region Helper Methods

    private static User CreateSampleUser(int id, string username)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hashedpassword",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>()
        };
    }

    private static List<User> CreateSampleUsers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateSampleUser(i, $"user{i}"))
            .ToList();
    }

    private void SetupPostAndCommentCounts(List<User> users)
    {
        foreach (var user in users)
        {
            _mockPostRepository
                .Setup(r => r.GetPostCountAsync(user.Id))
                .ReturnsAsync(user.Id * 2);
            _mockCommentRepository
                .Setup(r => r.GetCommentCountByUserAsync(user.Id))
                .ReturnsAsync(user.Id * 3);
        }
    }

    #endregion
}
