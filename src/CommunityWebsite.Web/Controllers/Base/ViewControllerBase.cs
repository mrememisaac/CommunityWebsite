using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Base controller for MVC view controllers providing common functionality
/// Demonstrates proper inheritance and code reuse in controller hierarchy
/// </summary>
public abstract class ViewControllerBase : Controller
{
    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims
    /// </summary>
    protected int? CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Gets the current authenticated user's username from JWT claims
    /// </summary>
    protected string? CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Checks if the current request is authenticated
    /// </summary>
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Sets common ViewBag properties for all views
    /// </summary>
    protected void SetCommonViewData()
    {
        ViewBag.IsAuthenticated = IsAuthenticated;
        ViewBag.CurrentUsername = CurrentUsername;
        ViewBag.CurrentUserId = CurrentUserId;
    }

    /// <summary>
    /// Returns a view with a user-friendly error message
    /// </summary>
    protected IActionResult ViewWithError(string viewName, string errorMessage)
    {
        ViewBag.ErrorMessage = errorMessage;
        return View(viewName);
    }

    /// <summary>
    /// Returns a view with a success message
    /// </summary>
    protected IActionResult ViewWithSuccess(string viewName, object? model, string successMessage)
    {
        ViewBag.SuccessMessage = successMessage;
        return View(viewName, model);
    }
}
