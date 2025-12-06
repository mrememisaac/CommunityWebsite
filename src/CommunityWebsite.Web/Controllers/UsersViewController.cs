using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Constants;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Users View Controller - Handles user-facing pages for viewing user profiles
/// </summary>
public class UsersViewController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<UsersViewController> _logger;

    public UsersViewController(
        IUserRepository userRepository,
        IPostRepository postRepository,
        ILogger<UsersViewController> logger)
    {
        _userRepository = userRepository;
        _postRepository = postRepository;
        _logger = logger;
    }

    /// <summary>
    /// Display list of all users (Members page) with pagination
    /// </summary>
    [HttpGet("/users")]
    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        const int pageSize = 12;
        var result = await _userRepository.GetUsersWithStatsAsync(page, pageSize, search);

        var userDtos = result.Items
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                ProfileImageUrl = u.ProfileImageUrl,
                CreatedAt = u.CreatedAt
            })
            .OrderBy(u => u.Username)
            .ToList();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

        return View("~/Views/Users/Index.cshtml", userDtos);
    }    /// <summary>
         /// Display a user's profile page
         /// </summary>
    [HttpGet("/users/{id}")]
    public async Task<IActionResult> Profile(int id)
    {
        var user = await _userRepository.GetUserWithRolesAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var pagedResult = await _postRepository.GetUserPostsAsync(id);
        var orderedPosts = pagedResult.Items.OrderByDescending(p => p.CreatedAt);
        var postCount = await _postRepository.GetPostCountAsync(id);

        var userProfile = new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            PostCount = postCount,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        // Get recent posts for the profile page (limit to configured amount)
        var recentPosts = pagedResult.Items
            .OrderByDescending(p => p.CreatedAt)
            .Take(PaginationDefaults.RecentPostsLimit)
            .Select(p => p.ToSummaryDto())
            .ToList();

        ViewBag.UserProfile = userProfile;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.CurrentUserId = GetCurrentUserId();

        return View("~/Views/Users/Profile.cshtml");
    }

    /// <summary>
    /// Get current authenticated user's ID from claims
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
