using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Tests for ViewControllerBase functionality
/// </summary>
public class ViewControllerBaseTests
{
    // Test implementation of abstract class
    private class TestViewController : CommunityWebsite.Web.Controllers.ViewControllerBase
    {
        public int? GetCurrentUserId() => CurrentUserId;
        public string? GetCurrentUsername() => CurrentUsername;
        public bool GetIsAuthenticated() => IsAuthenticated;
        public void CallSetCommonViewData() => SetCommonViewData();
        public IActionResult CallViewWithError(string viewName, string error) =>
            ViewWithError(viewName, error);
        public IActionResult CallViewWithSuccess(string viewName, object? model, string success) =>
            ViewWithSuccess(viewName, model, success);
    }

    private TestViewController CreateControllerWithClaims(params Claim[] claims)
    {
        var controller = new TestViewController();
        ClaimsIdentity identity;

        if (claims.Length > 0)
        {
            identity = new ClaimsIdentity(claims, "TestAuth");
        }
        else
        {
            identity = new ClaimsIdentity(); // Unauthenticated
        }

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
            new Claim(ClaimTypes.NameIdentifier, "123"));

        // Act
        var result = controller.GetCurrentUserId();

        // Assert
        result.Should().Be(123);
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
    public void CurrentUsername_WithClaim_ReturnsUsername()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Name, "johndoe"));

        // Act
        var result = controller.GetCurrentUsername();

        // Assert
        result.Should().Be("johndoe");
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.Name, "testuser"));

        // Act
        var result = controller.GetIsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        var result = controller.GetIsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetCommonViewData_SetsViewBagProperties()
    {
        // Arrange
        var controller = CreateControllerWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "testuser"));

        // Act
        controller.CallSetCommonViewData();

        // Assert
        ((bool)controller.ViewBag.IsAuthenticated).Should().BeTrue();
        ((string)controller.ViewBag.CurrentUsername).Should().Be("testuser");
        ((int?)controller.ViewBag.CurrentUserId).Should().Be(42);
    }

    [Fact]
    public void SetCommonViewData_WithUnauthenticatedUser_SetsCorrectValues()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        controller.CallSetCommonViewData();

        // Assert
        ((bool)controller.ViewBag.IsAuthenticated).Should().BeFalse();
        ((string?)controller.ViewBag.CurrentUsername).Should().BeNull();
        ((int?)controller.ViewBag.CurrentUserId).Should().BeNull();
    }

    [Fact]
    public void ViewWithError_SetsErrorMessageInViewBag()
    {
        // Arrange
        var controller = CreateControllerWithClaims();

        // Act
        var result = controller.CallViewWithError("TestView", "Something went wrong");

        // Assert
        result.Should().BeOfType<ViewResult>();
        ((string)controller.ViewBag.ErrorMessage).Should().Be("Something went wrong");
    }

    [Fact]
    public void ViewWithSuccess_SetsSuccessMessageInViewBag()
    {
        // Arrange
        var controller = CreateControllerWithClaims();
        var model = new { Id = 1, Name = "Test" };

        // Act
        var result = controller.CallViewWithSuccess("TestView", model, "Operation successful!");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(model);
        ((string)controller.ViewBag.SuccessMessage).Should().Be("Operation successful!");
    }
}
