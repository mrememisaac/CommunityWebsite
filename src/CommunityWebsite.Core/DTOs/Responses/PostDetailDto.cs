namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Detailed post DTO with content and comments
/// </summary>
public class PostDetailDto : ApiResponseBase
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto? Author { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
    public IEnumerable<CommentDto> Comments { get; set; } = new List<CommentDto>();
}
