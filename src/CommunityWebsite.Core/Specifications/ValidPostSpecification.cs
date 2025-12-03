using CommunityWebsite.Core.DTOs.Requests;

namespace CommunityWebsite.Core.Specifications;

/// <summary>
/// Validates post creation requests
/// Single responsibility: Post validation only
/// </summary>
public class ValidPostSpecification : ISpecification<CreatePostRequest>
{
    private readonly List<string> _errors = new();

    public bool IsSatisfiedBy(CreatePostRequest request)
    {
        _errors.Clear();

        if (string.IsNullOrWhiteSpace(request.Title))
            _errors.Add("Post title is required and cannot be empty.");

        if (request.Title?.Length > 300)
            _errors.Add("Post title cannot exceed 300 characters.");

        if (string.IsNullOrWhiteSpace(request.Content))
            _errors.Add("Post content is required and cannot be empty.");

        if (request.Content?.Length < 10)
            _errors.Add("Post content must be at least 10 characters long.");

        if (request.AuthorId <= 0)
            _errors.Add("Valid author ID is required.");

        return _errors.Count == 0;
    }

    public string GetErrorMessage() => string.Join(" ", _errors);
}
