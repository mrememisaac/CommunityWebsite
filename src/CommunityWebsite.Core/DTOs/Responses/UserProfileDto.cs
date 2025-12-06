namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Detailed user profile DTO
/// </summary>
public class UserProfileDto : ApiResponseBase
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PostCount { get; set; }
    public List<string> Roles { get; set; } = new();
}
