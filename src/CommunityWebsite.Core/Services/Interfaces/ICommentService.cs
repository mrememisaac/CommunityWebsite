using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Comment service interface - Dependency Inversion Principle
/// Defines contracts for comment operations
/// </summary>
public interface ICommentService
{
    Task<Result<IEnumerable<CommentDto>>> GetPostCommentsAsync(int postId);
    Task<Result<CommentDetailDto>> GetCommentDetailAsync(int commentId);
    Task<Result<CommentDto>> CreateCommentAsync(int postId, CreateCommentRequest request);
    Task<Result<CommentDto>> UpdateCommentAsync(int commentId, UpdateCommentRequest request);
    Task<Result> DeleteCommentAsync(int commentId);
    Task<Result<bool>> VerifyOwnershipAsync(int commentId, int userId);
}
