using System.ComponentModel.DataAnnotations;

namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for creating a comment
/// </summary>
public class CreateCommentRequest
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 5000 characters")]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Valid AuthorId is required")]
    public int AuthorId { get; set; }

    /// <summary>
    /// Optional parent comment ID for replies
    /// </summary>
    public int? ParentCommentId { get; set; }
}
