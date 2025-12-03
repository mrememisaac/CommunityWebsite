using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Users API controller - Demonstrates LINQ queries on user data.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api/[controller]")]
public class UsersController : ApiControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IPostRepository postRepository,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _postRepository = postRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets a user's profile with their roles
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        var user = await _userRepository.GetUserWithRolesAsync(id);
        if (user == null)
            return NotFound();

        var posts = await _postRepository.GetUserPostsAsync(id);

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            PostCount = posts.Count(),
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        });
    }

    /// <summary>
    /// Gets users with a specific role - demonstrates LINQ filtering
    /// </summary>
    [HttpGet("role/{roleName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsersByRole(string roleName)
    {
        var users = await _userRepository.GetUsersByRoleAsync(roleName);

        return Ok(users.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Username = u.Username
        }));
    }

    /// <summary>
    /// Gets active users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetActiveUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var users = await _userRepository.GetActiveUsersAsync(pageNumber, pageSize);

        return Ok(users.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Username = u.Username
        }));
    }

    /// <summary>
    /// Gets user by email
    /// </summary>
    [HttpGet("email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> GetUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound();

        return Ok(new UserSummaryDto
        {
            Id = user.Id,
            Username = user.Username
        });
    }
}
