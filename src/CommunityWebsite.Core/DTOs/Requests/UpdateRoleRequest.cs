namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for updating a role
/// </summary>
public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
