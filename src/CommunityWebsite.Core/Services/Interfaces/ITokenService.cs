using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// JWT Token service interface
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
    int? GetUserIdFromToken(string token);
}
