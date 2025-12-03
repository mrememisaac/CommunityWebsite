using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for comment endpoints
/// Tests comment CRUD operations, threading, and replies
/// </summary>
public class CommentsApiTests : ApiTestBase
{
    [Fact]
    public async Task GET_PostComments_ReturnsOkWithComments()
    {
        // Arrange - First create a post, then get its comments
        var postRequest = new
        {
            title = "Test Post for Comments",
            content = "This is a test post to verify comment retrieval works correctly.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.GetAsync($"/api/posts/{postId}/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_PostComments_ForNonExistentPost_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/posts/99999/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateComment_WithValidRequest_ReturnsCreated()
    {
        // Arrange - First create a post
        var postRequest = new
        {
            title = "Test Post for New Comment",
            content = "This is a test post where we will add a comment.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This is a test comment on the post.",
            authorId = UserId
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("content").GetString().Should().Be("This is a test comment on the post.");
    }

    [Fact]
    public async Task POST_CreateComment_WithEmptyContent_ReturnsBadRequest()
    {
        // Arrange - First create a post
        var postRequest = new
        {
            title = "Test Post for Empty Comment",
            content = "This is a test post where we will try to add an empty comment.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "",
            authorId = UserId
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateComment_OnNonExistentPost_ReturnsNotFound()
    {
        // Arrange
        var commentRequest = new
        {
            content = "This comment should not be created.",
            authorId = UserId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/posts/99999/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateComment_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - Remove auth token
        var clientWithoutAuth = Factory.CreateClient();

        var commentRequest = new
        {
            content = "This comment requires authentication.",
            authorId = UserId
        };

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/posts/1/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CommentDetail_ReturnsOkWithCommentData()
    {
        // Arrange - Create a post and a comment
        var postRequest = new
        {
            title = "Test Post for Comment Detail",
            content = "This is a test post to verify comment detail retrieval.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This is a comment to retrieve details for.",
            authorId = UserId
        };
        var commentResponse = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.GetAsync($"/api/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(commentId);
        doc.RootElement.GetProperty("content").GetString().Should().Be("This is a comment to retrieve details for.");
        doc.RootElement.GetProperty("postId").GetInt32().Should().Be(postId);
    }

    [Fact]
    public async Task GET_CommentDetail_ForNonExistentComment_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/comments/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateComment_ByOwner_ReturnsOk()
    {
        // Arrange - Create a post and a comment
        var postRequest = new
        {
            title = "Test Post for Comment Update",
            content = "This is a test post to verify comment updates.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "Original comment content.",
            authorId = UserId
        };
        var commentResponse = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            content = "Updated comment content."
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/comments/{commentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("content").GetString().Should().Be("Updated comment content.");
    }

    [Fact]
    public async Task PUT_UpdateComment_ForNonExistentComment_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new
        {
            content = "This update should fail."
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/comments/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Comment_ByOwner_ReturnsNoContent()
    {
        // Arrange - Create a post and a comment
        var postRequest = new
        {
            title = "Test Post for Comment Deletion",
            content = "This is a test post to verify comment deletion.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This comment will be deleted.",
            authorId = UserId
        };
        var commentResponse = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.DeleteAsync($"/api/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion - comment should not be found
        var verifyResponse = await Client.GetAsync($"/api/comments/{commentId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Comment_ForNonExistentComment_ReturnsNotFound()
    {
        // Act
        var response = await Client.DeleteAsync("/api/comments/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateReply_ToExistingComment_ReturnsCreated()
    {
        // Arrange - Create a post and a parent comment
        var postRequest = new
        {
            title = "Test Post for Reply",
            content = "This is a test post to verify reply creation.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var parentCommentRequest = new
        {
            content = "This is the parent comment.",
            authorId = UserId
        };
        var parentResponse = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", parentCommentRequest);
        parentResponse.EnsureSuccessStatusCode();
        var parentJson = await parentResponse.Content.ReadAsStringAsync();
        var parentDoc = JsonDocument.Parse(parentJson);
        var parentCommentId = parentDoc.RootElement.GetProperty("id").GetInt32();

        var replyRequest = new
        {
            content = "This is a reply to the parent comment.",
            authorId = UserId
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", replyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("content").GetString().Should().Be("This is a reply to the parent comment.");
    }

    [Fact]
    public async Task POST_CreateReply_ToNonExistentComment_ReturnsNotFound()
    {
        // Arrange
        var replyRequest = new
        {
            content = "This reply should not be created.",
            authorId = UserId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/comments/99999/replies", replyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CommentDetail_WithReplies_ReturnsRepliesInResponse()
    {
        // Arrange - Create a post, parent comment, and replies
        var postRequest = new
        {
            title = "Test Post for Replies Retrieval",
            content = "This is a test post to verify replies are included in comment detail.",
            authorId = UserId,
            category = "Testing"
        };
        var postResponse = await Client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var parentCommentRequest = new
        {
            content = "Parent comment with replies.",
            authorId = UserId
        };
        var parentResponse = await Client.PostAsJsonAsync($"/api/posts/{postId}/comments", parentCommentRequest);
        parentResponse.EnsureSuccessStatusCode();
        var parentJson = await parentResponse.Content.ReadAsStringAsync();
        var parentDoc = JsonDocument.Parse(parentJson);
        var parentCommentId = parentDoc.RootElement.GetProperty("id").GetInt32();

        // Create two replies
        var reply1Request = new { content = "First reply.", authorId = UserId };
        var reply2Request = new { content = "Second reply.", authorId = UserId };
        await Client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", reply1Request);
        await Client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", reply2Request);

        // Act
        var response = await Client.GetAsync($"/api/comments/{parentCommentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("replies").GetArrayLength().Should().Be(2);
    }
}
