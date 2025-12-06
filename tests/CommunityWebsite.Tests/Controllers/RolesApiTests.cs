using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for role endpoints
/// Tests role retrieval, assignment, and authorization checks
/// </summary>
public class RolesApiTests : ApiTestBase
{
    [Fact]
    public async Task GET_GetAllRoles_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_GetRoleByName_WithValidName_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/roles/name/User");

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
        var response = await Client.GetAsync("/api/roles/name/NonExistentRole");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_GetUsersInRoleByName_WithValidRole_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/roles/name/User/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_CreateRole_Unauthorized_ReturnsForbiddenOrUnauthorized()
    {
        // Arrange - Remove auth header to test as unauthenticated
        var clientWithoutAuth = Factory.CreateClient();

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
}
