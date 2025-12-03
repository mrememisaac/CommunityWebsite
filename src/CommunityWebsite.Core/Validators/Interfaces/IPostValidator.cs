using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;

namespace CommunityWebsite.Core.Validators.Interfaces;

/// <summary>
/// Interface for validating posts - Dependency Inversion Principle
/// </summary>
public interface IPostValidator
{
    Task<Result> ValidateCreateRequestAsync(CreatePostRequest request);
    Task<Result> ValidateUpdateRequestAsync(int postId, UpdatePostRequest request);
}
