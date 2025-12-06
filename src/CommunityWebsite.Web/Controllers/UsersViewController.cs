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
    /// Display list of all users (Members page)
    /// </summary>
    [HttpGet("/users")]
    public async Task<IActionResult> Index()
    {
        var users = await _userRepository.GetAllAsync();
        var userDtos = users.Select(u => new UserProfileDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            ProfileImageUrl = u.ProfileImageUrl,
            CreatedAt = u.CreatedAt
        }).OrderBy(u => u.Username).ToList();

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

        var posts = await _postRepository.GetUserPostsAsync(id);
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
        var recentPosts = posts
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
