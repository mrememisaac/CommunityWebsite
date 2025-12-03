namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Service interface for seeding default roles and admin user into the database
/// </summary>
public interface IRoleSeedService
{
    Task SeedDefaultRolesAsync();
    Task SeedAdminUserAsync();
}
