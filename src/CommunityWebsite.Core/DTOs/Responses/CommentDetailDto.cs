namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Extended comment DTO with replies for nested display
/// </summary>
public class CommentDetailDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public IEnumerable<CommentDto> Replies { get; set; } = new List<CommentDto>();
}
