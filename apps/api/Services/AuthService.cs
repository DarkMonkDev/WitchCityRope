using Microsoft.AspNetCore.Identity;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;
using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Authentication service implementation using ASP.NET Core Identity
/// For authentication vertical slice test - throwaway implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Register new user account with validation
    /// </summary>
    public async Task<(bool Success, AuthUserResponse? User, string ErrorMessage)> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return (false, null, "Email address is already registered");
            }

            // Check if scene name already exists (custom validation)
            var existingSceneName = await _userManager.Users
                .AnyAsync(u => u.SceneName == registerDto.SceneName);
            if (existingSceneName)
            {
                return (false, null, "Scene name is already taken");
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                SceneName = registerDto.SceneName,
                EmailConfirmed = true, // Auto-confirm for testing
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", registerDto.Email, errors);
                return (false, null, errors);
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User registered successfully: {Email} ({SceneName})", user.Email, user.SceneName);

            return (true, new AuthUserResponse(user), string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", registerDto.Email);
            return (false, null, "Registration could not be completed at this time");
        }
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    public async Task<(bool Success, WitchCityRope.Api.Models.Auth.LoginResponse? Response, string ErrorMessage)> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                return (false, null, "Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate JWT token
                var jwtToken = _jwtService.GenerateToken(user);
                var response = new WitchCityRope.Api.Models.Auth.LoginResponse
                {
                    Token = jwtToken.Token,
                    ExpiresAt = jwtToken.ExpiresAt,
                    User = new AuthUserResponse(user)
                };

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);
                return (true, response, string.Empty);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out for user: {Email}", loginDto.Email);
                return (false, null, "Account is temporarily locked due to failed login attempts");
            }

            _logger.LogWarning("Invalid login attempt for {Email}", loginDto.Email);
            return (false, null, "Invalid email or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", loginDto.Email);
            return (false, null, "Login could not be completed at this time");
        }
    }

    /// <summary>
    /// Generate service token for existing authenticated user
    /// Used for service-to-service authentication bridge
    /// </summary>
    public async Task<(bool Success, WitchCityRope.Api.Models.Auth.LoginResponse? Response, string ErrorMessage)> GetServiceTokenAsync(string userId, string email)
    {
        try
        {
            // Validate user exists and email matches
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Service token request failed - user not found or email mismatch: {UserId}, {Email}", userId, email);
                return (false, null, "User not found or email mismatch");
            }

            // Validate user is active
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Service token request failed - user email not confirmed: {UserId}", userId);
                return (false, null, "Account is not active or email not verified");
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);
            var response = new WitchCityRope.Api.Models.Auth.LoginResponse
            {
                Token = jwtToken.Token,
                ExpiresAt = jwtToken.ExpiresAt,
                User = new AuthUserResponse(user)
            };

            _logger.LogDebug("Service token generated for user: {UserId}", userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service token generation failed for {UserId}", userId);
            return (false, null, "Service token could not be generated at this time");
        }
    }

    /// <summary>
    /// Get user by ID for user info endpoint
    /// </summary>
    public async Task<AuthUserResponse?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? new AuthUserResponse(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID: {UserId}", userId);
            return null;
        }
    }
}

/// <summary>
/// Extension method to add AnyAsync to IQueryable for LINQ compatibility
/// </summary>
public static class QueryableExtensions
{
    public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(source, predicate);
    }
}