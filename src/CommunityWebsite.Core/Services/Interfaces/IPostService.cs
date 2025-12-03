using CommunityWebsite.Core.Common;
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
    Task<Result<IEnumerable<PostSummaryDto>>> GetFeaturedPostsAsync();
    Task<Result<IEnumerable<PostSummaryDto>>> GetPostsByCategoryAsync(string category, int pageNumber = 1);
    Task<Result<PostDetailDto>> CreatePostAsync(CreatePostRequest request);
    Task<Result<PostDetailDto>> UpdatePostAsync(int postId, UpdatePostRequest request);
    Task<Result> DeletePostAsync(int postId);
    Task<Result<IEnumerable<PostSummaryDto>>> SearchPostsAsync(string searchTerm);
    Task<Result<bool>> VerifyOwnershipAsync(int postId, int userId);
}
