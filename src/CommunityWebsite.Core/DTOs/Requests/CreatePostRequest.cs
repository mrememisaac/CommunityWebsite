using System.ComponentModel.DataAnnotations;

namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for creating a post
/// </summary>
public class CreatePostRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 300 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    [MinLength(10, ErrorMessage = "Content must be at least 10 characters")]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Valid AuthorId is required")]
    public int AuthorId { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }
}
