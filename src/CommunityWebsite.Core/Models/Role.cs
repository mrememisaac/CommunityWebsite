using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWebsite.Core.Models;

/// <summary>
/// Represents a role in the community website.
/// </summary>
public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    [InverseProperty("Role")]
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
