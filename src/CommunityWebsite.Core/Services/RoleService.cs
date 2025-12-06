using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Role service implementation - Single Responsibility Principle
/// Handles role business logic only
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RoleService> _logger;

    // Cache keys
    private const string AllRolesCacheKey = "all_roles";
    private const string RoleByNameCacheKeyPrefix = "role_name_";
    private const string RoleByIdCacheKeyPrefix = "role_id_";

    // System roles that cannot be modified or deleted
    private static readonly string[] ProtectedRoles = { "Admin", "User", "Moderator" };

    // Cache expiration settings
    private static readonly MemoryCacheEntryOptions RoleCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Size = 1
    };

    public RoleService(
        IRoleRepository roleRepository,
        IMemoryCache cache,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a role by ID with caching
    /// </summary>
    public async Task<Result<RoleDto>> GetRoleByIdAsync(int roleId)
    {
        try
        {
            _logger.LogInformation("Retrieving role with ID {RoleId}", roleId);

            if (roleId <= 0)
            {
                return Result<RoleDto>.Failure("Invalid role ID");
            }

            var cacheKey = $"{RoleByIdCacheKeyPrefix}{roleId}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out RoleDto? cachedRole))
            {
                _logger.LogDebug("Role {RoleId} retrieved from cache", roleId);
                return Result<RoleDto>.Success(cachedRole!);
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return Result<RoleDto>.Failure("Role not found");
            }

            var dto = MapToDto(role);

            // Cache the result
            _cache.Set(cacheKey, dto, RoleCacheOptions);
            _logger.LogDebug("Role {RoleId} cached", roleId);

            return Result<RoleDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", roleId);
            return Result<RoleDto>.Failure("An error occurred while retrieving the role.");
        }
    }

    /// <summary>
    /// Gets a role by name with caching
    /// </summary>
    public async Task<Result<RoleDto>> GetRoleByNameAsync(string name)
    {
        try
        {
            _logger.LogInformation("Retrieving role by name {RoleName}", name);

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<RoleDto>.Failure("Role name is required");
            }

            var cacheKey = $"{RoleByNameCacheKeyPrefix}{name.ToLower()}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out RoleDto? cachedRole))
            {
                _logger.LogDebug("Role {RoleName} retrieved from cache", name);
                return Result<RoleDto>.Success(cachedRole!);
            }

            var role = await _roleRepository.GetRoleByNameAsync(name);
            if (role == null)
            {
                return Result<RoleDto>.Failure("Role not found");
            }

            var dto = MapToDto(role);

            // Cache the result
            _cache.Set(cacheKey, dto, RoleCacheOptions);
            _logger.LogDebug("Role {RoleName} cached", name);

            return Result<RoleDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role by name {RoleName}", name);
            return Result<RoleDto>.Failure("An error occurred while retrieving the role.");
        }
    }

    /// <summary>
    /// Gets all roles with user counts and caching
    /// </summary>
    public async Task<Result<PagedResult<RoleDto>>> GetAllRolesAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving all roles");

            // Try to get from cache
            if (_cache.TryGetValue(AllRolesCacheKey, out PagedResult<RoleDto>? cachedRoles))
            {
                _logger.LogDebug("All roles retrieved from cache");
                return Result<PagedResult<RoleDto>>.Success(cachedRoles!);
            }

            var pagedRoles = await _roleRepository.GetAllRolesWithUsersAsync(pageNumber, pageSize);
            var dtoResult = new PagedResult<RoleDto>
            {
                Items = pagedRoles.Items.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    UserCount = r.UserRoles.Count
                }).ToList(),
                TotalCount = pagedRoles.TotalCount,
                PageNumber = pagedRoles.PageNumber,
                PageSize = pagedRoles.PageSize
            };
            // Cache the result
            _cache.Set(AllRolesCacheKey, dtoResult, RoleCacheOptions);
            _logger.LogDebug("All roles cached");
            return Result<PagedResult<RoleDto>>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            return Result<PagedResult<RoleDto>>.Failure("An error occurred while retrieving roles.");
        }
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        try
        {
            _logger.LogInformation("Creating role {RoleName}", request.Name);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<RoleDto>.Failure("Role name is required");
            }

            if (request.Name.Length < 2 || request.Name.Length > 50)
            {
                return Result<RoleDto>.Failure("Role name must be between 2 and 50 characters");
            }

            // Check if role already exists
            var exists = await _roleRepository.RoleExistsAsync(request.Name);
            if (exists)
            {
                return Result<RoleDto>.Failure("A role with this name already exists");
            }

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description
            };

            await _roleRepository.AddAsync(role);
            await _roleRepository.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove(AllRolesCacheKey);
            _logger.LogDebug("Cache invalidated: {CacheKey}", AllRolesCacheKey);

            _logger.LogInformation("Role {RoleName} created with ID {RoleId}", role.Name, role.Id);

            var dto = MapToDto(role);
            return Result<RoleDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", request.Name);
            return Result<RoleDto>.Failure("An error occurred while creating the role.");
        }
    }

    /// <summary>
    /// Updates an existing role
    /// </summary>
    public async Task<Result<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        try
        {
            _logger.LogInformation("Updating role {RoleId}", roleId);

            if (roleId <= 0)
            {
                return Result<RoleDto>.Failure("Invalid role ID");
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return Result<RoleDto>.Failure("Role not found");
            }

            // Check if it's a protected role and name is being changed
            if (ProtectedRoles.Contains(role.Name) &&
                !string.IsNullOrEmpty(request.Name) &&
                request.Name != role.Name)
            {
                return Result<RoleDto>.Failure($"Cannot rename the protected role '{role.Name}'");
            }

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                if (request.Name.Length < 2 || request.Name.Length > 50)
                {
                    return Result<RoleDto>.Failure("Role name must be between 2 and 50 characters");
                }

                // Check if new name already exists (for different role)
                var existingRole = await _roleRepository.GetRoleByNameAsync(request.Name);
                if (existingRole != null && existingRole.Id != roleId)
                {
                    return Result<RoleDto>.Failure("A role with this name already exists");
                }

                role.Name = request.Name;
            }

            // Update description if provided
            if (request.Description != null)
            {
                role.Description = request.Description;
            }

            await _roleRepository.UpdateAsync(role);
            await _roleRepository.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove(AllRolesCacheKey);
            _cache.Remove($"{RoleByIdCacheKeyPrefix}{roleId}");
            _cache.Remove($"{RoleByNameCacheKeyPrefix}{role.Name.ToLower()}");
            _logger.LogDebug("Cache invalidated for role {RoleId}", roleId);

            _logger.LogInformation("Role {RoleId} updated successfully", roleId);

            var dto = MapToDto(role);
            return Result<RoleDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return Result<RoleDto>.Failure("An error occurred while updating the role.");
        }
    }

    /// <summary>
    /// Deletes a role (protected roles cannot be deleted)
    /// </summary>
    public async Task<Result> DeleteRoleAsync(int roleId)
    {
        try
        {
            _logger.LogInformation("Deleting role {RoleId}", roleId);

            if (roleId <= 0)
            {
                return Result.Failure("Invalid role ID");
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return Result.Failure("Role not found");
            }

            // Check if it's a protected role
            if (ProtectedRoles.Contains(role.Name))
            {
                return Result.Failure($"Cannot delete the protected role '{role.Name}'");
            }

            // Check if role has users
            var usersPaged = await _roleRepository.GetUsersInRoleAsync(roleId, 1, 100);
            if (usersPaged.Items.Any())
            {
                return Result.Failure("Cannot delete role that has users assigned. Remove users from role first.");
            }

            await _roleRepository.DeleteAsync(roleId);
            await _roleRepository.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove(AllRolesCacheKey);
            _cache.Remove($"{RoleByIdCacheKeyPrefix}{roleId}");
            _cache.Remove($"{RoleByNameCacheKeyPrefix}{role.Name.ToLower()}");
            _logger.LogDebug("Cache invalidated for deleted role {RoleId}", roleId);

            _logger.LogInformation("Role {RoleId} deleted successfully", roleId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return Result.Failure("An error occurred while deleting the role.");
        }
    }

    /// <summary>
    /// Gets users in a role by role ID
    /// </summary>
    public async Task<Result<PagedResult<UserSummaryDto>>> GetUsersInRoleAsync(int roleId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving users in role {RoleId}", roleId);

            if (roleId <= 0)
            {
                return Result<PagedResult<UserSummaryDto>>.Failure("Invalid role ID");
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return Result<PagedResult<UserSummaryDto>>.Failure("Role not found");
            }

            var pagedResult = await _roleRepository.GetUsersInRoleAsync(roleId, pageNumber, pageSize);
            var dtos = pagedResult.Items.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username
            }).ToList();
            return Result<PagedResult<UserSummaryDto>>.Success(new PagedResult<UserSummaryDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users in role {RoleId}", roleId);
            return Result<PagedResult<UserSummaryDto>>.Failure("An error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Gets users in a role by role name
    /// </summary>
    public async Task<Result<PagedResult<UserSummaryDto>>> GetUsersInRoleByNameAsync(string roleName, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving users in role {RoleName}", roleName);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return Result<PagedResult<UserSummaryDto>>.Failure("Role name is required");
            }

            var role = await _roleRepository.GetRoleByNameAsync(roleName);
            if (role == null)
            {
                return Result<PagedResult<UserSummaryDto>>.Failure("Role not found");
            }

            var pagedResult = await _roleRepository.GetUsersInRoleAsync(role.Id, pageNumber, pageSize);
            var dtos = pagedResult.Items.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username
            }).ToList();
            return Result<PagedResult<UserSummaryDto>>.Success(new PagedResult<UserSummaryDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users in role {RoleName}", roleName);
            return Result<PagedResult<UserSummaryDto>>.Failure("An error occurred while retrieving users.");
        }
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            UserCount = role.UserRoles?.Count ?? 0
        };
    }
}
