namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for creating a role
/// </summary>
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
