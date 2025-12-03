namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for updating an event
/// </summary>
public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
}
