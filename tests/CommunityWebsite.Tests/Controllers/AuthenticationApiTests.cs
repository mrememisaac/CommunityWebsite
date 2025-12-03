using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for authentication endpoints
/// Tests registration and login functionality
/// </summary>
public class AuthenticationApiTests : ApiTestBase
{
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
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }
}
