using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Post repository interface showcasing LINQ and Entity Framework patterns.
/// </summary>
public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetActivePostsAsync(int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Post>> GetPostsByCategoryAsync(string category, int pageSize = 20);
    Task<IEnumerable<Post>> GetUserPostsAsync(int userId, bool includeSoftDeleted = false);
    Task<Post?> GetPostWithCommentsAsync(int postId);
    Task<IEnumerable<Post>> GetTrendingPostsAsync(int days = 7, int limit = 10);
    Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm);
    Task<int> GetPostCountAsync(int userId);
    Task IncrementViewCountAsync(int postId);
}
