namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Simple post DTO for view models
/// </summary>
public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string? AuthorUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CommentCount { get; set; }
}
