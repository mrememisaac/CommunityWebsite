using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Web.Controllers;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Unit tests for AdminUsersController API endpoints
/// </summary>
public class AdminUsersControllerTests
{
    private readonly Mock<IAdminUserService> _mockAdminUserService;
    private readonly Mock<ILogger<AdminUsersController>> _mockLogger;
    private readonly AdminUsersController _controller;

    public AdminUsersControllerTests()
    {
        _mockAdminUserService = new Mock<IAdminUserService>();
        _mockLogger = new Mock<ILogger<AdminUsersController>>();
        _controller = new AdminUsersController(_mockAdminUserService.Object, _mockLogger.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_WithValidRequest_ReturnsOkWithUsers()
    {
        // Arrange
        var users = CreateSampleAdminUserDtos(3);
        var pagedResult = new PagedResult<AdminUserDto>
        {
            Items = users,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 20
        };
        _mockAdminUserService
            .Setup(s => s.GetAllUsersAsync(1, 20, null))
            .ReturnsAsync(Result<PagedResult<AdminUserDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedData = okResult.Value.Should().BeOfType<PagedResult<AdminUserDto>>().Subject;
        returnedData.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllUsers_WithSearchTerm_PassesSearchToService()
    {
        // Arrange
        var users = CreateSampleAdminUserDtos(1);
        var pagedResult = new PagedResult<AdminUserDto>
        {
            Items = users,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };
        _mockAdminUserService
            .Setup(s => s.GetAllUsersAsync(1, 20, "test"))
            .ReturnsAsync(Result<PagedResult<AdminUserDto>>.Success(pagedResult));

        // Act
        await _controller.GetAllUsers(searchTerm: "test");

        // Assert
        _mockAdminUserService.Verify(s => s.GetAllUsersAsync(1, 20, "test"), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WithPagination_PassesPaginationToService()
    {
        // Arrange
        var users = CreateSampleAdminUserDtos(2);
        var pagedResult = new PagedResult<AdminUserDto>
        {
            Items = users,
            TotalCount = 5,
            PageNumber = 2,
            PageSize = 10
        };
        _mockAdminUserService
            .Setup(s => s.GetAllUsersAsync(2, 10, null))
            .ReturnsAsync(Result<PagedResult<AdminUserDto>>.Success(pagedResult));

        // Act
        await _controller.GetAllUsers(pageNumber: 2, pageSize: 10);

        // Assert
        _mockAdminUserService.Verify(s => s.GetAllUsersAsync(2, 10, null), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<PagedResult<AdminUserDto>>.Failure("Error message"));

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Error message");
    }

    #endregion

    #region GetUserDetail Tests

    [Fact]
    public async Task GetUserDetail_WithValidId_ReturnsOkWithUserDetail()
    {
        // Arrange
        var userDetail = CreateSampleAdminUserDetailDto(1, "testuser");
        _mockAdminUserService
            .Setup(s => s.GetUserDetailAsync(1))
            .ReturnsAsync(Result<AdminUserDetailDto>.Success(userDetail));

        // Act
        var result = await _controller.GetUserDetail(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<AdminUserDetailDto>().Subject;
        returnedUser.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserDetail_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.GetUserDetailAsync(999))
            .ReturnsAsync(Result<AdminUserDetailDto>.Failure("User not found"));

        // Act
        var result = await _controller.GetUserDetail(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region AssignRole Tests

    [Fact]
    public async Task AssignRole_WithValidUserAndRole_ReturnsOk()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.AssignRoleToUserAsync(1, "Admin"))
            .ReturnsAsync(Result<string>.Success("Role assigned successfully"));

        // Act
        var result = await _controller.AssignRole(1, "Admin");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRole_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.AssignRoleToUserAsync(1, "Admin"))
            .ReturnsAsync(Result<string>.Failure("User already has this role"));

        // Act
        var result = await _controller.AssignRole(1, "Admin");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("User already has this role");
    }

    #endregion

    #region RemoveRole Tests

    [Fact]
    public async Task RemoveRole_WithValidUserAndRole_ReturnsOk()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.RemoveRoleFromUserAsync(1, "Moderator"))
            .ReturnsAsync(Result<string>.Success("Role removed successfully"));

        // Act
        var result = await _controller.RemoveRole(1, "Moderator");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveRole_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.RemoveRoleFromUserAsync(1, "Admin"))
            .ReturnsAsync(Result<string>.Failure("Cannot remove the last Admin"));

        // Act
        var result = await _controller.RemoveRole(1, "Admin");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Cannot remove the last Admin");
    }

    #endregion

    #region DeactivateUser Tests

    [Fact]
    public async Task DeactivateUser_WithActiveUser_ReturnsOk()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.DeactivateUserAsync(1))
            .ReturnsAsync(Result<string>.Success("User deactivated"));

        // Act
        var result = await _controller.DeactivateUser(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeactivateUser_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.DeactivateUserAsync(1))
            .ReturnsAsync(Result<string>.Failure("User is already inactive"));

        // Act
        var result = await _controller.DeactivateUser(1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ReactivateUser Tests

    [Fact]
    public async Task ReactivateUser_WithInactiveUser_ReturnsOk()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.ReactivateUserAsync(1))
            .ReturnsAsync(Result<string>.Success("User reactivated"));

        // Act
        var result = await _controller.ReactivateUser(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReactivateUser_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.ReactivateUserAsync(1))
            .ReturnsAsync(Result<string>.Failure("User is already active"));

        // Act
        var result = await _controller.ReactivateUser(1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetAvailableRoles Tests

    [Fact]
    public async Task GetAvailableRoles_ReturnsOkWithRoles()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = 1, Name = "Admin" },
            new() { Id = 2, Name = "User" },
            new() { Id = 3, Name = "Moderator" }
        };

        _mockAdminUserService
            .Setup(s => s.GetAllRolesAsync())
            .ReturnsAsync(Result<IEnumerable<RoleDto>>.Success(roles));

        // Act
        var result = await _controller.GetAvailableRoles();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value.Should().BeAssignableTo<IEnumerable<RoleDto>>().Subject;
        returnedRoles.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAvailableRoles_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _mockAdminUserService
            .Setup(s => s.GetAllRolesAsync())
            .ReturnsAsync(Result<IEnumerable<RoleDto>>.Failure("Database error"));

        // Act
        var result = await _controller.GetAvailableRoles();

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static List<AdminUserDto> CreateSampleAdminUserDtos(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new AdminUserDto
            {
                Id = i,
                Username = $"user{i}",
                Email = $"user{i}@example.com",
                Roles = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PostCount = i * 2,
                CommentCount = i * 3
            })
            .ToList();
    }

    private static AdminUserDetailDto CreateSampleAdminUserDetailDto(int id, string username)
    {
        return new AdminUserDetailDto
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.com",
            Bio = "Test bio",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Roles = new List<RoleDto>
            {
                new() { Id = 1, Name = "User" }
            },
            PostCount = 5,
            CommentCount = 10
        };
    }

    #endregion
}
