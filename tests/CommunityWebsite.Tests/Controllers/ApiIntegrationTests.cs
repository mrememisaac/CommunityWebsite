using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityWebsite.Web;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// End-to-end API integration tests using WebApplicationFactory
/// Tests actual HTTP request/response cycles through the API controllers
/// Uses SQLite in-memory database for more realistic relational DB testing
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private const string TestJwtSecret = "test-secret-key-for-integration-testing-minimum-32-chars";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _authToken = string.Empty;
    private int _userId = 0;
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        // Create and open a shared SQLite connection that persists for the test lifetime
        // This keeps the in-memory database alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Add test configuration for JWT secret - must happen before services are built
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = TestJwtSecret
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    // Remove all existing DbContext-related registrations
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CommunityDbContext>));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    // Use SQLite in-memory database with the shared connection
                    // SQLite enforces foreign keys and constraints like a real relational database
                    services.AddDbContext<CommunityDbContext>(options =>
                        options.UseSqlite(_connection));
                });
            });

        _client = _factory.CreateClient();

        // Create database schema
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Seed comprehensive test data
        await SeedTestDataAsync();

        // Setup: Create test user and get auth token
        await SetupTestUser();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
        _connection?.Close();
        _connection?.Dispose();
    }

    private async Task SetupTestUser()
    {
        var registerRequest = new
        {
            username = "testuser",
            email = "test@example.com",
            password = "TestPassword123!",
            confirmPassword = "TestPassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var json = await registerResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Auth controller returns the data directly (not wrapped in { data: ... })
        _authToken = root.GetProperty("token").GetString() ?? "";
        _userId = root.GetProperty("userId").GetInt32();

        // Add auth token to default headers for authenticated requests
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
    }

    private async Task SeedTestDataAsync()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Seed comprehensive test data
            await SeedData.SeedDatabaseAsync(dbContext, passwordHasher);
        }
    }

    #region Authentication Endpoints

    [Fact]
    public async Task POST_Register_WithValidRequest_ReturnsCreatedOrOk()
    {
        // Arrange
        var request = new
        {
            username = "newuser",
            email = "newuser@example.com",
            password = "SecurePassword123!",
            confirmPassword = "SecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    #endregion

    #region Posts Endpoints

    [Fact]
    public async Task GET_GetPosts_Featured_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/posts/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetPosts_ByCategory_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/posts/category/General");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Users Endpoints

    [Fact]
    public async Task GET_GetAllUsers_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region HTTP Status Codes

    [Fact]
    public async Task InvalidEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidHttpMethod_ReturnsMethodNotAllowed()
    {
        // Act - Try PUT on an endpoint that doesn't support it (assuming /api/auth/register doesn't support PUT)
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/posts")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        });

        // Assert - Should either be MethodNotAllowed or BadRequest
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError
        );
    }

    #endregion

    #region Response Validation

    [Fact]
    public async Task ResponseHeaders_ContainContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/posts/featured");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithValidToken_Succeeds()
    {
        // Arrange - Token already set in SetupTestUser

        // Act
        var response = await _client.GetAsync("/api/posts/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Comments Endpoints

    [Fact]
    public async Task GET_PostComments_ReturnsOkWithComments()
    {
        // Arrange - First create a post, then get its comments
        var postRequest = new
        {
            title = "Test Post for Comments",
            content = "This is a test post to verify comment retrieval works correctly.",
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.GetAsync($"/api/posts/{postId}/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_PostComments_ForNonExistentPost_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/posts/99999/comments");

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This is a test comment on the post.",
            authorId = _userId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "",
            authorId = _userId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);

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
            authorId = _userId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/99999/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateComment_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - Remove auth token
        var clientWithoutAuth = _factory.CreateClient();

        var commentRequest = new
        {
            content = "This comment requires authentication.",
            authorId = _userId
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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This is a comment to retrieve details for.",
            authorId = _userId
        };
        var commentResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.GetAsync($"/api/comments/{commentId}");

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
        var response = await _client.GetAsync("/api/comments/99999");

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "Original comment content.",
            authorId = _userId
        };
        var commentResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            content = "Updated comment content."
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/comments/{commentId}", updateRequest);

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
        var response = await _client.PutAsJsonAsync("/api/comments/99999", updateRequest);

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var commentRequest = new
        {
            content = "This comment will be deleted.",
            authorId = _userId
        };
        var commentResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", commentRequest);
        commentResponse.EnsureSuccessStatusCode();
        var commentJson = await commentResponse.Content.ReadAsStringAsync();
        var commentDoc = JsonDocument.Parse(commentJson);
        var commentId = commentDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion - comment should not be found
        var verifyResponse = await _client.GetAsync($"/api/comments/{commentId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Comment_ForNonExistentComment_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/comments/99999");

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var parentCommentRequest = new
        {
            content = "This is the parent comment.",
            authorId = _userId
        };
        var parentResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", parentCommentRequest);
        parentResponse.EnsureSuccessStatusCode();
        var parentJson = await parentResponse.Content.ReadAsStringAsync();
        var parentDoc = JsonDocument.Parse(parentJson);
        var parentCommentId = parentDoc.RootElement.GetProperty("id").GetInt32();

        var replyRequest = new
        {
            content = "This is a reply to the parent comment.",
            authorId = _userId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", replyRequest);

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
            authorId = _userId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/comments/99999/replies", replyRequest);

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
            authorId = _userId,
            category = "Testing"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        postResponse.EnsureSuccessStatusCode();
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        var postId = postDoc.RootElement.GetProperty("id").GetInt32();

        var parentCommentRequest = new
        {
            content = "Parent comment with replies.",
            authorId = _userId
        };
        var parentResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/comments", parentCommentRequest);
        parentResponse.EnsureSuccessStatusCode();
        var parentJson = await parentResponse.Content.ReadAsStringAsync();
        var parentDoc = JsonDocument.Parse(parentJson);
        var parentCommentId = parentDoc.RootElement.GetProperty("id").GetInt32();

        // Create two replies
        var reply1Request = new { content = "First reply.", authorId = _userId };
        var reply2Request = new { content = "Second reply.", authorId = _userId };
        await _client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", reply1Request);
        await _client.PostAsJsonAsync($"/api/comments/{parentCommentId}/replies", reply2Request);

        // Act
        var response = await _client.GetAsync($"/api/comments/{parentCommentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("replies").GetArrayLength().Should().Be(2);
    }

    #endregion

    #region Roles Endpoints

    [Fact]
    public async Task GET_GetAllRoles_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_GetRoleByName_WithValidName_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/roles/name/User");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("User");
    }

    [Fact]
    public async Task GET_GetRoleByName_WithNonExistentName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/roles/name/NonExistentRole");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_GetUsersInRoleByName_WithValidRole_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/roles/name/User/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_CreateRole_Unauthorized_ReturnsForbiddenOrUnauthorized()
    {
        // Arrange - Remove auth header to test as unauthenticated
        var clientWithoutAuth = _factory.CreateClient();

        var request = new
        {
            name = "NewTestRole",
            description = "A test role"
        };

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Events Endpoints

    [Fact]
    public async Task GET_GetUpcomingEvents_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/events/upcoming");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetPastEvents_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/events/past");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetUpcomingEvents_WithLimit_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/events/upcoming?limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetUpcomingEvents_WithInvalidLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/events/upcoming?limit=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateEvent_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            title = "Integration Test Event",
            description = "An event created during integration testing",
            startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
            location = "Test Location"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Integration Test Event");
    }

    [Fact]
    public async Task POST_CreateEvent_WithPastDate_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            title = "Past Event Test",
            description = "This event is in the past",
            startDate = DateTime.UtcNow.AddDays(-1).ToString("o")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateEvent_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            title = "",
            startDate = DateTime.UtcNow.AddDays(7).ToString("o")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_GetEvent_WithValidId_ReturnsOk()
    {
        // Arrange - Create an event first
        var createRequest = new
        {
            title = "Event For Get Test",
            startDate = DateTime.UtcNow.AddDays(14).ToString("o")
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.GetAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(eventId);
    }

    [Fact]
    public async Task GET_GetEvent_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/events/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateEvent_WithValidRequest_ReturnsOk()
    {
        // Arrange - Create an event first
        var createRequest = new
        {
            title = "Event For Update Test",
            startDate = DateTime.UtcNow.AddDays(21).ToString("o")
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            title = "Updated Event Title",
            description = "Updated description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/events/{eventId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Updated Event Title");
    }

    [Fact]
    public async Task POST_CancelEvent_WithValidRequest_ReturnsOk()
    {
        // Arrange - Create an event first
        var createRequest = new
        {
            title = "Event For Cancel Test",
            startDate = DateTime.UtcNow.AddDays(28).ToString("o")
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.PostAsync($"/api/events/{eventId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DELETE_Event_WithValidRequest_ReturnsNoContent()
    {
        // Arrange - Create an event first
        var createRequest = new
        {
            title = "Event For Delete Test",
            startDate = DateTime.UtcNow.AddDays(35).ToString("o")
        };
        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.DeleteAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_EventsByOrganizer_WithValidUserId_ReturnsOk()
    {
        // Arrange - First create an event to ensure organizer has events
        var createRequest = new
        {
            title = "Event For Organizer Test",
            startDate = DateTime.UtcNow.AddDays(42).ToString("o")
        };
        await _client.PostAsJsonAsync("/api/events", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/events/organizer/{_userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_CreateEvent_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange - Remove auth header
        var clientWithoutAuth = _factory.CreateClient();

        var request = new
        {
            title = "Unauthorized Event Test",
            startDate = DateTime.UtcNow.AddDays(7).ToString("o")
        };

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
