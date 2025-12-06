namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Authentication response DTO
/// </summary>
public class AuthenticationResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
