using Xunit;
using FluentAssertions;
using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.Repositories.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Tests.Services;

/// <summary>
/// Integration tests for authentication service
/// Tests full authentication flow end-to-end
/// </summary>
public class AuthenticationServiceIntegrationTests
{
    private const string TestJwtSecret = "test-secret-key-for-unit-testing-minimum-32-characters";

    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuthenticationService _authService;

    public AuthenticationServiceIntegrationTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _passwordHasher = new PasswordHasher();
        _tokenService = new TokenService(TestJwtSecret);
        _authService = new AuthenticationService(
            _mockUserRepository.Object,
            _passwordHasher,
            _tokenService,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesUserAndReturnsToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = "john@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(r => r.GetUserByUsernameAsync(request.Username))
            .ReturnsAsync((User?)null);

        var createdUser = new User
        {
            Id = 1,
            Username = request.Username,
            Email = request.Email,
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(1);
        result.Data.Username.Should().Be("johndoe");
        result.Data.Token.Should().NotBeNullOrEmpty();

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        var existingUser = new User { Id = 1, Email = request.Email };

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Email already registered");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        var user = new User
        {
            Id = 1,
            Username = "johndoe",
            Email = "john@example.com",
            PasswordHash = hashedPassword,
            IsActive = true
        };

        var request = new LoginRequest
        {
            Email = user.Email,
            Password = password
        };

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(1);
        result.Data.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var correctPassword = "SecurePassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(correctPassword);

        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = hashedPassword,
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = wrongPassword };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        var inactiveUser = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = hashedPassword,
            IsActive = false
        };

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(inactiveUser.Email))
            .ReturnsAsync(inactiveUser);

        var request = new LoginRequest { Email = inactiveUser.Email, Password = password };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public async Task GetUserFromToken_WithValidToken_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Username = "johndoe", Email = "john@example.com", IsActive = true };
        var token = _tokenService.GenerateToken(user);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserFromTokenAsync(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task GetUserFromToken_WithInvalidToken_ReturnsFailure()
    {
        // Act
        var result = await _authService.GetUserFromTokenAsync("invalid-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid token");
    }
}
