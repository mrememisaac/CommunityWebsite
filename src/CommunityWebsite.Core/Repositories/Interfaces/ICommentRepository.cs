using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Comment repository interface.
/// </summary>
public interface ICommentRepository : IRepository<Comment>
{
    Task<PagedResult<Comment>> GetPostCommentsAsync(int postId, int pageNumber = 1, int pageSize = 20);
    Task<PagedResult<Comment>> GetUserCommentsAsync(int userId, int pageNumber = 1, int pageSize = 20);
    Task<Comment?> GetCommentWithRepliesAsync(int commentId);
    Task<int> GetCommentCountForPostAsync(int postId);
    Task<int> GetCommentCountByUserAsync(int userId);
}
