using CommunityWebsite.Core.Common;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunityWebsite.Core.Services;

/// <summary>
/// Authentication service implementation
/// Handles user registration and login with proper validation
/// Implements Dependency Inversion and Single Responsibility principles
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register new user with validation
    /// </summary>
    public async Task<Result<AuthenticationResponse>> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
            return Result<AuthenticationResponse>.Failure("Request cannot be null");

        try
        {
            // Validate request
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Username))
                validationErrors.Add("Username is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                validationErrors.Add("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                validationErrors.Add("Password is required");

            if (request.Password != request.ConfirmPassword)
                validationErrors.Add("Passwords do not match");

            if (request.Password?.Length < 8)
                validationErrors.Add("Password must be at least 8 characters");

            if (validationErrors.Any())
            {
                _logger.LogWarning("Registration validation failed: {Errors}", string.Join(", ", validationErrors));
                return Result<AuthenticationResponse>.Failure(validationErrors);
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return Result<AuthenticationResponse>.Failure("Email already registered");
            }

            var existingUsername = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (existingUsername != null)
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
                return Result<AuthenticationResponse>.Failure("Username already taken");
            }

            // Create new user
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password ?? string.Empty),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully", request.Username);

            // Generate token
            var token = _tokenService.GenerateToken(createdUser);

            return Result<AuthenticationResponse>.Success(new AuthenticationResponse
            {
                UserId = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username}", request.Username);
            return Result<AuthenticationResponse>.Failure("An error occurred during registration");
        }
    }

    /// <summary>
    /// Login user with email and password
    /// </summary>
    public async Task<Result<AuthenticationResponse>> LoginAsync(LoginRequest request)
    {
        if (request == null)
            return Result<AuthenticationResponse>.Failure("Request cannot be null");

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Email))
                return Result<AuthenticationResponse>.Failure("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result<AuthenticationResponse>.Failure("Password is required");

            // Find user by email
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Email} not found", request.Email);
                return Result<AuthenticationResponse>.Failure("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User {Email} is not active", request.Email);
                return Result<AuthenticationResponse>.Failure("User account is not active");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {Email}", request.Email);
                return Result<AuthenticationResponse>.Failure("Invalid email or password");
            }

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            // Generate token
            var token = _tokenService.GenerateToken(user);

            return Result<AuthenticationResponse>.Success(new AuthenticationResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user");
            return Result<AuthenticationResponse>.Failure("An error occurred during login");
        }
    }

    /// <summary>
    /// Get user from JWT token
    /// </summary>
    public async Task<Result<User?>> GetUserFromTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<User?>.Failure("Token is required");

        try
        {
            var userId = _tokenService.GetUserIdFromToken(token);
            if (!userId.HasValue)
                return Result<User?>.Failure("Invalid or expired token");

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
                return Result<User?>.Failure("User not found");

            return Result<User?>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user from token");
            return Result<User?>.Failure("Invalid token");
        }
    }

    /// <summary>
    /// Generate token for user (public method for token renewal)
    /// </summary>
    public string GenerateToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return _tokenService.GenerateToken(user);
    }
}
