using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CommunityWebsite.Web.Controllers;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Unit tests for AccountController
/// Tests MVC view controller functionality for authentication views
/// </summary>
public class AccountControllerTests
{
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockLogger = new Mock<ILogger<AccountController>>();
        _controller = new AccountController(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountController(null!));
    }

    #endregion

    #region Login Action Tests

    [Fact]
    public void Login_ReturnsViewResult()
    {
        // Act
        var result = _controller.Login();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Login_WithReturnUrl_SetsViewBagReturnUrl()
    {
        // Arrange
        var returnUrl = "/Posts/Create";

        // Act
        var result = _controller.Login(returnUrl);

        // Assert
        result.Should().BeOfType<ViewResult>();
        ((string)_controller.ViewBag.ReturnUrl).Should().Be(returnUrl);
    }

    [Fact]
    public void Login_WithNullReturnUrl_SetsNullViewBagReturnUrl()
    {
        // Act
        var result = _controller.Login(null);

        // Assert
        result.Should().BeOfType<ViewResult>();
        ((object?)_controller.ViewBag.ReturnUrl).Should().BeNull();
    }

    #endregion

    #region Register Action Tests

    [Fact]
    public void Register_ReturnsViewResult()
    {
        // Act
        var result = _controller.Register();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    #endregion

    #region Profile Action Tests

    [Fact]
    public void Profile_ReturnsViewResult()
    {
        // Act
        var result = _controller.Profile();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    #endregion

    #region Logout Action Tests

    [Fact]
    public void Logout_RedirectsToHomeIndex()
    {
        // Act
        var result = _controller.Logout();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Home");
    }

    #endregion
}
