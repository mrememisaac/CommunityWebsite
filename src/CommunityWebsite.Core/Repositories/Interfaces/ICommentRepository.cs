using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Repositories.Interfaces;

/// <summary>
/// Comment repository interface.
/// </summary>
public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetPostCommentsAsync(int postId);
    Task<IEnumerable<Comment>> GetUserCommentsAsync(int userId);
    Task<Comment?> GetCommentWithRepliesAsync(int commentId);
    Task<int> GetCommentCountForPostAsync(int postId);
}
