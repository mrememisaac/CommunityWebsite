namespace CommunityWebsite.Core.Common;

/// <summary>
/// Generic result wrapper following Railway-Oriented Programming pattern
/// Provides better error handling than throwing exceptions
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error, Errors = new List<string> { error } };
    public static Result<T> Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
