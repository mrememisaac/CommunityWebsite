using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// API integration tests for event endpoints
/// Tests event CRUD operations, date filtering, and organizer functions
/// </summary>
public class EventsApiTests : ApiTestBase
{
    [Fact]
    public async Task GET_GetUpcomingEvents_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/events/upcoming");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetPastEvents_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/events/past");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetUpcomingEvents_WithLimit_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/events/upcoming?limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_GetUpcomingEvents_WithInvalidLimit_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/events/upcoming?limit=0");

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
        var response = await Client.PostAsJsonAsync("/api/events", request);

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
        var response = await Client.PostAsJsonAsync("/api/events", request);

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
        var response = await Client.PostAsJsonAsync("/api/events", request);

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
        var createResponse = await Client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.GetAsync($"/api/events/{eventId}");

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
        var response = await Client.GetAsync("/api/events/99999");

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
        var createResponse = await Client.PostAsJsonAsync("/api/events", createRequest);
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
        var response = await Client.PutAsJsonAsync($"/api/events/{eventId}", updateRequest);

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
        var createResponse = await Client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.PostAsync($"/api/events/{eventId}/cancel", null);

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
        var createResponse = await Client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var eventId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await Client.DeleteAsync($"/api/events/{eventId}");

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
        await Client.PostAsJsonAsync("/api/events", createRequest);

        // Act
        var response = await Client.GetAsync($"/api/events/organizer/{UserId}");

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
        var clientWithoutAuth = Factory.CreateClient();

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
}
