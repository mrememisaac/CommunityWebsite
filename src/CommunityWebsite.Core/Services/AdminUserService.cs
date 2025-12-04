using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Admin user service implementation - Handles administrative user management operations
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILogger<AdminUserService> _logger;

    // Protected roles that cannot be removed from the last admin
    private static readonly string[] ProtectedRoles = { "Admin" };

    public AdminUserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        ILogger<AdminUserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all users with pagination and optional filtering
    /// </summary>
    public async Task<Result<IEnumerable<AdminUserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Retrieving all users for admin panel, page {PageNumber}, search: {SearchTerm}", pageNumber, searchTerm);

            if (pageNumber < 1)
                return Result<IEnumerable<AdminUserDto>>.Failure("Page number must be greater than zero.");

            if (pageSize < 1 || pageSize > 100)
                return Result<IEnumerable<AdminUserDto>>.Failure("Page size must be between 1 and 100.");

            var users = await _userRepository.GetAllUsersAsync();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                users = users.Where(u =>
                    u.Username.ToLower().Contains(lowerSearch) ||
                    u.Email.ToLower().Contains(lowerSearch)).ToList();
            }

            // Apply pagination
            var skip = (pageNumber - 1) * pageSize;
            var paginatedUsers = users.Skip(skip).Take(pageSize).ToList();

            var result = new List<AdminUserDto>();
            foreach (var user in paginatedUsers)
            {
                var postCount = await _postRepository.GetPostCountAsync(user.Id);
                var commentCount = await _commentRepository.GetCommentCountByUserAsync(user.Id);

                result.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = string.Join(", ", user.UserRoles.Select(ur => ur.Role.Name)),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    PostCount = postCount,
                    CommentCount = commentCount
                });
            }

            return Result<IEnumerable<AdminUserDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for admin panel");
            return Result<IEnumerable<AdminUserDto>>.Failure("An error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Gets a specific user with all their roles and details
    /// </summary>
    public async Task<Result<AdminUserDetailDto>> GetUserDetailAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Retrieving user detail for admin panel, user {UserId}", userId);

            if (userId <= 0)
                return Result<AdminUserDetailDto>.Failure("Invalid user ID.");

            var user = await _userRepository.GetUserWithRolesAsync(userId);
            if (user == null)
                return Result<AdminUserDetailDto>.Failure("User not found.");

            var postCount = await _postRepository.GetPostCountAsync(userId);
            var commentCount = await _commentRepository.GetCommentCountByUserAsync(userId);

            var detail = new AdminUserDetailDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList(),
                PostCount = postCount,
                CommentCount = commentCount
            };

            return Result<AdminUserDetailDto>.Success(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user detail for admin panel, user {UserId}", userId);
            return Result<AdminUserDetailDto>.Failure("An error occurred while retrieving user details.");
        }
    }

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    public async Task<Result<string>> AssignRoleToUserAsync(int userId, string roleName)
    {
        try
        {
            _logger.LogInformation("Assigning role {RoleName} to user {UserId}", roleName, userId);

            if (userId <= 0)
                return Result<string>.Failure("Invalid user ID.");

            if (string.IsNullOrWhiteSpace(roleName))
                return Result<string>.Failure("Role name is required.");

            var user = await _userRepository.GetUserWithRolesAsync(userId);
            if (user == null)
                return Result<string>.Failure("User not found.");

            var role = await _roleRepository.GetRoleByNameAsync(roleName);
            if (role == null)
                return Result<string>.Failure("Role not found.");

            // Check if user already has this role
            if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
                return Result<string>.Failure($"User already has the {roleName} role.");

            // Create new user role assignment
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            user.UserRoles.Add(userRole);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Role {RoleName} assigned to user {UserId} successfully", roleName, userId);
            return Result<string>.Success($"Role {roleName} assigned successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
            return Result<string>.Failure("An error occurred while assigning the role.");
        }
    }

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    public async Task<Result<string>> RemoveRoleFromUserAsync(int userId, string roleName)
    {
        try
        {
            _logger.LogInformation("Removing role {RoleName} from user {UserId}", roleName, userId);

            if (userId <= 0)
                return Result<string>.Failure("Invalid user ID.");

            if (string.IsNullOrWhiteSpace(roleName))
                return Result<string>.Failure("Role name is required.");

            var user = await _userRepository.GetUserWithRolesAsync(userId);
            if (user == null)
                return Result<string>.Failure("User not found.");

            var role = await _roleRepository.GetRoleByNameAsync(roleName);
            if (role == null)
                return Result<string>.Failure("Role not found.");

            // Check if user has this role
            var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
            if (userRole == null)
                return Result<string>.Failure($"User does not have the {roleName} role.");

            // Prevent removing the last Admin role
            if (roleName == "Admin")
            {
                var adminCount = await _userRepository.GetUsersByRoleAsync("Admin");
                if (adminCount.Count() <= 1)
                    return Result<string>.Failure("Cannot remove the last Admin role. At least one admin must exist.");
            }

            user.UserRoles.Remove(userRole);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Role {RoleName} removed from user {UserId} successfully", roleName, userId);
            return Result<string>.Success($"Role {roleName} removed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            return Result<string>.Failure("An error occurred while removing the role.");
        }
    }

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    public async Task<Result<string>> DeactivateUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Deactivating user {UserId}", userId);

            if (userId <= 0)
                return Result<string>.Failure("Invalid user ID.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result<string>.Failure("User not found.");

            if (!user.IsActive)
                return Result<string>.Failure("User is already inactive.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deactivated successfully", userId);
            return Result<string>.Success("User deactivated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return Result<string>.Failure("An error occurred while deactivating the user.");
        }
    }

    /// <summary>
    /// Reactivates a user account
    /// </summary>
    public async Task<Result<string>> ReactivateUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Reactivating user {UserId}", userId);

            if (userId <= 0)
                return Result<string>.Failure("Invalid user ID.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result<string>.Failure("User not found.");

            if (user.IsActive)
                return Result<string>.Failure("User is already active.");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} reactivated successfully", userId);
            return Result<string>.Success("User reactivated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user {UserId}", userId);
            return Result<string>.Failure("An error occurred while reactivating the user.");
        }
    }

    /// <summary>
    /// Gets all available roles
    /// </summary>
    public async Task<Result<IEnumerable<RoleDto>>> GetAllRolesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all available roles");

            var roles = await _roleRepository.GetAllAsync();

            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();

            return Result<IEnumerable<RoleDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Result<IEnumerable<RoleDto>>.Failure("An error occurred while retrieving roles.");
        }
    }
}
