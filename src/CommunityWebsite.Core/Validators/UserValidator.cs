using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.Specifications;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Validators;

/// <summary>
/// User validator implementation - Single Responsibility
/// </summary>
public class UserValidator : IUserValidator
{
    private readonly ILogger<UserValidator> _logger;

    public UserValidator(ILogger<UserValidator> logger)
    {
        _logger = logger;
    }

    public Result ValidateUser(User user)
    {
        var specification = new ValidUserSpecification();

        if (!specification.IsSatisfiedBy(user))
        {
            _logger.LogWarning("User validation failed: {Error}", specification.GetErrorMessage());
            return Result.Failure(specification.GetErrorMessage());
        }

        return Result.Success();
    }
}
