using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Comments API controller - RESTful endpoints for comment management.
/// Follows REST conventions and proper HTTP semantics.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api")]
public class CommentsController : ApiControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all comments for a specific post
    /// </summary>
    [HttpGet("posts/{postId}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetPostComments(int postId)
    {
        _logger.LogInformation("GET /api/posts/{PostId}/comments", postId);

        var result = await _commentService.GetPostCommentsAsync(postId);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a specific comment with its replies
    /// </summary>
    [HttpGet("comments/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDetailDto>> GetComment(int id)
    {
        _logger.LogInformation("GET /api/comments/{CommentId}", id);

        var result = await _commentService.GetCommentDetailAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new comment on a post
    /// </summary>
    [HttpPost("posts/{postId}/comments")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> CreateComment(
        int postId,
        [FromBody] CreateCommentRequest request)
    {
        _logger.LogInformation("POST /api/posts/{PostId}/comments - Creating comment by user {UserId}", postId, request.AuthorId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _commentService.CreateCommentAsync(postId, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetComment), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Creates a reply to an existing comment
    /// </summary>
    [HttpPost("comments/{commentId}/replies")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> CreateReply(
        int commentId,
        [FromBody] CreateCommentRequest request)
    {
        _logger.LogInformation("POST /api/comments/{CommentId}/replies - Creating reply by user {UserId}", commentId, request.AuthorId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get the parent comment to find the post ID
        var parentResult = await _commentService.GetCommentDetailAsync(commentId);
        if (!parentResult.IsSuccess)
            return NotFound(new { error = "Parent comment not found" });

        // Set the parent comment ID and get post ID from parent
        request.ParentCommentId = commentId;
        var postId = parentResult.Data!.PostId;

        var result = await _commentService.CreateCommentAsync(postId, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetComment), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing comment
    /// </summary>
    [HttpPut("comments/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        int id,
        [FromBody] UpdateCommentRequest request)
    {
        _logger.LogInformation("PUT /api/comments/{CommentId}", id);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Use base class property for current user ID
        if (CurrentUserId == null)
            return Unauthorized(new { error = "Invalid user token" });

        // Verify ownership before update
        var ownershipResult = await _commentService.VerifyOwnershipAsync(id, CurrentUserId.Value);
        if (!ownershipResult.IsSuccess)
            return NotFound(new { error = ownershipResult.ErrorMessage });

        if (!ownershipResult.Data)
            return Forbid();

        var result = await _commentService.UpdateCommentAsync(id, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a comment (soft delete)
    /// </summary>
    [HttpDelete("comments/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        _logger.LogInformation("DELETE /api/comments/{CommentId}", id);

        // Use base class property for current user ID
        if (CurrentUserId == null)
            return Unauthorized(new { error = "Invalid user token" });

        // Verify ownership before delete
        var ownershipResult = await _commentService.VerifyOwnershipAsync(id, CurrentUserId.Value);
        if (!ownershipResult.IsSuccess)
            return NotFound(new { error = ownershipResult.ErrorMessage });

        if (!ownershipResult.Data)
            return Forbid();

        var result = await _commentService.DeleteCommentAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return NoContent();
    }
}
