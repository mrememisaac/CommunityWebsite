using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWebsite.Core.Models;

/// <summary>
/// Represents an event in the community.
/// </summary>
public class Event
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(500)]
    public string? Location { get; set; }

    public int OrganizerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Range(0, int.MaxValue)]
    public int AttendeeCount { get; set; } = 0;

    public bool IsCancelled { get; set; } = false;

    // Foreign keys
    [ForeignKey("OrganizerId")]
    public User Organizer { get; set; } = null!;
}
