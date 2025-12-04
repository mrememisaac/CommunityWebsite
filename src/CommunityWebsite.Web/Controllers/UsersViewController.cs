using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;

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

        var userProfile = new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            PostCount = posts.Count(),
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        // Get recent posts for the profile page (limit to 5)
        var recentPosts = posts
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new PostSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Preview = p.Content.Length > 150 ? p.Content.Substring(0, 150) + "..." : p.Content,
                CreatedAt = p.CreatedAt,
                ViewCount = p.ViewCount,
                CommentCount = p.Comments.Count
            })
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
