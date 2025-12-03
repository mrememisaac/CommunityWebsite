using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Common;

namespace CommunityWebsite.Core.Validators.Interfaces;

/// <summary>
/// Interface for comment validation - Dependency Inversion Principle
/// </summary>
public interface ICommentValidator
{
    Result ValidateComment(Comment comment);
}
