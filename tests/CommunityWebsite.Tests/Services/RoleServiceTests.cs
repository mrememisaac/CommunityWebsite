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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Unit tests for RoleService following SOLID principles
/// </summary>
public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<ILogger<RoleService>> _mockLogger;
    private readonly RoleService _roleService;

    public RoleServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<RoleService>>();

        _roleService = new RoleService(
            _mockRoleRepository.Object,
            _mockMemoryCache.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRoleRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RoleService(null!, _mockMemoryCache.Object, _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("roleRepository");
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RoleService(_mockRoleRepository.Object, null!, _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RoleService(_mockRoleRepository.Object, _mockMemoryCache.Object, null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetRoleByIdAsync Tests

    [Fact]
    public async Task GetRoleByIdAsync_WithValidId_ReturnsRole()
    {
        // Arrange
        var role = CreateSampleRole(1, "TestRole");

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        // Act
        var result = await _roleService.GetRoleByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.Name.Should().Be("TestRole");
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _roleService.GetRoleByIdAsync(-1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithNonExistentId_ReturnsFailure()
    {
        // Arrange
        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _roleService.GetRoleByIdAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetRoleByNameAsync Tests

    [Fact]
    public async Task GetRoleByNameAsync_WithValidName_ReturnsRole()
    {
        // Arrange
        var role = CreateSampleRole(1, "Admin");

        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(role);

        // Act
        var result = await _roleService.GetRoleByNameAsync("Admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task GetRoleByNameAsync_WithEmptyName_ReturnsFailure()
    {
        // Act
        var result = await _roleService.GetRoleByNameAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task GetRoleByNameAsync_WithNonExistentName_ReturnsFailure()
    {
        // Arrange
        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("NonExistent"))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _roleService.GetRoleByNameAsync("NonExistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetAllRolesAsync Tests

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRolesWithUserCounts()
    {
        // Arrange
        var roles = new List<Role>
        {
            CreateRoleWithUsers(1, "Admin", 2),
            CreateRoleWithUsers(2, "User", 10),
            CreateRoleWithUsers(3, "Moderator", 3)
        };

        _mockRoleRepository
            .Setup(r => r.GetAllRolesWithUsersAsync())
            .ReturnsAsync(roles);

        // Act
        var result = await _roleService.GetAllRolesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data!.First(r => r.Name == "Admin").UserCount.Should().Be(2);
        result.Data!.First(r => r.Name == "User").UserCount.Should().Be(10);
    }

    [Fact]
    public async Task GetAllRolesAsync_WithEmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        _mockRoleRepository
            .Setup(r => r.GetAllRolesWithUsersAsync())
            .ReturnsAsync(new List<Role>());

        // Act
        var result = await _roleService.GetAllRolesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    #endregion

    #region CreateRoleAsync Tests

    [Fact]
    public async Task CreateRoleAsync_WithValidRequest_ReturnsCreatedRole()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "NewRole",
            Description = "A new custom role"
        };

        _mockRoleRepository
            .Setup(r => r.RoleExistsAsync("NewRole"))
            .ReturnsAsync(false);

        _mockRoleRepository
            .Setup(r => r.AddAsync(It.IsAny<Role>()))
            .ReturnsAsync((Role r) => { r.Id = 1; return r; });

        _mockRoleRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _roleService.CreateRoleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("NewRole");
        result.Data.Description.Should().Be("A new custom role");
    }

    [Fact]
    public async Task CreateRoleAsync_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateRoleRequest { Name = "" };

        // Act
        var result = await _roleService.CreateRoleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task CreateRoleAsync_WithTooShortName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateRoleRequest { Name = "A" };

        // Act
        var result = await _roleService.CreateRoleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("between 2 and 50");
    }

    [Fact]
    public async Task CreateRoleAsync_WithTooLongName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateRoleRequest { Name = new string('A', 51) };

        // Act
        var result = await _roleService.CreateRoleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("between 2 and 50");
    }

    [Fact]
    public async Task CreateRoleAsync_WithExistingName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateRoleRequest { Name = "Admin" };

        _mockRoleRepository
            .Setup(r => r.RoleExistsAsync("Admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _roleService.CreateRoleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    #endregion

    #region UpdateRoleAsync Tests

    [Fact]
    public async Task UpdateRoleAsync_WithValidRequest_ReturnsUpdatedRole()
    {
        // Arrange
        var role = CreateSampleRole(1, "CustomRole");
        var request = new UpdateRoleRequest
        {
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("UpdatedRole"))
            .ReturnsAsync((Role?)null);

        _mockRoleRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Role>()))
            .ReturnsAsync((Role r) => r);

        _mockRoleRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _roleService.UpdateRoleAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoleAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _roleService.UpdateRoleAsync(-1, new UpdateRoleRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task UpdateRoleAsync_WithNonExistentRole_ReturnsFailure()
    {
        // Arrange
        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _roleService.UpdateRoleAsync(999, new UpdateRoleRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateRoleAsync_RenamingProtectedRole_ReturnsFailure()
    {
        // Arrange
        var role = CreateSampleRole(1, "Admin");
        var request = new UpdateRoleRequest { Name = "SuperAdmin" };

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        // Act
        var result = await _roleService.UpdateRoleAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("protected role");
    }

    [Fact]
    public async Task UpdateRoleAsync_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var role = CreateSampleRole(1, "CustomRole");
        var existingRole = CreateSampleRole(2, "ExistingRole");
        var request = new UpdateRoleRequest { Name = "ExistingRole" };

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("ExistingRole"))
            .ReturnsAsync(existingRole);

        // Act
        var result = await _roleService.UpdateRoleAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    #endregion

    #region DeleteRoleAsync Tests

    [Fact]
    public async Task DeleteRoleAsync_WithValidCustomRole_ReturnsSuccess()
    {
        // Arrange
        var role = CreateSampleRole(1, "CustomRole");

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetUsersInRoleAsync(1))
            .ReturnsAsync(new List<User>());

        _mockRoleRepository
            .Setup(r => r.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        _mockRoleRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _roleService.DeleteRoleAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRoleAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _roleService.DeleteRoleAsync(-1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task DeleteRoleAsync_WithProtectedRole_ReturnsFailure()
    {
        // Arrange
        var role = CreateSampleRole(1, "Admin");

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        // Act
        var result = await _roleService.DeleteRoleAsync(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("protected role");
    }

    [Fact]
    public async Task DeleteRoleAsync_WithUsersAssigned_ReturnsFailure()
    {
        // Arrange
        var role = CreateSampleRole(1, "CustomRole");
        var users = new List<User> { CreateSampleUser(1) };

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetUsersInRoleAsync(1))
            .ReturnsAsync(users);

        // Act
        var result = await _roleService.DeleteRoleAsync(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("has users assigned");
    }

    #endregion

    #region GetUsersInRoleAsync Tests

    [Fact]
    public async Task GetUsersInRoleAsync_WithValidRoleId_ReturnsUsers()
    {
        // Arrange
        var role = CreateSampleRole(1, "Admin");
        var users = new List<User>
        {
            CreateSampleUser(1),
            CreateSampleUser(2)
        };

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetUsersInRoleAsync(1))
            .ReturnsAsync(users);

        // Act
        var result = await _roleService.GetUsersInRoleAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _roleService.GetUsersInRoleAsync(-1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithNonExistentRole_ReturnsFailure()
    {
        // Arrange
        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _roleService.GetUsersInRoleAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region GetUsersInRoleByNameAsync Tests

    [Fact]
    public async Task GetUsersInRoleByNameAsync_WithValidName_ReturnsUsers()
    {
        // Arrange
        var role = CreateSampleRole(1, "Admin");
        var users = new List<User>
        {
            CreateSampleUser(1),
            CreateSampleUser(2)
        };

        _mockRoleRepository
            .Setup(r => r.GetRoleByNameAsync("Admin"))
            .ReturnsAsync(role);

        _mockRoleRepository
            .Setup(r => r.GetUsersInRoleAsync(1))
            .ReturnsAsync(users);

        // Act
        var result = await _roleService.GetUsersInRoleByNameAsync("Admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersInRoleByNameAsync_WithEmptyName_ReturnsFailure()
    {
        // Act
        var result = await _roleService.GetUsersInRoleByNameAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    #endregion

    #region Helper Methods

    private static Role CreateSampleRole(int id, string name)
    {
        return new Role
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}"
        };
    }

    private static Role CreateRoleWithUsers(int id, string name, int userCount)
    {
        var role = CreateSampleRole(id, name);
        for (int i = 0; i < userCount; i++)
        {
            var user = CreateSampleUser(i + 1);
            role.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        }
        return role;
    }

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

    #endregion
}
