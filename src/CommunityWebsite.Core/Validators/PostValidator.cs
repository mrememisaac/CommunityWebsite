using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Specifications;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Validators;

/// <summary>
/// Post validator implementation - Single Responsibility
/// Handles all post validation logic
/// </summary>
public class PostValidator : IPostValidator
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PostValidator> _logger;

    public PostValidator(IUserRepository userRepository, ILogger<PostValidator> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> ValidateCreateRequestAsync(CreatePostRequest request)
    {
        _logger.LogInformation("Validating post creation request for user {UserId}", request.AuthorId);

        // Apply specification
        var specification = new ValidPostSpecification();
        if (!specification.IsSatisfiedBy(request))
        {
            _logger.LogWarning("Post validation failed: {Error}", specification.GetErrorMessage());
            return Result.Failure(specification.GetErrorMessage());
        }

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.AuthorId);
        if (user == null)
        {
            var error = $"User with ID {request.AuthorId} not found.";
            _logger.LogWarning(error);
            return Result.Failure(error);
        }

        // Verify user is active
        if (!user.IsActive)
        {
            var error = "Cannot create post: user account is inactive.";
            _logger.LogWarning(error);
            return Result.Failure(error);
        }

        return Result.Success();
    }

    public async Task<Result> ValidateUpdateRequestAsync(int postId, UpdatePostRequest request)
    {
        _logger.LogInformation("Validating post update for post {PostId}", postId);

        if (postId <= 0)
            return Result.Failure("Invalid post ID.");

        return Result.Success();
    }
}
