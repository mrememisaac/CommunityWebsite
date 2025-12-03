namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Simple user DTO for view models
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int EventCount { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}
