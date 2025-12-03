namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// Event DTO for API responses
/// </summary>
public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsCancelled { get; set; }
    public int OrganizerId { get; set; }
    public string? OrganizerUsername { get; set; }
    public UserSummaryDto? Organizer { get; set; }
    public int AttendeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
