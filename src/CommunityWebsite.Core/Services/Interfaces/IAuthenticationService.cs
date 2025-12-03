using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;

namespace CommunityWebsite.Core.Services.Interfaces;

/// <summary>
/// Authentication service interface - Dependency Inversion Principle
/// </summary>
public interface IAuthenticationService
{
    Task<Result<AuthenticationResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthenticationResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<User?>> GetUserFromTokenAsync(string token);
    string GenerateToken(User user);
}
