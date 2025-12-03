using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Specifications;

/// <summary>
/// Validates user credentials
/// </summary>
public class ValidUserSpecification : ISpecification<User>
{
    private readonly List<string> _errors = new();

    public bool IsSatisfiedBy(User user)
    {
        _errors.Clear();

        if (string.IsNullOrWhiteSpace(user.Username))
            _errors.Add("Username is required.");

        if (user.Username?.Length > 100)
            _errors.Add("Username cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(user.Email))
            _errors.Add("Email is required.");

        if (!IsValidEmail(user.Email))
            _errors.Add("Email format is invalid.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            _errors.Add("Password hash is required.");

        return _errors.Count == 0;
    }

    public string GetErrorMessage() => string.Join(" ", _errors);

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
