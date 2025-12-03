using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Tests for ApiControllerBase functionality
/// </summary>
public class ApiControllerBaseTests
{
    // Test implementation of abstract class
    private class TestApiController : CommunityWebsite.Web.Controllers.ApiControllerBase
    {
        public int? GetCurrentUserId() => CurrentUserId;
        public string? GetCurrentUsername() => CurrentUsername;
        public string? GetCurrentEmail() => CurrentEmail;
        public IEnumerable<string> GetCurrentUserRoles() => CurrentUserRoles;
        public bool GetIsInRole(string role) => IsInRole(role);
        public bool GetIsAdmin() => IsAdmin;
        public ActionResult GetErrorResponse(string message, int statusCode = 400) =>
            ErrorResponse(message, statusCode);
        public ActionResult<T> GetSuccessResponse<T>(T data) => SuccessResponse(data);
    }

    private TestApiController CreateControllerWithClaims(params Claim[] claims)
    {
        var controller = new TestApiController();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    [Fact]
    public void CurrentUserId_WithValidClaim_ReturnsUserId()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"));

        // Act
        var result = controller.GetCurrentUserId();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void CurrentUserId_WithNoClaim_ReturnsNull()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        var result = controller.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CurrentUserId_WithInvalidClaim_ReturnsNull()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        // Act
        var result = controller.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CurrentUsername_WithClaim_ReturnsUsername()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Name, "testuser"));

        // Act
        var result = controller.GetCurrentUsername();

        // Assert
        result.Should().Be("testuser");
    }

    [Fact]
    public void CurrentEmail_WithClaim_ReturnsEmail()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Email, "test@example.com"));

        // Act
        var result = controller.GetCurrentEmail();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void CurrentUserRoles_WithMultipleRoles_ReturnsAllRoles()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Moderator"),
            new Claim(ClaimTypes.Role, "User"));

        // Act
        var result = controller.GetCurrentUserRoles().ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Admin");
        result.Should().Contain("Moderator");
        result.Should().Contain("User");
    }

    [Fact]
    public void IsInRole_WithMatchingRole_ReturnsTrue()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Role, "Admin"));

        // Act
        var result = controller.GetIsInRole("Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WithoutMatchingRole_ReturnsFalse()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Role, "User"));

        // Act
        var result = controller.GetIsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Role, "Admin"));

        // Act
        var result = controller.GetIsAdmin();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WithoutAdminRole_ReturnsFalse()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Role, "User"));

        // Act
        var result = controller.GetIsAdmin();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ErrorResponse_ReturnsObjectResultWithStatusCode()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        var result = controller.GetErrorResponse("Test error", 404);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ErrorResponse_DefaultsTo400StatusCode()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        var result = controller.GetErrorResponse("Test error");

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public void SuccessResponse_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateControllerWithClaims();
        var data = new { Name = "Test", Value = 42 };

        // Act
        var result = controller.GetSuccessResponse(data);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(data);
    }
}
