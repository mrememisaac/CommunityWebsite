namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Role DTO for API responses
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
}
