namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for updating user profile
/// </summary>
public class UpdateUserProfileRequest
{
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}
