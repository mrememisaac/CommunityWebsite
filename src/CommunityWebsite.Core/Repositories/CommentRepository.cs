using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.DTOs;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// Comment repository implementation.
/// </summary>
public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(CommunityDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Comment>> GetPostCommentsAsync(int postId, int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = 20;
        var skip = (pageNumber - 1) * pageSize;
        var query = _dbSet
            .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .ThenInclude(r => r.Author)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Comment>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Comment>> GetUserCommentsAsync(int userId, int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = 20;
        var skip = (pageNumber - 1) * pageSize;
        var query = _dbSet
            .Where(c => c.AuthorId == userId && !c.IsDeleted);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Post)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
        return new PagedResult<Comment>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
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
