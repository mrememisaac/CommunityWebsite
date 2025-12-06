namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Summary DTO for user references in other DTOs
/// </summary>
public class UserSummaryDto : ApiResponseBase
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}
