using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Specifications;

/// <summary>
/// Validates comment content
/// </summary>
public class ValidCommentSpecification : ISpecification<Comment>
{
    private readonly List<string> _errors = new();

    public bool IsSatisfiedBy(Comment comment)
    {
        _errors.Clear();

        if (string.IsNullOrWhiteSpace(comment.Content))
            _errors.Add("Comment content is required.");

        if (comment.Content?.Length < 1)
            _errors.Add("Comment must contain at least one character.");

        if (comment.Content?.Length > 5000)
            _errors.Add("Comment cannot exceed 5000 characters.");

        if (comment.AuthorId <= 0)
            _errors.Add("Valid author ID is required.");

        if (comment.PostId <= 0)
            _errors.Add("Valid post ID is required.");

        return _errors.Count == 0;
    }

    public string GetErrorMessage() => string.Join(" ", _errors);
}
