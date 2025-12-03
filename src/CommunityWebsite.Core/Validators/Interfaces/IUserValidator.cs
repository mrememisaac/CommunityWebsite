using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Common;

namespace CommunityWebsite.Core.Validators.Interfaces;

/// <summary>
/// Interface for validating users - Dependency Inversion Principle
/// </summary>
public interface IUserValidator
{
    Result ValidateUser(User user);
}
