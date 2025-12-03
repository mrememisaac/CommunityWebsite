using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for Role domain model
/// </summary>
public class RoleModelTests
{
    [Fact]
    public void Role_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var role = new Role { Name = "Admin" };

        // Assert
        role.Name.Should().Be("Admin");
        role.Description.Should().BeNull();
        role.UserRoles.Should().BeEmpty();
    }

    [Fact]
    public void Role_WithDescription_SetsValue()
    {
        // Arrange & Act
        var role = new Role
        {
            Name = "Moderator",
            Description = "Content moderation rights"
        };

        // Assert
        role.Description.Should().Be("Content moderation rights");
    }

    [Fact]
    public void Role_WithMultipleUsers_NavigationWorks()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "User" };
        var user1 = new User { Id = 1, Username = "user1", Email = "u1@test.com", PasswordHash = "hash" };
        var user2 = new User { Id = 2, Username = "user2", Email = "u2@test.com", PasswordHash = "hash" };

        var ur1 = new UserRole { UserId = 1, RoleId = 1, User = user1, Role = role };
        var ur2 = new UserRole { UserId = 2, RoleId = 1, User = user2, Role = role };

        role.UserRoles.Add(ur1);
        role.UserRoles.Add(ur2);

        // Assert
        role.UserRoles.Should().HaveCount(2);
        role.UserRoles.Select(ur => ur.User.Username).Should().Contain("user1", "user2");
    }

    [Fact]
    public void Role_DefaultStrings_AreEmpty()
    {
        // Arrange & Act
        var role = new Role();

        // Assert
        role.Name.Should().BeEmpty();
    }
}
