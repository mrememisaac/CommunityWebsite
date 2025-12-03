using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWebsite.Core.Models;

/// <summary>
/// Represents a forum post or discussion topic.
/// </summary>
public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int AuthorId { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int ViewCount { get; set; } = 0;

    public bool IsPinned { get; set; } = false;

    public bool IsLocked { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    // Foreign keys
    [ForeignKey("AuthorId")]
    public User Author { get; set; } = null!;

    // Navigation properties
    [InverseProperty("Post")]
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
