namespace CommunityWebsite.Core.DTOs.Responses;

/// <summary>
/// DTO for displaying admin user list
/// </summary>
public class AdminUserDto
{
    /// <summary>User ID</summary>
    public int Id { get; set; }

    /// <summary>Username</summary>
    public string Username { get; set; } = null!;

    /// <summary>Email address</summary>
    public string Email { get; set; } = null!;

    /// <summary>Comma-separated list of role names</summary>
    public string Roles { get; set; } = null!;

    /// <summary>User active status</summary>
    public bool IsActive { get; set; }

    /// <summary>Account creation date</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update date</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Number of posts created by this user</summary>
    public int PostCount { get; set; }

    /// <summary>Number of comments created by this user</summary>
    public int CommentCount { get; set; }
}

/// <summary>
/// DTO for displaying detailed admin user information
/// </summary>
public class AdminUserDetailDto
{
    /// <summary>User ID</summary>
    public int Id { get; set; }

    /// <summary>Username</summary>
    public string Username { get; set; } = null!;

    /// <summary>Email address</summary>
    public string Email { get; set; } = null!;

    /// <summary>User biography</summary>
    public string? Bio { get; set; }

    /// <summary>Profile image URL</summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>User active status</summary>
    public bool IsActive { get; set; }

    /// <summary>Account creation date</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update date</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>List of assigned roles</summary>
    public List<RoleDto> Roles { get; set; } = new();

    /// <summary>Number of posts created by this user</summary>
    public int PostCount { get; set; }

    /// <summary>Number of comments created by this user</summary>
    public int CommentCount { get; set; }
}
