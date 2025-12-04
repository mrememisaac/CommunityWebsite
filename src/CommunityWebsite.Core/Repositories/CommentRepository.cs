using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// Comment repository implementation.
/// </summary>
public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(CommunityDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Comment>> GetPostCommentsAsync(int postId)
    {
        return await _dbSet
            .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .ThenInclude(r => r.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetUserCommentsAsync(int userId)
    {
        return await _dbSet
            .Where(c => c.AuthorId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Post)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Comment?> GetCommentWithRepliesAsync(int commentId)
    {
        return await _dbSet
            .Where(c => c.Id == commentId && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Replies)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetCommentCountForPostAsync(int postId)
    {
        return await _dbSet
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .CountAsync();
    }

    public async Task<int> GetCommentCountByUserAsync(int userId)
    {
        return await _dbSet
            .Where(c => c.AuthorId == userId && !c.IsDeleted)
            .CountAsync();
    }
}
