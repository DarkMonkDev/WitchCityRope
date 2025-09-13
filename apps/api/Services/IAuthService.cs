using WitchCityRope.Api.Models.Auth;
using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Authentication service interface for user management
/// For authentication vertical slice test
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register new user account
    /// </summary>
    /// <param name="registerDto">Registration data</param>
    /// <returns>Created user or error</returns>
    Task<(bool Success, AuthUserResponse? User, string ErrorMessage)> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>User data and JWT token or error</returns>
    Task<(bool Success, WitchCityRope.Api.Models.Auth.LoginResponse? Response, string ErrorMessage)> LoginAsync(LoginDto loginDto);

    /// <summary>
    /// Get JWT token for existing user (service-to-service authentication)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email for verification</param>
    /// <returns>JWT token response or error</returns>
    Task<(bool Success, WitchCityRope.Api.Models.Auth.LoginResponse? Response, string ErrorMessage)> GetServiceTokenAsync(string userId, string email);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User data or null</returns>
    Task<AuthUserResponse?> GetUserByIdAsync(string userId);
}