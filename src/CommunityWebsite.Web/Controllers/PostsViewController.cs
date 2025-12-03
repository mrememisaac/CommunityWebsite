using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// MVC Controller for Post views - serves Razor pages.
/// Inherits from <see cref="ViewControllerBase"/> for common MVC functionality.
/// </summary>
public class PostsViewController : ViewControllerBase
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly ILogger<PostsViewController> _logger;

    public PostsViewController(
        IPostService postService,
        ICommentService commentService,
        ILogger<PostsViewController> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays list of all posts with optional filtering
    /// </summary>
    [HttpGet]
    [Route("Posts")]
    [Route("Posts/Index")]
    public async Task<IActionResult> Index(string? search = null, string? sortBy = "newest", int? authorId = null, int page = 1)
    {
        _logger.LogInformation("GET Posts/Index - Search: {Search}, Sort: {Sort}, Page: {Page}", search, sortBy, page);

        // Get featured posts (we'll use this as the base list)
        var result = await _postService.GetFeaturedPostsAsync();
        var posts = result.Data?.ToList() ?? new List<PostSummaryDto>();

        // If search term provided, use search instead
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchResult = await _postService.SearchPostsAsync(search);
            posts = searchResult.Data?.ToList() ?? new List<PostSummaryDto>();
        }

        // Convert to PostDto for view compatibility
        var postDtos = posts.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Preview,
            AuthorId = p.Author?.Id ?? 0,
            AuthorUsername = p.Author?.Username,
            CreatedAt = p.CreatedAt,
            CommentCount = p.CommentCount
        }).ToList();

        // Apply sorting
        postDtos = sortBy switch
        {
            "oldest" => postDtos.OrderBy(p => p.CreatedAt).ToList(),
            "popular" => postDtos.OrderByDescending(p => p.CommentCount).ToList(),
            _ => postDtos.OrderByDescending(p => p.CreatedAt).ToList()
        };

        // Pagination
        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(postDtos.Count / (double)pageSize);
        postDtos = postDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.Search = search;
        ViewBag.SortBy = sortBy;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View("~/Views/Posts/Index.cshtml", postDtos);
    }

    /// <summary>
    /// Displays a single post with its comments
    /// </summary>
    [HttpGet]
    [Route("Posts/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        _logger.LogInformation("GET Posts/Details/{PostId}", id);

        var result = await _postService.GetPostDetailAsync(id);

        if (!result.IsSuccess || result.Data == null)
        {
            return View("~/Views/Posts/Details.cshtml", null);
        }

        var post = result.Data;

        // Get comments for the post
        var commentsResult = await _commentService.GetPostCommentsAsync(id);
        ViewBag.Comments = commentsResult.Data ?? new List<CommentDto>();

        // Map PostDetailDto to PostDto for the view
        var postDto = new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            AuthorId = post.Author?.Id ?? 0,
            AuthorUsername = post.Author?.Username,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            CommentCount = post.CommentCount
        };

        return View("~/Views/Posts/Details.cshtml", postDto);
    }

    /// <summary>
    /// Displays create post form
    /// </summary>
    [HttpGet]
    [Route("Posts/Create")]
    public IActionResult Create()
    {
        return View("~/Views/Posts/Create.cshtml");
    }

    /// <summary>
    /// Displays edit post form
    /// </summary>
    [HttpGet]
    [Route("Posts/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        _logger.LogInformation("GET Posts/Edit/{PostId}", id);

        var result = await _postService.GetPostDetailAsync(id);

        if (!result.IsSuccess || result.Data == null)
        {
            return View("~/Views/Posts/Edit.cshtml", null);
        }

        var post = result.Data;
        var postDto = new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            AuthorId = post.Author?.Id ?? 0,
            AuthorUsername = post.Author?.Username,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            CommentCount = post.CommentCount
        };

        return View("~/Views/Posts/Edit.cshtml", postDto);
    }
}
