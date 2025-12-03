namespace CommunityWebsite.Core.DTOs.Requests;

/// <summary>
/// Request DTO for creating an event
/// </summary>
public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
}
