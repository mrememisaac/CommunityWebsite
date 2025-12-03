using System.ComponentModel.DataAnnotations;

namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for updating a post
/// </summary>
public class UpdatePostRequest
{
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 300 characters")]
    public string? Title { get; set; }

    [MinLength(10, ErrorMessage = "Content must be at least 10 characters")]
    public string? Content { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }
}
