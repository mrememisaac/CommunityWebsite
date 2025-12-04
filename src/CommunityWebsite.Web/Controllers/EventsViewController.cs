using Microsoft.AspNetCore.Mvc;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// MVC Controller for Event views - serves Razor pages.
/// Inherits from <see cref="ViewControllerBase"/> for common MVC functionality.
/// </summary>
public class EventsViewController : ViewControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsViewController> _logger;

    public EventsViewController(
        IEventService eventService,
        ILogger<EventsViewController> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays list of all events with optional filtering
    /// </summary>
    [HttpGet]
    [Route("Events")]
    [Route("Events/Index")]
    public async Task<IActionResult> Index(string? search = null, string? period = "upcoming", string? sortBy = "date-asc", int page = 1)
    {
        _logger.LogInformation("GET Events/Index - Search: {Search}, Period: {Period}, Page: {Page}", search, period, page);

        // Get events based on period
        IEnumerable<EventDto> events;
        if (period == "past")
        {
            var result = await _eventService.GetPastEventsAsync(100);
            events = result.Data ?? new List<EventDto>();
        }
        else
        {
            var result = await _eventService.GetUpcomingEventsAsync(100);
            events = result.Data ?? new List<EventDto>();
        }

        var eventList = events.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            eventList = eventList.Where(e =>
                e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        // Apply sorting
        eventList = sortBy switch
        {
            "date-desc" => [.. eventList.OrderByDescending(e => e.Date)],
            "title" => [.. eventList.OrderBy(e => e.Title)],
            _ => [.. eventList.OrderBy(e => e.Date)] // date-asc
        };

        // Pagination
        const int pageSize = 9;
        var totalPages = (int)Math.Ceiling(eventList.Count / (double)pageSize);
        eventList = [.. eventList.Skip((page - 1) * pageSize).Take(pageSize)];

        ViewBag.Search = search;
        ViewBag.Period = period;
        ViewBag.SortBy = sortBy;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View("~/Views/Events/Index.cshtml", eventList);
    }

    /// <summary>
    /// Displays a single event
    /// </summary>
    [HttpGet]
    [Route("Events/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        _logger.LogInformation("GET Events/Details/{EventId}", id);

        var result = await _eventService.GetEventByIdAsync(id);

        if (!result.IsSuccess || result.Data == null)
        {
            return View("~/Views/Events/Details.cshtml", null);
        }

        return View("~/Views/Events/Details.cshtml", result.Data);
    }

    /// <summary>
    /// Displays create event form
    /// </summary>
    [HttpGet]
    [Route("Events/Create")]
    public IActionResult Create()
    {
        return View("~/Views/Events/Create.cshtml");
    }

    /// <summary>
    /// Displays edit event form
    /// </summary>
    [HttpGet]
    [Route("Events/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        _logger.LogInformation("GET Events/Edit/{EventId}", id);

        var result = await _eventService.GetEventByIdAsync(id);

        if (!result.IsSuccess || result.Data == null)
        {
            return View("~/Views/Events/Edit.cshtml", null);
        }

        return View("~/Views/Events/Edit.cshtml", result.Data);
    }
}
