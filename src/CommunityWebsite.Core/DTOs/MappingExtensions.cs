using CommunityWebsite.Core.Constants;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.DTOs;

/// <summary>
/// Extension methods for mapping domain models to DTOs
/// Centralizes DTO mapping logic to avoid duplication across services and controllers
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a Post entity to PostSummaryDto
    /// </summary>
    public static PostSummaryDto ToSummaryDto(this Post post)
    {
        return new PostSummaryDto
        {
            Id = post.Id,
            Title = post.Title,
            Preview = post.Content.TruncateContent(PaginationDefaults.ContentPreviewLength),
            Author = post.Author?.ToSummaryDto(),
            Category = post.Category,
            CreatedAt = post.CreatedAt,
            ViewCount = post.ViewCount,
            CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0
        };
    }

    /// <summary>
    /// Maps a User entity to UserSummaryDto
    /// </summary>
    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            Username = user.Username
        };
    }

    /// <summary>
    /// Maps a Comment entity to CommentDto
    /// </summary>
    public static CommentDto ToCommentDto(this Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            Author = comment.Author?.ToSummaryDto(),
            CreatedAt = comment.CreatedAt,
            ReplyCount = comment.Replies?.Count(r => !r.IsDeleted) ?? 0
        };
    }

    /// <summary>
    /// Truncates content to specified length with ellipsis
    /// </summary>
    private static string TruncateContent(this string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        if (content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }
}
