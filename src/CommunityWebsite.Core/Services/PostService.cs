using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.Constants;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Post service implementation - Single Responsibility Principle
/// Handles post business logic only, delegates validation and data access
/// </summary>
public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPostValidator _postValidator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PostService> _logger;

    // Cache keys
    private const string FeaturedPostsCacheKey = "featured_posts";
    private const string SearchResultsCacheKeyPrefix = "search_results_";

    // Cache expiration settings
    private static readonly MemoryCacheEntryOptions PostCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Size = 1
    };

    private static readonly MemoryCacheEntryOptions SearchCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        Size = 1
    };

    public PostService(
        IPostRepository postRepository,
        IUserRepository userRepository,
        IPostValidator postValidator,
        IMemoryCache cache,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _postValidator = postValidator ?? throw new ArgumentNullException(nameof(postValidator));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PostDetailDto?>> GetPostDetailAsync(int postId)
    {
        try
        {
            _logger.LogInformation("Retrieving post detail for post {PostId}", postId);

            if (postId <= 0)
            {
                var error = "Invalid post ID.";
                _logger.LogWarning(error);
                return Result<PostDetailDto?>.Failure(error);
            }

            var post = await _postRepository.GetPostWithCommentsAsync(postId);
            if (post == null)
            {
                _logger.LogInformation("Post {PostId} not found", postId);
                return Result<PostDetailDto?>.Success(null);
            }

            // Increment view count asynchronously
            await _postRepository.IncrementViewCountAsync(postId);

            var result = MapPostToDetailDto(post);
            return Result<PostDetailDto?>.Success(result);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving post {PostId}", postId);
            return Result<PostDetailDto?>.Failure("Database error occurred while retrieving the post.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving post {PostId}", postId);
            return Result<PostDetailDto?>.Failure("Request timeout while retrieving the post.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving post {PostId}", postId);
            return Result<PostDetailDto?>.Failure("An error occurred while retrieving the post.");
        }
    }

    public async Task<Result<PagedResult<PostSummaryDto>>> GetFeaturedPostsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving featured posts");

            // Try to get from cache
            if (_cache.TryGetValue(FeaturedPostsCacheKey, out PagedResult<PostSummaryDto>? cachedPosts))
            {
                _logger.LogDebug("Featured posts retrieved from cache");
                return Result<PagedResult<PostSummaryDto>>.Success(cachedPosts!);
            }

            var posts = await _postRepository.GetTrendingPostsAsync(
                days: PaginationDefaults.FeaturedPostsExtendedDays,
                limit: PaginationDefaults.FeaturedPostsExtendedLimit);

            var result = posts.Select(p => p.ToSummaryDto()).ToList();

            var pagedResult = new PagedResult<PostSummaryDto> { Items = result, TotalCount = result.Count, PageNumber = pageNumber, PageSize = pageSize };

            // Cache the result
            _cache.Set(FeaturedPostsCacheKey, pagedResult, PostCacheOptions);
            _logger.LogDebug("Featured posts cached");

            return Result<PagedResult<PostSummaryDto>>.Success(pagedResult);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving featured posts");
            return Result<PagedResult<PostSummaryDto>>.Failure("Database error occurred while retrieving featured posts.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving featured posts");
            return Result<PagedResult<PostSummaryDto>>.Failure("Request timeout while retrieving featured posts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving featured posts");
            return Result<PagedResult<PostSummaryDto>>.Failure("An error occurred while retrieving featured posts.");
        }
    }

    public async Task<Result<PagedResult<PostSummaryDto>>> GetPostsByCategoryAsync(string category, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving posts for category {Category}, page {PageNumber}", category, pageNumber);

            if (string.IsNullOrWhiteSpace(category))
                return Result<PagedResult<PostSummaryDto>>.Failure("Category is required.");

            if (pageNumber < 1)
                return Result<PagedResult<PostSummaryDto>>.Failure("Page number must be greater than zero.");

            var pagedPosts = await _postRepository.GetPostsByCategoryAsync(category, pageNumber, pageSize);
            var dtoResult = new PagedResult<PostSummaryDto>
            {
                Items = pagedPosts.Items.Select(p => p.ToSummaryDto()).ToList(),
                TotalCount = pagedPosts.TotalCount,
                PageNumber = pagedPosts.PageNumber,
                PageSize = pagedPosts.PageSize
            };
            return Result<PagedResult<PostSummaryDto>>.Success(dtoResult);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving posts for category {Category}", category);
            return Result<PagedResult<PostSummaryDto>>.Failure("Database error occurred while retrieving posts.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving posts for category {Category}", category);
            return Result<PagedResult<PostSummaryDto>>.Failure("Request timeout while retrieving posts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving posts for category {Category}", category);
            return Result<PagedResult<PostSummaryDto>>.Failure("An error occurred while retrieving posts.");
        }
    }

    public async Task<Result<PostDetailDto>> CreatePostAsync(CreatePostRequest request)
    {
        try
        {
            _logger.LogInformation("Creating post for user {UserId}", request.AuthorId);

            // Validate request
            var validationResult = await _postValidator.ValidateCreateRequestAsync(request);
            if (!validationResult.IsSuccess)
                return Result<PostDetailDto>.Failure(validationResult.ErrorMessage!);

            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                Category = request.Category,
                AuthorId = request.AuthorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPost = await _postRepository.AddAsync(post);
            await _postRepository.SaveChangesAsync();

            // Invalidate featured posts cache
            _cache.Remove(FeaturedPostsCacheKey);
            _logger.LogDebug("Cache invalidated: {CacheKey}", FeaturedPostsCacheKey);

            _logger.LogInformation("Post {PostId} created successfully by user {UserId}", createdPost.Id, request.AuthorId);

            var user = await _userRepository.GetByIdAsync(request.AuthorId);
            return Result<PostDetailDto>.Success(new PostDetailDto
            {
                Id = createdPost.Id,
                Title = createdPost.Title,
                Content = createdPost.Content,
                Author = new UserSummaryDto { Id = user!.Id, Username = user.Username },
                Category = createdPost.Category,
                CreatedAt = createdPost.CreatedAt,
                UpdatedAt = createdPost.UpdatedAt,
                ViewCount = createdPost.ViewCount,
                CommentCount = 0,
                Comments = new List<CommentDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post for user {UserId}", request.AuthorId);
            return Result<PostDetailDto>.Failure("An error occurred while creating the post.");
        }
    }

    public async Task<Result<PostDetailDto>> UpdatePostAsync(int postId, UpdatePostRequest request)
    {
        try
        {
            _logger.LogInformation("Updating post {PostId}", postId);

            var validationResult = await _postValidator.ValidateUpdateRequestAsync(postId, request);
            if (!validationResult.IsSuccess)
                return Result<PostDetailDto>.Failure(validationResult.ErrorMessage!);

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for update", postId);
                return Result<PostDetailDto>.Failure("Post not found.");
            }

            post.Title = request.Title ?? post.Title;
            post.Content = request.Content ?? post.Content;
            post.Category = request.Category ?? post.Category;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdateAsync(post);
            await _postRepository.SaveChangesAsync();

            // Invalidate featured posts cache
            _cache.Remove(FeaturedPostsCacheKey);
            _logger.LogDebug("Cache invalidated: {CacheKey}", FeaturedPostsCacheKey);

            _logger.LogInformation("Post {PostId} updated successfully", postId);

            var updatedPost = await _postRepository.GetPostWithCommentsAsync(postId);
            return Result<PostDetailDto>.Success(MapPostToDetailDto(updatedPost!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", postId);
            return Result<PostDetailDto>.Failure("An error occurred while updating the post.");
        }
    }

    public async Task<Result> DeletePostAsync(int postId)
    {
        try
        {
            _logger.LogInformation("Deleting post {PostId}", postId);

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for deletion", postId);
                return Result.Failure("Post not found.");
            }

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdateAsync(post);
            await _postRepository.SaveChangesAsync();

            // Invalidate featured posts cache
            _cache.Remove(FeaturedPostsCacheKey);
            _logger.LogDebug("Cache invalidated: {CacheKey}", FeaturedPostsCacheKey);

            _logger.LogInformation("Post {PostId} deleted successfully", postId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", postId);
            return Result.Failure("An error occurred while deleting the post.");
        }
    }

    public async Task<Result<PagedResult<PostSummaryDto>>> SearchPostsAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Searching posts with term: {SearchTerm}", searchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
                return Result<PagedResult<PostSummaryDto>>.Failure("Search term is required.");

            var cacheKey = $"{SearchResultsCacheKeyPrefix}{searchTerm.ToLower()}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out PagedResult<PostSummaryDto>? cachedResults))
            {
                _logger.LogDebug("Search results for '{SearchTerm}' retrieved from cache", searchTerm);
                return Result<PagedResult<PostSummaryDto>>.Success(cachedResults!);
            }

            var pagedResult = await _postRepository.SearchPostsAsync(searchTerm);
            var result = pagedResult.Items.Select(p => new PostSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Preview = TruncateContent(p.Content, 150),
                Author = new UserSummaryDto { Id = p.Author.Id, Username = p.Author.Username },
                Category = p.Category,
                CreatedAt = p.CreatedAt,
                ViewCount = p.ViewCount,
                CommentCount = p.Comments.Count
            }).ToList();
            var resultPaged = new PagedResult<PostSummaryDto> { Items = result, TotalCount = pagedResult.TotalCount, PageNumber = pagedResult.PageNumber, PageSize = pagedResult.PageSize };
            // Cache the result for 5 minutes
            _cache.Set(cacheKey, resultPaged, SearchCacheOptions);
            _logger.LogDebug("Search results for '{SearchTerm}' cached", searchTerm);
            return Result<PagedResult<PostSummaryDto>>.Success(resultPaged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with term: {SearchTerm}", searchTerm);
            return Result<PagedResult<PostSummaryDto>>.Failure("An error occurred while searching posts.");
        }
    }

    private PostDetailDto MapPostToDetailDto(Post post)
    {
        return new PostDetailDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Author = new UserSummaryDto { Id = post.Author.Id, Username = post.Author.Username },
            Category = post.Category,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            ViewCount = post.ViewCount,
            CommentCount = post.Comments.Count,
            Comments = post.Comments
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    Author = new UserSummaryDto { Id = c.Author.Id, Username = c.Author.Username },
                    CreatedAt = c.CreatedAt,
                    ReplyCount = c.Replies.Count
                })
                .ToList()
        };
    }

    private string TruncateContent(string content, int maxLength)
    {
        return content.Length > maxLength
            ? content[..maxLength] + "..."
            : content;
    }

    /// <summary>
    /// Verifies if the specified user owns the post.
    /// Used for authorization before update/delete operations.
    /// </summary>
    public async Task<Result<bool>> VerifyOwnershipAsync(int postId, int userId)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                return Result<bool>.Failure("Post not found");

            return Result<bool>.Success(post.AuthorId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying ownership for post {PostId} and user {UserId}", postId, userId);
            return Result<bool>.Failure("An error occurred while verifying ownership.");
        }
    }

    /// <summary>
    /// Gets all posts created by a specific user
    /// </summary>
    public async Task<Result<PagedResult<PostSummaryDto>>> GetPostsByUserAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var pagedPosts = await _postRepository.GetUserPostsAsync(userId, pageNumber, pageSize, false);

            var result = pagedPosts.Items
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Preview = TruncateContent(p.Content, 150),
                    Author = new UserSummaryDto { Id = p.Author?.Id ?? 0, Username = p.Author?.Username ?? "Unknown" },
                    Category = p.Category,
                    CreatedAt = p.CreatedAt,
                    ViewCount = p.ViewCount,
                    CommentCount = p.Comments?.Count(c => !c.IsDeleted) ?? 0
                }).ToList();

            var pagedResult = new PagedResult<PostSummaryDto>
            {
                Items = result,
                TotalCount = pagedPosts.TotalCount,
                PageNumber = pagedPosts.PageNumber,
                PageSize = pagedPosts.PageSize
            };

            return Result<PagedResult<PostSummaryDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts for user {UserId}", userId);
            return Result<PagedResult<PostSummaryDto>>.Failure("An error occurred while retrieving posts.");
        }
    }
}