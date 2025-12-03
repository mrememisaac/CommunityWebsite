namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Input sanitization service interface for preventing XSS and HTML injection attacks
/// </summary>
public interface IInputSanitizer
{
    string SanitizeHtml(string? input);
    string SanitizeText(string? input);
}
