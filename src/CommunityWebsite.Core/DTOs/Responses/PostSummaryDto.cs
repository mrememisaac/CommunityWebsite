namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Summary DTO for post listings
/// </summary>
public class PostSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public UserSummaryDto? Author { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}
