using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for UserRole domain model
/// </summary>
public class UserRoleModelTests
{
    [Fact]
    public void UserRole_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var userRole = new UserRole { UserId = 1, RoleId = 1 };

        // Assert
        userRole.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserRole_AssignmentWorks()
    {
        // Arrange
        var user = new User { Id = 1, Username = "admin", Email = "admin@test.com", PasswordHash = "hash" };
        var role = new Role { Id = 1, Name = "Admin" };
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role };

        user.UserRoles.Add(userRole);
        role.UserRoles.Add(userRole);

        // Act & Assert
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().Role.Name.Should().Be("Admin");
        role.UserRoles.Should().HaveCount(1);
        role.UserRoles.First().User.Username.Should().Be("admin");
    }

    [Fact]
    public void UserRole_MultipleRolesPerUser_Works()
    {
        // Arrange
        var user = new User { Id = 1, Username = "superuser", Email = "super@test.com", PasswordHash = "hash" };
        var adminRole = new Role { Id = 1, Name = "Admin" };
        var modRole = new Role { Id = 2, Name = "Moderator" };

        var ur1 = new UserRole { UserId = 1, RoleId = 1, User = user, Role = adminRole };
        var ur2 = new UserRole { UserId = 1, RoleId = 2, User = user, Role = modRole };

        user.UserRoles.Add(ur1);
        user.UserRoles.Add(ur2);

        // Assert
        user.UserRoles.Should().HaveCount(2);
        user.UserRoles.Select(ur => ur.Role.Name).Should().Contain("Admin", "Moderator");
    }
}
