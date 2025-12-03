using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWebsite.Core.Models;

/// <summary>
/// Represents a comment on a post.
/// </summary>
public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int PostId { get; set; }

    [Required]
    public int AuthorId { get; set; }

    public int? ParentCommentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    // Foreign keys
    [ForeignKey("PostId")]
    public Post Post { get; set; } = null!;

    [ForeignKey("AuthorId")]
    public User Author { get; set; } = null!;

    [ForeignKey("ParentCommentId")]
    public Comment? ParentComment { get; set; }

    // Navigation properties
    [InverseProperty("ParentComment")]
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
