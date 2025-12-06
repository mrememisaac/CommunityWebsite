using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Post repository interface showcasing LINQ and Entity Framework patterns.
/// </summary>
public interface IPostRepository : IRepository<Post>
{
    Task<PagedResult<Post>> GetActivePostsAsync(int pageNumber = 1, int pageSize = 10);
    Task<PagedResult<Post>> GetPostsByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20);
    Task<PagedResult<Post>> GetUserPostsAsync(int userId, int pageNumber = 1, int pageSize = 20, bool includeSoftDeleted = false);
    Task<Post?> GetPostWithCommentsAsync(int postId);
    Task<IEnumerable<Post>> GetTrendingPostsAsync(int days = 7, int limit = 10);
    Task<PagedResult<Post>> SearchPostsAsync(string searchTerm);
    Task<int> GetPostCountAsync(int userId);
    Task IncrementViewCountAsync(int postId);
}
