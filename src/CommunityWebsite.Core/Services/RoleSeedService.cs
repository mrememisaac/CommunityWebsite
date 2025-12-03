using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Implementation of role seeding
/// </summary>
public class RoleSeedService : IRoleSeedService
{
    private readonly CommunityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RoleSeedService> _logger;

    public RoleSeedService(
        CommunityDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<RoleSeedService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seed default roles: Admin, Moderator, User
    /// </summary>
    public async Task SeedDefaultRolesAsync()
    {
        var roles = new[]
        {
            new Role { Name = "Admin", Description = "Administrator with full system access" },
            new Role { Name = "Moderator", Description = "Content moderator with post deletion rights" },
            new Role { Name = "User", Description = "Regular community user" }
        };

        foreach (var role in roles)
        {
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role.Name);
            if (existingRole == null)
            {
                _context.Roles.Add(role);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed a default admin user from configuration
    /// Admin credentials should be set via environment variables or secure configuration
    /// </summary>
    public async Task SeedAdminUserAsync()
    {
        var adminEmail = _configuration["Admin:Email"];
        var adminPassword = _configuration["Admin:Password"];
        var adminUsername = _configuration["Admin:Username"] ?? "admin";

        // Skip seeding if admin credentials are not configured
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogWarning(
                "Admin user not seeded: Admin:Email and Admin:Password must be configured via environment variables or secure configuration");
            return;
        }

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                PasswordHash = _passwordHasher.HashPassword(adminPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // Assign Admin role
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Admin user '{Username}' seeded successfully", adminUsername);
        }
    }
}
