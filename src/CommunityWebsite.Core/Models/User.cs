using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWebsite.Core.Models;

/// <summary>
/// Represents a user in the community website.
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }

    [Url]
    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [InverseProperty("Author")]
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    [InverseProperty("Author")]
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    [InverseProperty("User")]
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
