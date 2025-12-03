using Xunit;
using FluentAssertions;
using System.Net;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for user endpoints
/// Tests user listing and retrieval functionality
/// </summary>
public class UsersApiTests : ApiTestBase
{
    [Fact]
    public async Task GET_GetAllUsers_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
