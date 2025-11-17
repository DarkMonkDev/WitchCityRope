using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Authentication service interface for dependency injection and testing
/// Enables unit testing of AuthenticationEndpoints following Pattern B (interface-based DI)
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Get current user by ID - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    Task<(bool Success, AuthUserResponse? Response, string Error)> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticate user with email and password with optional return URL validation
    /// Direct Entity Framework and Identity service calls - NO MediatR complexity
    /// Implements OWASP-compliant return URL validation per BR-1 and SEC-1 requirements
    /// </summary>
    /// <param name="request">Login request containing email, password, and optional return URL</param>
    /// <param name="httpContext">HTTP context for return URL validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with validated return URL (defaults to /dashboard if validation fails)</returns>
    Task<(bool Success, LoginResponse? Response, string Error)> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Register new user account with validation
    /// Direct Entity Framework and Identity operations - NO MediatR complexity
    /// </summary>
    Task<(bool Success, AuthUserResponse? Response, string Error)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate service token for existing authenticated user
    /// Used for service-to-service authentication bridge - Direct Entity Framework access
    /// </summary>
    Task<(bool Success, LoginResponse? Response, string Error)> GetServiceTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate email verification token for user - Phase 2: Email Verification
    /// </summary>
    Task<(bool Success, string Token, string Error)> GenerateEmailVerificationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify user email with token - Phase 2: Email Verification
    /// Uses userId for security and stability (email can change)
    /// </summary>
    Task<(bool Success, string Error)> VerifyEmailAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend email verification email - Phase 2: Email Verification
    /// </summary>
    Task<(bool Success, string Error)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiate password reset process - Phase 3: Password Reset
    /// Generates reset token and sends email with reset link
    /// </summary>
    Task<(bool Success, string Error)> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password with token - Phase 3: Password Reset
    /// Uses userId for security and stability (email can change)
    /// </summary>
    Task<(bool Success, string Error)> ResetPasswordAsync(
        string userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
