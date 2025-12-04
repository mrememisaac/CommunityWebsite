using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Services;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Unit tests for TokenService - JWT token generation and validation
/// </summary>
public class TokenServiceTests
{
    private const string ValidSecretKey = "ThisIsAValidSecretKeyWith32Characters!";
    private const string ShortSecretKey = "ShortKey";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSecretKey_CreatesInstance()
    {
        // Act
        var tokenService = new TokenService(ValidSecretKey);

        // Assert
        tokenService.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithShortSecretKey_ThrowsArgumentException()
    {
        // Act
        var action = () => new TokenService(ShortSecretKey);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("secretKey");
    }

    [Fact]
    public void Constructor_WithNullSecretKey_ThrowsArgumentException()
    {
        // Act
        var action = () => new TokenService(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ThrowsArgumentException()
    {
        // Act
        var action = () => new TokenService("");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceSecretKey_ThrowsArgumentException()
    {
        // Act
        var action = () => new TokenService("   ");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GenerateToken Tests

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsToken()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var user = CreateSampleUser(1, "testuser");

        // Act
        var token = tokenService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().Contain("."); // JWT tokens have dots separating parts
    }

    [Fact]
    public void GenerateToken_WithNullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);

        // Act
        var action = () => tokenService.GenerateToken(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateToken_GeneratesValidJwtFormat()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var user = CreateSampleUser(1, "testuser");

        // Act
        var token = tokenService.GenerateToken(user);

        // Assert - JWT has 3 parts separated by dots
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_WithDifferentUsers_GeneratesDifferentTokens()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var user1 = CreateSampleUser(1, "user1");
        var user2 = CreateSampleUser(2, "user2");

        // Act
        var token1 = tokenService.GenerateToken(user1);
        var token2 = tokenService.GenerateToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }

    #endregion

    #region GetUserIdFromToken Tests

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var user = CreateSampleUser(42, "testuser");
        var token = tokenService.GenerateToken(user);

        // Act
        var userId = tokenService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(42);
    }

    [Fact]
    public void GetUserIdFromToken_WithNullToken_ReturnsNull()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);

        // Act
        var userId = tokenService.GetUserIdFromToken(null!);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);

        // Act
        var userId = tokenService.GetUserIdFromToken("");

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithWhitespaceToken_ReturnsNull()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);

        // Act
        var userId = tokenService.GetUserIdFromToken("   ");

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);

        // Act - use a malformed token that will cause a parsing exception
        var userId = tokenService.GetUserIdFromToken("malformed-token");

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithWrongSecretKey_ReturnsNull()
    {
        // Arrange
        var tokenService1 = new TokenService(ValidSecretKey);
        var tokenService2 = new TokenService("AnotherValidSecretKeyWith32Chars!");
        var user = CreateSampleUser(1, "testuser");
        var token = tokenService1.GenerateToken(user);

        // Act - try to validate with different secret
        var userId = tokenService2.GetUserIdFromToken(token);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var user = CreateSampleUser(1, "testuser");
        var token = tokenService.GenerateToken(user);
        var tamperedToken = token + "tampered";

        // Act
        var userId = tokenService.GetUserIdFromToken(tamperedToken);

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region Token Expiration Tests

    [Fact]
    public void GenerateToken_WithCustomExpiration_GeneratesToken()
    {
        // Arrange
        var tokenService = new TokenService(
            ValidSecretKey,
            issuer: "TestIssuer",
            audience: "TestAudience",
            expirationMinutes: 120);
        var user = CreateSampleUser(1, "testuser");

        // Act
        var token = tokenService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Verify token is still valid
        var userId = tokenService.GetUserIdFromToken(token);
        userId.Should().Be(1);
    }

    [Fact]
    public void GenerateToken_TokenIsValidImmediatelyAfterGeneration()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey, expirationMinutes: 1);
        var user = CreateSampleUser(1, "testuser");

        // Act
        var token = tokenService.GenerateToken(user);
        var userId = tokenService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(1);
    }

    #endregion

    #region Issuer and Audience Tests

    [Fact]
    public void Constructor_WithCustomIssuerAndAudience_CreatesInstance()
    {
        // Act
        var tokenService = new TokenService(
            ValidSecretKey,
            issuer: "CustomIssuer",
            audience: "CustomAudience");

        // Assert
        tokenService.Should().NotBeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithMismatchedIssuer_ReturnsNull()
    {
        // Arrange
        var tokenService1 = new TokenService(ValidSecretKey, issuer: "Issuer1");
        var tokenService2 = new TokenService(ValidSecretKey, issuer: "Issuer2");
        var user = CreateSampleUser(1, "testuser");
        var token = tokenService1.GenerateToken(user);

        // Act - different issuer should fail validation
        var userId = tokenService2.GetUserIdFromToken(token);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithMismatchedAudience_ReturnsNull()
    {
        // Arrange
        var tokenService1 = new TokenService(ValidSecretKey, audience: "Audience1");
        var tokenService2 = new TokenService(ValidSecretKey, audience: "Audience2");
        var user = CreateSampleUser(1, "testuser");
        var token = tokenService1.GenerateToken(user);

        // Act - different audience should fail validation
        var userId = tokenService2.GetUserIdFromToken(token);

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region Multiple Users Tests

    [Fact]
    public void GenerateToken_ForMultipleUsers_AllTokensAreValid()
    {
        // Arrange
        var tokenService = new TokenService(ValidSecretKey);
        var users = Enumerable.Range(1, 10)
            .Select(i => CreateSampleUser(i, $"user{i}"))
            .ToList();

        // Act & Assert
        foreach (var user in users)
        {
            var token = tokenService.GenerateToken(user);
            var userId = tokenService.GetUserIdFromToken(token);

            userId.Should().Be(user.Id);
        }
    }

    #endregion

    #region Helper Methods

    private static User CreateSampleUser(int id, string username)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hashedpassword",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
