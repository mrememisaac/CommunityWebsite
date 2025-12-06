using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Post service interface - Dependency Inversion Principle
/// Defines contracts for post operations
/// </summary>
public interface IPostService
{
    Task<Result<PostDetailDto?>> GetPostDetailAsync(int postId);
    Task<Result<PagedResult<PostSummaryDto>>> GetFeaturedPostsAsync(int pageNumber = 1, int pageSize = 20);
    Task<Result<PagedResult<PostSummaryDto>>> GetPostsByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20);
    Task<Result<PagedResult<PostSummaryDto>>> GetPostsByUserAsync(int userId, int pageNumber = 1, int pageSize = 20);
    Task<Result<PostDetailDto>> CreatePostAsync(CreatePostRequest request);
    Task<Result<PostDetailDto>> UpdatePostAsync(int postId, UpdatePostRequest request);
    Task<Result> DeletePostAsync(int postId);
    Task<Result<PagedResult<PostSummaryDto>>> SearchPostsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20);
    Task<Result<bool>> VerifyOwnershipAsync(int postId, int userId);
}
