using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Posts API controller - RESTful endpoints for post management.
/// Follows REST conventions and proper HTTP semantics.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api/[controller]")]
public class PostsController : ApiControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, ILogger<PostsController> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a specific post with all comments
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDetailDto>> GetPost(int id)
    {
        _logger.LogInformation("GET /api/posts/{PostId}", id);

        var result = await _postService.GetPostDetailAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Post not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets featured/trending posts
    /// </summary>
    [HttpGet("featured")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PostSummaryDto>>> GetFeaturedPosts()
    {
        _logger.LogInformation("GET /api/posts/featured");

        var result = await _postService.GetFeaturedPostsAsync();

        if (!result.IsSuccess)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets posts by category
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PostSummaryDto>>> GetByCategory(
        string category,
        [FromQuery] int pageNumber = 1)
    {
        _logger.LogInformation("GET /api/posts/category/{Category}?pageNumber={PageNumber}", category, pageNumber);

        var result = await _postService.GetPostsByCategoryAsync(category, pageNumber);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Searches posts by query term
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PostSummaryDto>>> SearchPosts([FromQuery] string q)
    {
        _logger.LogInformation("GET /api/posts/search?q={SearchTerm}", q);

        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search term (q) is required" });

        var result = await _postService.SearchPostsAsync(q);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new post
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDetailDto>> CreatePost([FromBody] CreatePostRequest request)
    {
        _logger.LogInformation("POST /api/posts - Creating post for user {UserId}", request.AuthorId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _postService.CreatePostAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetPost), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing post
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDetailDto>> UpdatePost(
        int id,
        [FromBody] UpdatePostRequest request)
    {
        _logger.LogInformation("PUT /api/posts/{PostId}", id);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Use base class property for current user ID
        if (CurrentUserId == null)
            return Unauthorized(new { error = "Invalid user token" });

        // Verify ownership before update
        var ownershipResult = await _postService.VerifyOwnershipAsync(id, CurrentUserId.Value);
        if (!ownershipResult.IsSuccess)
            return NotFound(new { error = ownershipResult.ErrorMessage });

        if (!ownershipResult.Data)
            return Forbid();

        var result = await _postService.UpdatePostAsync(id, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a post (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(int id)
    {
        _logger.LogInformation("DELETE /api/posts/{PostId}", id);

        // Use base class property for current user ID
        if (CurrentUserId == null)
            return Unauthorized(new { error = "Invalid user token" });

        // Verify ownership before delete
        var ownershipResult = await _postService.VerifyOwnershipAsync(id, CurrentUserId.Value);
        if (!ownershipResult.IsSuccess)
            return NotFound(new { error = ownershipResult.ErrorMessage });

        if (!ownershipResult.Data)
            return Forbid();

        var result = await _postService.DeletePostAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Gets all posts by a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PostSummaryDto>>> GetUserPosts(int userId)
    {
        _logger.LogInformation("GET /api/posts/user/{UserId}", userId);

        if (userId <= 0)
            return BadRequest(new { error = "Invalid user ID" });

        var result = await _postService.GetPostsByUserAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }
}

