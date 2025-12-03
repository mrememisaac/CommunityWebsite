using Microsoft.AspNetCore.Mvc;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// MVC Controller for Account views - serves authentication Razor pages.
/// Inherits from <see cref="ViewControllerBase"/> for common MVC functionality.
/// </summary>
public class AccountController : ViewControllerBase
{
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays login page
    /// </summary>
    [HttpGet]
    [Route("Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        _logger.LogInformation("GET Account/Login - ReturnUrl: {ReturnUrl}", returnUrl);
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Displays registration page
    /// </summary>
    [HttpGet]
    [Route("Account/Register")]
    public IActionResult Register()
    {
        _logger.LogInformation("GET Account/Register");
        return View();
    }

    /// <summary>
    /// Displays user profile page
    /// </summary>
    [HttpGet]
    [Route("Account/Profile")]
    public IActionResult Profile()
    {
        _logger.LogInformation("GET Account/Profile");
        return View();
    }

    /// <summary>
    /// Handles logout - clears client-side auth and redirects to home
    /// </summary>
    [HttpGet]
    [Route("Account/Logout")]
    public IActionResult Logout()
    {
        _logger.LogInformation("GET Account/Logout");
        // Client-side logout is handled by JavaScript
        // This endpoint just redirects to home
        return RedirectToAction("Index", "Home");
    }
}
