using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.Specifications;
using CommunityWebsite.Core.Validators.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Validators;

/// <summary>
/// Comment validator implementation - Single Responsibility
/// </summary>
public class CommentValidator : ICommentValidator
{
    private readonly ILogger<CommentValidator> _logger;

    public CommentValidator(ILogger<CommentValidator> logger)
    {
        _logger = logger;
    }

    public Result ValidateComment(Comment comment)
    {
        var specification = new ValidCommentSpecification();

        if (!specification.IsSatisfiedBy(comment))
        {
            _logger.LogWarning("Comment validation failed: {Error}", specification.GetErrorMessage());
            return Result.Failure(specification.GetErrorMessage());
        }

        return Result.Success();
    }
}
