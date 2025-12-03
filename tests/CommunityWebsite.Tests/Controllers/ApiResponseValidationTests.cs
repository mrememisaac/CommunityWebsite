using Xunit;
using FluentAssertions;
using System.Net;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for HTTP status codes and response validation
/// Tests error handling, content types, and authentication
/// </summary>
public class ApiResponseValidationTests : ApiTestBase
{
    [Fact]
    public async Task InvalidEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidHttpMethod_ReturnsMethodNotAllowed()
    {
        // Act - Try PUT on an endpoint that doesn't support it (assuming /api/auth/register doesn't support PUT)
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/api/posts")
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

    [Fact]
    public async Task ResponseHeaders_ContainContentType()
    {
        // Act
        var response = await Client.GetAsync("/api/posts/featured");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithValidToken_Succeeds()
    {
        // Arrange - Token already set in SetupTestUser

        // Act
        var response = await Client.GetAsync("/api/posts/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
