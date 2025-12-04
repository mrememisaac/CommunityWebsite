using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Admin users view controller - Handles admin user management pages
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminUsersViewController : Controller
{
    private readonly IAdminUserService _adminUserService;
    private readonly ILogger<AdminUsersViewController> _logger;

    public AdminUsersViewController(
        IAdminUserService adminUserService,
        ILogger<AdminUsersViewController> logger)
    {
        _adminUserService = adminUserService;
        _logger = logger;
    }

    /// <summary>
    /// Display user management page
    /// </summary>
    [HttpGet("/admin/users")]
    public async Task<IActionResult> Index(int pageNumber = 1, string? searchTerm = null)
    {
        _logger.LogInformation("Admin accessing users management page");

        var result = await _adminUserService.GetAllUsersAsync(pageNumber, 20, searchTerm);
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.ErrorMessage;
            return View("~/Views/Admin/Users/Index.cshtml", new List<object>());
        }

        var rolesResult = await _adminUserService.GetAllRolesAsync();
        ViewBag.AvailableRoles = rolesResult.IsSuccess ? (rolesResult.Data?.ToList() ?? new List<RoleDto>()) : new List<RoleDto>();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.PageNumber = pageNumber;

        return View("~/Views/Admin/Users/Index.cshtml", result.Data);
    }

    /// <summary>
    /// Display user detail page
    /// </summary>
    [HttpGet("/admin/users/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        _logger.LogInformation("Admin viewing user detail for user {UserId}", id);

        var result = await _adminUserService.GetUserDetailAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        var rolesResult = await _adminUserService.GetAllRolesAsync();
        ViewBag.AvailableRoles = rolesResult.IsSuccess ? (rolesResult.Data?.ToList() ?? new List<RoleDto>()) : new List<RoleDto>();

        return View("~/Views/Admin/Users/Detail.cshtml", result.Data);
    }
}
