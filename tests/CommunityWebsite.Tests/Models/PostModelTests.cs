using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for Post domain model
/// </summary>
public class PostModelTests
{
    [Fact]
    public void Post_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var post = new Post
        {
            Title = "Test Post",
            Content = "Test Content",
            AuthorId = 1
        };

        // Assert
        post.ViewCount.Should().Be(0);
        post.IsPinned.Should().BeFalse();
        post.IsLocked.Should().BeFalse();
        post.IsDeleted.Should().BeFalse();
        post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        post.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        post.Comments.Should().BeEmpty();
        post.Category.Should().BeNull();
    }

    [Fact]
    public void Post_WithComments_CountsCorrectly()
    {
        // Arrange
        var post = new Post
        {
            Title = "Test Post",
            Content = "Test Content",
            AuthorId = 1,
            Author = new User { Id = 1, Username = "testuser", Email = "test@example.com", PasswordHash = "hash" }
        };

        var comment1 = new Comment { Id = 1, Content = "Comment 1", PostId = post.Id, AuthorId = 1 };
        var comment2 = new Comment { Id = 2, Content = "Comment 2", PostId = post.Id, AuthorId = 2 };

        post.Comments.Add(comment1);
        post.Comments.Add(comment2);

        // Act & Assert
        post.Comments.Should().HaveCount(2);
    }

    [Fact]
    public void Post_WithCategory_SetsValue()
    {
        // Arrange & Act
        var post = new Post
        {
            Title = "Tech Post",
            Content = "Content",
            AuthorId = 1,
            Category = "Technology"
        };

        // Assert
        post.Category.Should().Be("Technology");
    }

    [Fact]
    public void Post_PinnedAndLocked_SetCorrectly()
    {
        // Arrange & Act
        var post = new Post
        {
            Title = "Important",
            Content = "Content",
            AuthorId = 1,
            IsPinned = true,
            IsLocked = true
        };

        // Assert
        post.IsPinned.Should().BeTrue();
        post.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Post_ViewCount_CanBeIncremented()
    {
        // Arrange
        var post = new Post { Title = "Popular", Content = "Content", AuthorId = 1 };

        // Act
        post.ViewCount = 100;
        post.ViewCount++;

        // Assert
        post.ViewCount.Should().Be(101);
    }

    [Fact]
    public void Post_SoftDelete_WorksCorrectly()
    {
        // Arrange
        var post = new Post { Title = "Deleted Post", Content = "Content", AuthorId = 1 };

        // Act
        post.IsDeleted = true;

        // Assert
        post.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Post_DefaultStrings_AreEmpty()
    {
        // Arrange & Act
        var post = new Post();

        // Assert
        post.Title.Should().BeEmpty();
        post.Content.Should().BeEmpty();
    }
}
