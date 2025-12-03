using System.Net;
using System.Text.RegularExpressions;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Implementation of input sanitization to prevent XSS attacks
/// Uses regex-based filtering for safe basic HTML and WebUtility for HTML encoding
/// </summary>
public class InputSanitizer : IInputSanitizer
{
    /// <summary>
    /// Sanitize HTML content to prevent XSS attacks
    /// Removes dangerous tags while preserving safe formatting
    /// </summary>
    public string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        try
        {
            // Remove script tags and their content
            var result = Regex.Replace(input, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove event handlers
            result = Regex.Replace(result, @"\s*on\w+\s*=\s*[""']?[^""']*[""']?", string.Empty, RegexOptions.IgnoreCase);

            // Remove iframe, object, embed tags
            result = Regex.Replace(result, @"<(iframe|object|embed)[^>]*>.*?</\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove other dangerous tags
            result = Regex.Replace(result, @"</?(?!(?:b|i|u|p|br|ul|ol|li|a|strong|em|code|pre)\b)[^>]+>", string.Empty);

            return result;
        }
        catch
        {
            return HtmlEncode(input);
        }
    }

    /// <summary>
    /// Sanitize plain text by escaping HTML entities
    /// </summary>
    public string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return HtmlEncode(input);
    }

    /// <summary>
    /// HTML encode to prevent injection
    /// </summary>
    private static string HtmlEncode(string text)
    {
        return WebUtility.HtmlEncode(text);
    }
}
