namespace CommunityWebsite.Core.Common;

/// <summary>
/// Non-generic result for operations that don't return data
/// </summary>
public class Result
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, ErrorMessage = error, Errors = new List<string> { error } };
    public static Result Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
