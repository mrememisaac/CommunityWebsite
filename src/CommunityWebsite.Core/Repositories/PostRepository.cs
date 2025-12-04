using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Constants;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;

namespace CommunityWebsite.Core.Repositories;

/// <summary>
/// Post repository implementation showcasing LINQ queries and performance optimization.
/// </summary>
public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(CommunityDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets active (non-deleted, non-locked) posts with pagination.
    /// Demonstrates: Filtering, Pagination, Query optimization
    /// </summary>
    public async Task<IEnumerable<Post>> GetActivePostsAsync(int pageNumber = 1, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = PaginationDefaults.PostsPageSize;

        var skip = (pageNumber - 1) * pageSize;

        return await _dbSet
            .Where(p => !p.IsDeleted && !p.IsLocked)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Include(p => p.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets posts by category with pagination.
    /// Demonstrates: String comparison, Pagination, Eager loading
    /// </summary>
    public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(string category, int pageSize = 0)
    {
        if (pageSize <= 0)
            pageSize = PaginationDefaults.DefaultPageSize;

        return await _dbSet
            .Where(p => p.Category == category && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize)
            .Include(p => p.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets all posts by a specific user.
    /// </summary>
    public async Task<IEnumerable<Post>> GetUserPostsAsync(int userId, bool includeSoftDeleted = false)
    {
        var query = _dbSet.Where(p => p.AuthorId == userId);

        if (!includeSoftDeleted)
        {
            query = query.Where(p => !p.IsDeleted);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a post with all its comments (eager loading).
    /// Demonstrates: Explicit loading, Navigation properties
    /// </summary>
    public async Task<Post?> GetPostWithCommentsAsync(int postId)
    {
        return await _dbSet
            .Where(p => p.Id == postId && !p.IsDeleted)
            .Include(p => p.Author)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .ThenInclude(c => c.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets trending posts based on view count over a period.
    /// Demonstrates: Date calculations, Ordering, Top N queries
    /// </summary>
    public async Task<IEnumerable<Post>> GetTrendingPostsAsync(int days = 0, int limit = 0)
    {
        if (days <= 0)
            days = PaginationDefaults.TrendingPostsDays;

        if (limit <= 0)
            limit = PaginationDefaults.FeaturedPostsLimit;

        var startDate = DateTime.UtcNow.AddDays(-days);

        return await _dbSet
            .Where(p => p.CreatedAt >= startDate && !p.IsDeleted)
            .OrderByDescending(p => p.ViewCount)
            .ThenByDescending(p => p.CreatedAt)
            .Take(limit)
            .Include(p => p.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Searches posts by title and content.
    /// Demonstrates: Full-text search patterns, Multiple conditions
    /// </summary>
    public async Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(p => !p.IsDeleted &&
                   (p.Title.ToLower().Contains(lowerSearchTerm) ||
                    p.Content.ToLower().Contains(lowerSearchTerm)))
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Author)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets the count of posts by a specific user.
    /// Demonstrates: Efficient count query
    /// </summary>
    public async Task<int> GetPostCountAsync(int userId)
    {
        return await _dbSet
            .Where(p => p.AuthorId == userId && !p.IsDeleted)
            .CountAsync();
    }

    /// <summary>
    /// Increments view count for a post.
    /// Demonstrates: Update specific property, Concurrency patterns
    /// </summary>
    public async Task IncrementViewCountAsync(int postId)
    {
        var post = await _dbSet.FindAsync(postId);
        if (post != null)
        {
            post.ViewCount++;
            post.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(post);
            await _context.SaveChangesAsync();
        }
    }
}
