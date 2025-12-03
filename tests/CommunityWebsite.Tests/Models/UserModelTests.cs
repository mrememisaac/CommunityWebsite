using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for User domain model
/// </summary>
public class UserModelTests
{
    [Fact]
    public void User_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password"
        };

        // Assert
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.Posts.Should().BeEmpty();
        user.Comments.Should().BeEmpty();
        user.UserRoles.Should().BeEmpty();
    }

    [Fact]
    public void User_OptionalFields_CanBeNull()
    {
        // Arrange & Act
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        // Assert
        user.Bio.Should().BeNull();
        user.ProfileImageUrl.Should().BeNull();
    }

    [Fact]
    public void User_WithPosts_NavigationWorks()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "author",
            Email = "author@example.com",
            PasswordHash = "hash"
        };

        var post1 = new Post { Id = 1, Title = "Post 1", Content = "Content 1", AuthorId = 1, Author = user };
        var post2 = new Post { Id = 2, Title = "Post 2", Content = "Content 2", AuthorId = 1, Author = user };

        user.Posts.Add(post1);
        user.Posts.Add(post2);

        // Assert
        user.Posts.Should().HaveCount(2);
        user.Posts.All(p => p.Author == user).Should().BeTrue();
    }

    [Fact]
    public void User_WithComments_NavigationWorks()
    {
        // Arrange
        var user = new User { Id = 1, Username = "commenter", Email = "c@test.com", PasswordHash = "hash" };
        var comment = new Comment { Id = 1, Content = "Comment", PostId = 1, AuthorId = 1, Author = user };

        user.Comments.Add(comment);

        // Assert
        user.Comments.Should().HaveCount(1);
        user.Comments.First().Author.Should().Be(user);
    }

    [Fact]
    public void User_WithRoles_NavigationWorks()
    {
        // Arrange
        var user = new User { Id = 1, Username = "admin", Email = "admin@test.com", PasswordHash = "hash" };
        var adminRole = new Role { Id = 1, Name = "Admin" };
        var userRole = new UserRole { UserId = 1, RoleId = 1, User = user, Role = adminRole };

        user.UserRoles.Add(userRole);

        // Assert
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().Role.Name.Should().Be("Admin");
    }

    [Fact]
    public void User_DefaultStrings_AreEmpty()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Username.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
    }
}
