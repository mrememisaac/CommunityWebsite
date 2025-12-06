namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Comment DTO for API responses
/// </summary>
public class CommentDto : ApiResponseBase
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto? Author { get; set; }
    public int AuthorId => Author?.Id ?? 0;
    public string? AuthorUsername => Author?.Username;
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReplyCount { get; set; }
}
