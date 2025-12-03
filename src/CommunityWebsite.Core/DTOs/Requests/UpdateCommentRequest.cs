using System.ComponentModel.DataAnnotations;

namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for updating a comment
/// </summary>
public class UpdateCommentRequest
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 5000 characters")]
    public string Content { get; set; } = string.Empty;
}
