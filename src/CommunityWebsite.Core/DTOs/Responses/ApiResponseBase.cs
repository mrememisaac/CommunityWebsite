namespace CommunityWebsite.Core.DTOs.Responses
{
    public class ApiResponseBase<T>
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public T? Data { get; set; }
    }
}
