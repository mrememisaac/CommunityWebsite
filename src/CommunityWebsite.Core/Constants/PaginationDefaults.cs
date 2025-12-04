namespace CommunityWebsite.Core.Constants;

/// <summary>
/// Default pagination and query size constants used across the application
/// </summary>
public static class PaginationDefaults
{
    /// <summary>Default page size for paginated queries</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Default page size for posts listings</summary>
    public const int PostsPageSize = 10;

    /// <summary>Number of featured/trending posts to display</summary>
    public const int FeaturedPostsLimit = 5;

    /// <summary>Number of days to consider for trending posts calculation</summary>
    public const int TrendingPostsDays = 7;

    /// <summary>Limit for featured posts from extended period</summary>
    public const int FeaturedPostsExtendedLimit = 10;

    /// <summary>Limit for featured posts from extended period in days</summary>
    public const int FeaturedPostsExtendedDays = 30;

    /// <summary>Number of upcoming events to display</summary>
    public const int UpcomingEventsLimit = 20;

    /// <summary>Content preview truncation length</summary>
    public const int ContentPreviewLength = 150;

    /// <summary>Extended content preview truncation length</summary>
    public const int ContentPreviewExtendedLength = 200;

    /// <summary>Recent posts limit for profile view</summary>
    public const int RecentPostsLimit = 5;
}
