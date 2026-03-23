using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Authentication service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IReturnUrlValidator _returnUrlValidator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IReturnUrlValidator returnUrlValidator,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _returnUrlValidator = returnUrlValidator;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get current user by ID - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, AuthUserResponse? Response, string Error)> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework query for user lookup
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return (false, null, "User not found");
            }

            var response = new AuthUserResponse(user);

            _logger.LogDebug("Current user retrieved successfully: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user: {UserId}", userId);
            return (false, null, "Failed to retrieve user information");
        }
    }

    /// <summary>
    /// Authenticate user with email and password with optional return URL validation
    /// Direct Entity Framework and Identity service calls - NO MediatR complexity
    /// Implements OWASP-compliant return URL validation per BR-1 and SEC-1 requirements
    /// </summary>
    /// <param name="request">Login request containing email, password, and optional return URL</param>
    /// <param name="httpContext">HTTP context for return URL validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with validated return URL (defaults to /dashboard if validation fails)</returns>
    public async Task<(bool Success, LoginResponse? Response, string Error)> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate required fields (belt-and-suspenders with DataAnnotations)
            if (string.IsNullOrWhiteSpace(request.EmailOrSceneName))
            {
                return (false, null, "Email or Scene Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return (false, null, "Password is required");
            }

            // Try to find user by email first (most common case, indexed)
            var user = await _userManager.FindByEmailAsync(request.EmailOrSceneName);

            // If not found by email, try by scene name (case-insensitive)
            if (user == null)
            {
                // Check if scene name has duplicates
                var sceneNameCount = await _context.Users
                    .CountAsync(u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName), cancellationToken);

                if (sceneNameCount > 1)
                {
                    _logger.LogInformation("Login attempt with duplicate scene name: {SceneName}", request.EmailOrSceneName);
                    return (false, null, "This scene name is used by multiple members. Please log in with your email address instead.");
                }

                if (sceneNameCount == 1)
                {
                    // If unique, proceed with scene name login
                    user = await _context.Users.FirstOrDefaultAsync(
                        u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName),
                        cancellationToken);
                }
            }

            if (user == null)
            {
                _logger.LogWarning("Login failed. Reason={Reason}, Identifier={Identifier}", "UserNotFound", request.EmailOrSceneName);
                return (false, null, "Invalid email/scene name or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            // Phase 2: Check email confirmation after password validation
            if (result.Succeeded && !user.EmailConfirmed)
            {
                _logger.LogWarning("Login failed. Reason={Reason}, Identifier={Identifier}", "EmailNotVerified", user.Email);
                return (false, null, "Please verify your email address before logging in. Check your inbox for the verification link.");
            }

            if (result.Succeeded)
            {
                // Update last login time using direct Entity Framework
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                // Validate return URL if provided (OWASP-compliant security validation)
                string? validatedReturnUrl = null;
                if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
                {
                    var validationResult = _returnUrlValidator.ValidateReturnUrl(
                        request.ReturnUrl,
                        httpContext,
                        user.Id.ToString());

                    if (validationResult.IsValid)
                    {
                        validatedReturnUrl = validationResult.ValidatedUrl;
                        _logger.LogInformation(
                            "Return URL validated for user {UserId}: {OriginalUrl} -> {ValidatedUrl}",
                            user.Id, validationResult.OriginalUrl, validatedReturnUrl);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Return URL validation failed for user {UserId}: {OriginalUrl}, Reason: {Reason}",
                            user.Id, validationResult.OriginalUrl, validationResult.FailureReason);
                        // validatedReturnUrl remains null - frontend will default to /dashboard
                    }
                }

                // Generate JWT token
                var jwtToken = _jwtService.GenerateToken(user);

                // Generate database-backed refresh token for persistent login
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                    user.Id, request.RememberMe, userAgent, cancellationToken);

                var response = new LoginResponse
                {
                    Token = jwtToken.Token,
                    ExpiresAt = jwtToken.ExpiresAt,
                    User = new AuthUserResponse(user),
                    ReturnUrl = validatedReturnUrl, // Null if not provided or validation failed
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                    RememberMe = request.RememberMe
                };

                _logger.LogInformation("User logged in successfully. Email={Email}, Method={Method}, UserId={UserId}", user.Email, "Password", user.Id);
                return (true, response, string.Empty);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed. Reason={Reason}, Identifier={Identifier}", "AccountLockedOut", request.EmailOrSceneName);
                return (false, null, "Account is temporarily locked due to failed login attempts");
            }

            _logger.LogWarning("Login failed. Reason={Reason}, Identifier={Identifier}", "InvalidPassword", request.EmailOrSceneName);
            return (false, null, "Invalid email/scene name or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Identifier}", request.EmailOrSceneName);
            return (false, null, "Login could not be completed at this time");
        }
    }

    /// <summary>
    /// Register new user account with validation
    /// Direct Entity Framework and Identity operations - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, AuthUserResponse? Response, string Error)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL: Validate Terms of Service acceptance
            if (!request.TermsOfServiceAccepted)
            {
                return (false, null, "You must accept the Terms of Service to register");
            }

            // Check if email already exists using direct Entity Framework
            // IMPORTANT: Use case-insensitive comparison to match ASP.NET Identity behavior.
            // PostgreSQL string comparison is case-sensitive by default, so without ToLower()
            // a user registering with "User@Email.com" would bypass this check if "user@email.com"
            // already exists, causing Identity's CreateAsync to return a confusing
            // "User name already taken" error instead of our friendly message.
            var normalizedEmail = request.Email.ToLower();
            var existingUser = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email!.ToLower() == normalizedEmail, cancellationToken);

            if (existingUser)
            {
                return (false, null, "Email address is already registered");
            }

            // Scene names can now be duplicated - only email must be unique

            // Create new user with Terms of Service acceptance
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                SceneName = request.SceneName,
                EmailConfirmed = false, // Phase 2: Email verification required
                TermsOfServiceAccepted = true,
                TermsOfServiceAcceptedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
                return (false, null, errors);
            }

            // Phase 2: Generate and send email verification
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "https://staging.witchcityrope.com";
            var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&token={System.Web.HttpUtility.UrlEncode(token)}";

            // Send verification email using EmailService with Admin/EmailVerification template
            var emailVariables = new Dictionary<string, string>
            {
                { "user_name", user.SceneName ?? user.Email },
                { "verification_url", verificationUrl }
            };

            var emailResult = await _emailService.SendTemplatedEmailAsync(
                toEmail: user.Email!,
                toName: user.SceneName ?? user.Email,
                category: EmailCategory.Admin,
                templateType: "EmailVerification",
                variables: emailVariables,
                cancellationToken: cancellationToken);

            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send verification email for {Email}: {Error}",
                    user.Email, emailResult.Error);
                // Don't fail registration if email fails - user can resend
            }
            else
            {
                _logger.LogInformation("Verification email sent successfully to {Email}", user.Email);
            }

            var response = new AuthUserResponse(user);

            _logger.LogInformation("User registered successfully: {Email} ({SceneName}) with ToS acceptance at {ToSTime} - Email verification required",
                user.Email, user.SceneName, user.TermsOfServiceAcceptedAt);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", request.Email);
            return (false, null, "Registration could not be completed at this time");
        }
    }

    /// <summary>
    /// Generate service token for existing authenticated user
    /// Used for service-to-service authentication bridge - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, LoginResponse? Response, string Error)> GetServiceTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user exists and email matches using direct Entity Framework
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId && u.Email == email, cancellationToken);

            if (user == null)
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
            var response = new LoginResponse
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
    /// Generate email verification token for user - Phase 2: Email Verification
    /// </summary>
    public async Task<(bool Success, string Token, string Error)> GenerateEmailVerificationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Email verification token generation failed - user not found: {UserId}", userId);
                return (false, string.Empty, "User not found");
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email verification token requested for already verified user: {UserId}", userId);
                return (false, string.Empty, "Email already verified");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogInformation("Email verification token generated for user: {UserId}", userId);

            return (true, token, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification token generation failed for {UserId}", userId);
            return (false, string.Empty, "Failed to generate verification token");
        }
    }

    /// <summary>
    /// Verify user email with token - Phase 2: Email Verification
    /// Uses userId for security and stability (email can change)
    /// </summary>
    public async Task<(bool Success, string Error)> VerifyEmailAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return (false, "User ID and token are required");
            }

            // Parse userId to Guid
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Email verification failed - invalid user ID format: {UserId}", userId);
                return (false, "Invalid verification link");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email verification failed - user not found: {UserId}", userId);
                return (false, "Invalid verification link");
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email verification attempted for already verified user: {Email}", user.Email);
                return (true, string.Empty); // Return success, email already verified
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verified successfully for user: {UserId}, Email: {Email}", userId, user.Email);
                return (true, string.Empty);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Email verification failed for user {UserId}: {Errors}", userId, errors);

            // Check if token is expired/invalid
            if (errors.Contains("Invalid token") || errors.Contains("invalid"))
            {
                return (false, "This verification link is invalid or has expired. Please request a new verification email.");
            }

            return (false, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for user {UserId}", userId);
            return (false, "Email verification could not be completed at this time");
        }
    }

    /// <summary>
    /// Resend email verification email - Phase 2: Email Verification
    /// Rate limited to 3 requests per 15 minutes per email
    /// </summary>
    public async Task<(bool Success, string Error)> ResendVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email is required");
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(email);

            // SECURITY: Always return success to prevent email enumeration
            // Only send email if user exists and email not verified
            if (user == null)
            {
                _logger.LogInformation("Verification resend requested for non-existent email (security: returning success): {Email}", email);
                return (true, string.Empty);
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Verification resend requested for already verified email: {Email}", email);
                return (true, string.Empty); // Return success, no action needed
            }

            // Generate new verification token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "https://staging.witchcityrope.com";
            var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&token={System.Web.HttpUtility.UrlEncode(token)}";

            // Send verification email using EmailService
            var emailVariables = new Dictionary<string, string>
            {
                { "user_name", user.SceneName ?? user.Email },
                { "verification_url", verificationUrl }
            };

            var emailResult = await _emailService.SendTemplatedEmailAsync(
                toEmail: user.Email!,
                toName: user.SceneName ?? user.Email,
                category: EmailCategory.Admin,
                templateType: "EmailVerification",
                variables: emailVariables,
                cancellationToken: cancellationToken);

            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to resend verification email for {Email}: {Error}",
                    user.Email, emailResult.Error);
                // SECURITY: Still return success to prevent enumeration
            }
            else
            {
                _logger.LogInformation("Verification email resent successfully to {Email}", user.Email);
            }

            return (true, string.Empty); // Always return success for security
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification email failed for {Email}", email);
            return (true, string.Empty); // SECURITY: Return success even on error
        }
    }

    /// <summary>
    /// Initiate password reset process - Phase 3: Password Reset
    /// Generates reset token and sends email with reset link
    /// Always returns success to prevent email enumeration
    /// </summary>
    public async Task<(bool Success, string Error)> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email is required");
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(email);

            // SECURITY: Always return success to prevent email enumeration
            // Only send email if user exists
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for non-existent email (security: returning success): {Email}", email);
                return (true, string.Empty);
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "https://staging.witchcityrope.com";
            var resetUrl = $"{frontendUrl}/reset-password?userId={user.Id}&token={System.Web.HttpUtility.UrlEncode(token)}";

            // Diagnostic logging: capture token fingerprint at generation time so we can
            // compare against what arrives at the reset-password endpoint. Uses SHA256 hash
            // prefix (not the actual token) to avoid exposing secrets in logs.
            // Property names avoid "token"/"key"/"secret" to prevent Serilog sensitive data masking.
            var urlEncodedToken = System.Web.HttpUtility.UrlEncode(token);
            _logger.LogInformation(
                "Password reset credential generated for user {UserId}. " +
                "ResetCredentialLength={ResetCredentialLength}, " +
                "ResetCredentialDigest={ResetCredentialDigest}, " +
                "EncodedCredentialLength={EncodedCredentialLength}, " +
                "StampDigest={StampDigest}",
                user.Id,
                token.Length,
                ComputeDigestPrefix(token),
                urlEncodedToken.Length,
                ComputeDigestPrefix(user.SecurityStamp ?? ""));

            // Send password reset email using EmailService with Admin/PasswordReset template
            var emailVariables = new Dictionary<string, string>
            {
                { "user_name", user.SceneName ?? user.Email },
                { "reset_url", resetUrl }
            };

            var emailResult = await _emailService.SendTemplatedEmailAsync(
                toEmail: user.Email!,
                toName: user.SceneName ?? user.Email,
                category: EmailCategory.Admin,
                templateType: "PasswordReset",
                variables: emailVariables,
                cancellationToken: cancellationToken);

            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send password reset email for {Email}: {Error}",
                    user.Email, emailResult.Error);
                // SECURITY: Still return success to prevent enumeration
            }
            else
            {
                _logger.LogInformation("Password reset email sent successfully to {Email}", user.Email);
            }

            return (true, string.Empty); // Always return success for security
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for {Email}", email);
            return (true, string.Empty); // SECURITY: Return success even on error
        }
    }

    /// <summary>
    /// Reset password with token - Phase 3: Password Reset
    /// Uses userId for security and stability (email can change)
    /// </summary>
    public async Task<(bool Success, string Error)> ResetPasswordAsync(
        string userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "User ID, token, and new password are required");
            }

            // Parse userId to Guid
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Password reset failed - invalid user ID format: {UserId}", userId);
                return (false, "Invalid password reset link");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed - user not found: {UserId}", userId);
                return (false, "Invalid password reset link");
            }

            // Diagnostic logging: capture incoming token fingerprint so we can compare against
            // what was generated in ForgotPasswordAsync. Detects truncation (length mismatch),
            // corruption (digest mismatch), or SecurityStamp rotation (stamp digest mismatch).
            // "CfDJ8" is the standard ASP.NET Identity Data Protection token prefix.
            _logger.LogInformation(
                "Password reset attempt for user {UserId}. " +
                "ResetCredentialLength={ResetCredentialLength}, " +
                "ResetCredentialDigest={ResetCredentialDigest}, " +
                "HasExpectedPrefix={HasExpectedPrefix}, " +
                "StampDigest={StampDigest}",
                userId,
                token.Length,
                ComputeDigestPrefix(token),
                token.StartsWith("CfDJ8"),
                ComputeDigestPrefix(user.SecurityStamp ?? ""));

            // Reset the password using ASP.NET Identity
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                // SECURITY: If user can reset password via email, they've proven email ownership
                // Automatically verify email on successful password reset
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Email automatically verified for user {UserId} via password reset", userId);
                }

                _logger.LogInformation("Password reset successfully for user: {UserId}, Email: {Email}", userId, user.Email);
                return (true, string.Empty);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning(
                "Password reset FAILED for user {UserId}: {Errors}. " +
                "ResetCredentialLength={ResetCredentialLength}, " +
                "ResetCredentialDigest={ResetCredentialDigest}, " +
                "HasExpectedPrefix={HasExpectedPrefix}, " +
                "StampDigest={StampDigest}",
                userId,
                errors,
                token.Length,
                ComputeDigestPrefix(token),
                token.StartsWith("CfDJ8"),
                ComputeDigestPrefix(user.SecurityStamp ?? ""));

            // Check if token is expired/invalid
            if (errors.Contains("Invalid token") || errors.Contains("invalid"))
            {
                return (false, "This password reset link is invalid or has expired. Please request a new password reset.");
            }

            return (false, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for user {UserId}", userId);
            return (false, "Password reset could not be completed at this time");
        }
    }

    /// <summary>
    /// Compute a truncated SHA256 hash prefix for diagnostic logging.
    /// Returns only the first 12 hex characters (48 bits) — sufficient for correlating
    /// generated vs. received tokens in logs, but not reversible to the original value.
    /// Follows the same pattern as PayPalCheckoutController.ComputeSha256Hash.
    /// </summary>
    private static string ComputeDigestPrefix(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var fullHex = Convert.ToHexStringLower(hashBytes);
        return fullHex[..12];
    }
}
