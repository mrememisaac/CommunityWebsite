using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Models;

/// <summary>
/// Unit tests for Comment domain model
/// </summary>
public class CommentModelTests
{
    [Fact]
    public void Comment_Initialization_SetsDefaults()
    {
        // Arrange & Act
        var comment = new Comment
        {
            Content = "Test Comment",
            PostId = 1,
            AuthorId = 1
        };

        // Assert
        comment.IsDeleted.Should().BeFalse();
        comment.ParentCommentId.Should().BeNull();
        comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        comment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        comment.Replies.Should().BeEmpty();
    }

    [Fact]
    public void Comment_WithReplies_NestsCorrectly()
    {
        // Arrange
        var parentComment = new Comment
        {
            Id = 1,
            Content = "Parent Comment",
            PostId = 1,
            AuthorId = 1
        };

        var reply = new Comment
        {
            Id = 2,
            Content = "Reply Comment",
            PostId = 1,
            AuthorId = 2,
            ParentCommentId = parentComment.Id,
            ParentComment = parentComment
        };

        parentComment.Replies.Add(reply);

        // Act & Assert
        parentComment.Replies.Should().HaveCount(1);
        reply.ParentCommentId.Should().Be(parentComment.Id);
        reply.ParentComment.Should().Be(parentComment);
    }

    [Fact]
    public void Comment_MultipleReplies_WorkCorrectly()
    {
        // Arrange
        var parent = new Comment { Id = 1, Content = "Parent", PostId = 1, AuthorId = 1 };
        var reply1 = new Comment { Id = 2, Content = "Reply 1", PostId = 1, AuthorId = 2, ParentCommentId = 1 };
        var reply2 = new Comment { Id = 3, Content = "Reply 2", PostId = 1, AuthorId = 3, ParentCommentId = 1 };

        parent.Replies.Add(reply1);
        parent.Replies.Add(reply2);

        // Assert
        parent.Replies.Should().HaveCount(2);
    }

    [Fact]
    public void Comment_SoftDelete_WorksCorrectly()
    {
        // Arrange
        var comment = new Comment { Content = "To be deleted", PostId = 1, AuthorId = 1 };

        // Act
        comment.IsDeleted = true;

        // Assert
        comment.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Comment_DefaultStrings_AreEmpty()
    {
        // Arrange & Act
        var comment = new Comment();

        // Assert
        comment.Content.Should().BeEmpty();
    }
}
