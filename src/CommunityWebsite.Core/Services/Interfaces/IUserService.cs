using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// User service interface - Dependency Inversion Principle
/// Defines contracts for user operations
/// </summary>
public interface IUserService
{
    Task<Result<UserProfileDto>> GetUserProfileAsync(int userId);
    Task<Result<UserProfileDto>> GetUserByEmailAsync(string email);
    Task<Result<UserProfileDto>> GetUserByUsernameAsync(string username);
    Task<Result<PagedResult<UserSummaryDto>>> GetActiveUsersAsync(int pageNumber, int pageSize);
    Task<Result<PagedResult<UserSummaryDto>>> GetUsersByRoleAsync(string roleName, int pageNumber, int pageSize);
    Task<Result<UserProfileDto>> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request);
    Task<Result> DeactivateUserAsync(int userId);
    Task<Result> ReactivateUserAsync(int userId);
}
