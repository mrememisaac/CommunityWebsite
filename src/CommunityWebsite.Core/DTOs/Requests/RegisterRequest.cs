namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Register request DTO
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AgreeTerms { get; set; }
}
