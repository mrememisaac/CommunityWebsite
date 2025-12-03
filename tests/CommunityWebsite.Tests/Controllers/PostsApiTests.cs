using Xunit;
using FluentAssertions;
using System.Net;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for post endpoints
/// Tests featured posts and category-based retrieval
/// </summary>
public class PostsApiTests : ApiTestBase
{
    [Fact]
    public async Task GET_GetPosts_Featured_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/posts/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetPosts_ByCategory_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/posts/category/General");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
