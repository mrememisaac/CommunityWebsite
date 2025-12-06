using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Home controller for main site pages.
/// Inherits from <see cref="ViewControllerBase"/> for common MVC functionality.
/// </summary>
public class HomeController : ViewControllerBase
{
    private readonly IPostService _postService;
    private readonly IEventService _eventService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPostService postService,
        IEventService eventService,
        ILogger<HomeController> logger)
    {
        _postService = postService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Home page - displays recent posts and upcoming events
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get recent posts for homepage (using featured posts)
            var postsResult = await _postService.GetFeaturedPostsAsync();
            var eventsResult = await _eventService.GetUpcomingEventsAsync(4);

            ViewBag.RecentPosts = postsResult.IsSuccess ? postsResult.Data?.Items?.Take(6) ?? new List<Core.DTOs.Responses.PostSummaryDto>() : new List<Core.DTOs.Responses.PostSummaryDto>();
            ViewBag.UpcomingEvents = eventsResult.IsSuccess ? eventsResult.Data?.Items ?? new List<Core.DTOs.Responses.EventDto>() : new List<Core.DTOs.Responses.EventDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page data");
            ViewBag.RecentPosts = new List<Core.DTOs.Responses.PostSummaryDto>();
            ViewBag.UpcomingEvents = new List<Core.DTOs.Responses.EventDto>();
        }

        return View();
    }

    /// <summary>
    /// About page
    /// </summary>
    public IActionResult About()
    {
        return View();
    }

    /// <summary>
    /// Contact page
    /// </summary>
    public IActionResult Contact()
    {
        return View();
    }

    /// <summary>
    /// Error page
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
