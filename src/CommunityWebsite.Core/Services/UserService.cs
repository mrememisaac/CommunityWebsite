using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// User service implementation - Single Responsibility Principle
/// Handles user business logic only
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPostRepository postRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a user's profile with roles and post count
    /// </summary>
    public async Task<Result<UserProfileDto>> GetUserProfileAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Retrieving profile for user {UserId}", userId);

            if (userId <= 0)
            {
                return Result<UserProfileDto>.Failure("Invalid user ID");
            }

            var user = await _userRepository.GetUserWithRolesAsync(userId);
            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            var posts = await _postRepository.GetUserPostsAsync(userId);

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                PostCount = posts.Count(),
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };

            return Result<UserProfileDto>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
            return Result<UserProfileDto>.Failure("An error occurred while retrieving the user profile.");
        }
    }

    /// <summary>
    /// Gets a user by email address
    /// </summary>
    public async Task<Result<UserProfileDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Retrieving user by email");

            if (string.IsNullOrWhiteSpace(email))
            {
                return Result<UserProfileDto>.Failure("Email is required");
            }

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            var userWithRoles = await _userRepository.GetUserWithRolesAsync(user.Id);
            var posts = await _postRepository.GetUserPostsAsync(user.Id);

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                PostCount = posts.Count(),
                Roles = userWithRoles?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            };

            return Result<UserProfileDto>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email");
            return Result<UserProfileDto>.Failure("An error occurred while retrieving the user.");
        }
    }

    /// <summary>
    /// Gets a user by username
    /// </summary>
    public async Task<Result<UserProfileDto>> GetUserByUsernameAsync(string username)
    {
        try
        {
            _logger.LogInformation("Retrieving user by username {Username}", username);

            if (string.IsNullOrWhiteSpace(username))
            {
                return Result<UserProfileDto>.Failure("Username is required");
            }

            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            var userWithRoles = await _userRepository.GetUserWithRolesAsync(user.Id);
            var posts = await _postRepository.GetUserPostsAsync(user.Id);

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                PostCount = posts.Count(),
                Roles = userWithRoles?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            };

            return Result<UserProfileDto>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username {Username}", username);
            return Result<UserProfileDto>.Failure("An error occurred while retrieving the user.");
        }
    }

    /// <summary>
    /// Gets active users with pagination
    /// </summary>
    public async Task<Result<IEnumerable<UserSummaryDto>>> GetActiveUsersAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving active users, page {PageNumber}, size {PageSize}", pageNumber, pageSize);

            if (pageNumber < 1)
            {
                return Result<IEnumerable<UserSummaryDto>>.Failure("Page number must be at least 1");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Result<IEnumerable<UserSummaryDto>>.Failure("Page size must be between 1 and 100");
            }

            var users = await _userRepository.GetActiveUsersAsync(pageNumber, pageSize);

            var result = users.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username
            });

            return Result<IEnumerable<UserSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active users");
            return Result<IEnumerable<UserSummaryDto>>.Failure("An error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Gets users by role name
    /// </summary>
    public async Task<Result<IEnumerable<UserSummaryDto>>> GetUsersByRoleAsync(string roleName)
    {
        try
        {
            _logger.LogInformation("Retrieving users with role {RoleName}", roleName);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return Result<IEnumerable<UserSummaryDto>>.Failure("Role name is required");
            }

            var users = await _userRepository.GetUsersByRoleAsync(roleName);

            var result = users.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username
            });

            return Result<IEnumerable<UserSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by role {RoleName}", roleName);
            return Result<IEnumerable<UserSummaryDto>>.Failure("An error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Updates a user's profile
    /// </summary>
    public async Task<Result<UserProfileDto>> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Updating profile for user {UserId}", userId);

            if (userId <= 0)
            {
                return Result<UserProfileDto>.Failure("Invalid user ID");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            // Update fields if provided
            if (request.Bio != null)
            {
                user.Bio = request.Bio;
            }

            if (request.ProfileImageUrl != null)
            {
                user.ProfileImageUrl = request.ProfileImageUrl;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Profile updated for user {UserId}", userId);

            return await GetUserProfileAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return Result<UserProfileDto>.Failure("An error occurred while updating the profile.");
        }
    }

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    public async Task<Result> DeactivateUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Deactivating user {UserId}", userId);

            if (userId <= 0)
            {
                return Result.Failure("Invalid user ID");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (!user.IsActive)
            {
                return Result.Failure("User is already deactivated");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deactivated successfully", userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return Result.Failure("An error occurred while deactivating the user.");
        }
    }

    /// <summary>
    /// Reactivates a user account
    /// </summary>
    public async Task<Result> ReactivateUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Reactivating user {UserId}", userId);

            if (userId <= 0)
            {
                return Result.Failure("Invalid user ID");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (user.IsActive)
            {
                return Result.Failure("User is already active");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} reactivated successfully", userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user {UserId}", userId);
            return Result.Failure("An error occurred while reactivating the user.");
        }
    }
}
