using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Comment service implementation - Single Responsibility Principle
/// Handles comment business logic only, delegates validation and data access
/// </summary>
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentValidator _commentValidator;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        IUserRepository userRepository,
        ICommentValidator commentValidator,
        ILogger<CommentService> logger)
    {
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _commentValidator = commentValidator ?? throw new ArgumentNullException(nameof(commentValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all comments for a specific post
    /// </summary>
    public async Task<Result<IEnumerable<CommentDto>>> GetPostCommentsAsync(int postId)
    {
        try
        {
            _logger.LogInformation("Retrieving comments for post {PostId}", postId);

            // Verify post exists
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
            {
                return Result<IEnumerable<CommentDto>>.Failure("Post not found");
            }

            var comments = await _commentRepository.GetPostCommentsAsync(postId);

            var result = comments
                .Select(c => c.ToCommentDto())
                .ToList();

            return Result<IEnumerable<CommentDto>>.Success(result);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving comments for post {PostId}", postId);
            return Result<IEnumerable<CommentDto>>.Failure("Database error occurred while retrieving comments.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving comments for post {PostId}", postId);
            return Result<IEnumerable<CommentDto>>.Failure("Request timeout while retrieving comments.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving comments for post {PostId}", postId);
            return Result<IEnumerable<CommentDto>>.Failure("An error occurred while retrieving comments.");
        }
    }

    /// <summary>
    /// Gets detailed comment with replies
    /// </summary>
    public async Task<Result<CommentDetailDto>> GetCommentDetailAsync(int commentId)
    {
        try
        {
            _logger.LogInformation("Retrieving comment detail for {CommentId}", commentId);

            var comment = await _commentRepository.GetCommentWithRepliesAsync(commentId);

            if (comment == null)
            {
                return Result<CommentDetailDto>.Failure("Comment not found");
            }

            var result = new CommentDetailDto
            {
                Id = comment.Id,
                Content = comment.Content,
                Author = comment.Author?.ToSummaryDto(),
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                PostId = comment.PostId,
                ParentCommentId = comment.ParentCommentId,
                Replies = comment.Replies?
                    .Where(r => !r.IsDeleted)
                    .Select(r => new CommentDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        Author = r.Author?.ToSummaryDto(),
                        CreatedAt = r.CreatedAt,
                        ReplyCount = 0
                    })
                    .ToList() ?? new List<CommentDto>()
            };

            return Result<CommentDetailDto>.Success(result);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving comment {CommentId}", commentId);
            return Result<CommentDetailDto>.Failure("Database error occurred while retrieving the comment.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving comment {CommentId}", commentId);
            return Result<CommentDetailDto>.Failure("Request timeout while retrieving the comment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving comment {CommentId}", commentId);
            return Result<CommentDetailDto>.Failure("An error occurred while retrieving the comment.");
        }
    }

    /// <summary>
    /// Creates a new comment on a post
    /// </summary>
    public async Task<Result<CommentDto>> CreateCommentAsync(int postId, CreateCommentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating comment for post {PostId} by user {UserId}", postId, request.AuthorId);

            // Verify post exists and is not deleted/locked
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
            {
                return Result<CommentDto>.Failure("Post not found");
            }

            if (post.IsLocked)
            {
                return Result<CommentDto>.Failure("Cannot comment on a locked post");
            }

            // Verify user exists and is active
            var user = await _userRepository.GetByIdAsync(request.AuthorId);
            if (user == null || !user.IsActive)
            {
                return Result<CommentDto>.Failure("User not found or inactive");
            }

            // If replying to a comment, verify parent exists
            if (request.ParentCommentId.HasValue)
            {
                var parentComment = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value);
                if (parentComment == null || parentComment.IsDeleted)
                {
                    return Result<CommentDto>.Failure("Parent comment not found");
                }

                // Verify parent comment belongs to the same post
                if (parentComment.PostId != postId)
                {
                    return Result<CommentDto>.Failure("Parent comment does not belong to this post");
                }
            }

            var comment = new Comment
            {
                Content = request.Content,
                PostId = postId,
                AuthorId = request.AuthorId,
                ParentCommentId = request.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Validate comment
            var validationResult = _commentValidator.ValidateComment(comment);
            if (!validationResult.IsSuccess)
            {
                return Result<CommentDto>.Failure(validationResult.ErrorMessage!);
            }

            var createdComment = await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            _logger.LogInformation("Comment {CommentId} created successfully for post {PostId}", createdComment.Id, postId);

            return Result<CommentDto>.Success(new CommentDto
            {
                Id = createdComment.Id,
                Content = createdComment.Content,
                Author = new UserSummaryDto { Id = user.Id, Username = user.Username },
                CreatedAt = createdComment.CreatedAt,
                ReplyCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for post {PostId}", postId);
            return Result<CommentDto>.Failure("An error occurred while creating the comment.");
        }
    }

    /// <summary>
    /// Updates an existing comment
    /// </summary>
    public async Task<Result<CommentDto>> UpdateCommentAsync(int commentId, UpdateCommentRequest request)
    {
        try
        {
            _logger.LogInformation("Updating comment {CommentId}", commentId);

            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted)
            {
                return Result<CommentDto>.Failure("Comment not found");
            }

            // Update content
            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            // Validate updated comment
            var validationResult = _commentValidator.ValidateComment(comment);
            if (!validationResult.IsSuccess)
            {
                return Result<CommentDto>.Failure(validationResult.ErrorMessage!);
            }

            await _commentRepository.UpdateAsync(comment);
            await _commentRepository.SaveChangesAsync();

            // Reload with author
            var updatedComment = await _commentRepository.GetCommentWithRepliesAsync(commentId);

            _logger.LogInformation("Comment {CommentId} updated successfully", commentId);

            return Result<CommentDto>.Success(new CommentDto
            {
                Id = updatedComment!.Id,
                Content = updatedComment.Content,
                Author = updatedComment.Author != null
                    ? new UserSummaryDto { Id = updatedComment.Author.Id, Username = updatedComment.Author.Username }
                    : null,
                CreatedAt = updatedComment.CreatedAt,
                ReplyCount = updatedComment.Replies?.Count(r => !r.IsDeleted) ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
            return Result<CommentDto>.Failure("An error occurred while updating the comment.");
        }
    }

    /// <summary>
    /// Soft deletes a comment
    /// </summary>
    public async Task<Result> DeleteCommentAsync(int commentId)
    {
        try
        {
            _logger.LogInformation("Deleting comment {CommentId}", commentId);

            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted)
            {
                return Result.Failure("Comment not found");
            }

            // Soft delete
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);
            await _commentRepository.SaveChangesAsync();

            _logger.LogInformation("Comment {CommentId} deleted successfully", commentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return Result.Failure("An error occurred while deleting the comment.");
        }
    }

    /// <summary>
    /// Verifies if the specified user owns the comment
    /// </summary>
    public async Task<Result<bool>> VerifyOwnershipAsync(int commentId, int userId)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);

            if (comment == null || comment.IsDeleted)
                return Result<bool>.Failure("Comment not found");

            return Result<bool>.Success(comment.AuthorId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying ownership for comment {CommentId} and user {UserId}", commentId, userId);
            return Result<bool>.Failure("An error occurred while verifying ownership.");
        }
    }
}
