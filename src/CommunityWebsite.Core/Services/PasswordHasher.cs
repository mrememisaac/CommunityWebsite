using System.Security.Cryptography;
using System.Text;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Password hasher implementation using PBKDF2 with SHA256
/// Provides secure password storage following industry best practices
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 310000; // OWASP 2023 recommendation for PBKDF2-SHA256

    /// <summary>
    /// Hash a password using PBKDF2-SHA256
    /// Returns format: "salt:hash" both base64 encoded
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password), "Password cannot be empty");

        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(HashSize);

            // Combine salt and hash
            byte[] combined = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, combined, 0, SaltSize);
            Array.Copy(hash, 0, combined, SaltSize, HashSize);

            // Return as base64 string
            return Convert.ToBase64String(combined);
        }
    }

    /// <summary>
    /// Verify a password against its hash
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            // Convert base64 back to bytes
            byte[] combined = Convert.FromBase64String(hash);

            // Extract salt and stored hash
            byte[] salt = new byte[SaltSize];
            Array.Copy(combined, 0, salt, 0, SaltSize);

            byte[] storedHash = new byte[HashSize];
            Array.Copy(combined, SaltSize, storedHash, 0, HashSize);

            // Compute hash of provided password
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                byte[] computedHash = pbkdf2.GetBytes(HashSize);

                // Compare hashes using constant-time comparison to prevent timing attacks
                return ConstantTimeComparison(storedHash, computedHash);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Constant-time comparison to prevent timing attacks
    /// </summary>
    private static bool ConstantTimeComparison(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
