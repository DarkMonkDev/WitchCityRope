using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Authentication.Endpoints;

/// <summary>
/// Authentication minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Register authentication endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        // Get current authenticated user information
        app.MapGet("/api/auth/current-user", async (
            IAuthenticationService authService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Extract user ID from JWT token claims
                var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Problem(
                        title: "Invalid Token",
                        detail: "User ID not found in token claims",
                        statusCode: 401);
                }

                var (success, response, error) = await authService.GetCurrentUserAsync(userId, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Current User Failed",
                        detail: error,
                        statusCode: response == null ? 404 : 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user information")
            .WithDescription("Returns the current user's profile information based on JWT token")
            .WithTags("Authentication")
            .Produces<AuthUserResponse>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // User login endpoint with httpOnly cookie support and return URL validation
        // SECURITY: Login is PUBLIC and should NOT require CSRF token (user doesn't have one yet)
        // CSRF protection will be required AFTER authentication for state-changing operations
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IAuthenticationService authService,
            HttpContext context,
            IConfiguration configuration,
            IAntiforgery antiforgery,
            CancellationToken cancellationToken) =>
            {
                // Pass HttpContext to service for return URL validation
                var (success, response, error) = await authService.LoginAsync(request, context, cancellationToken);

                if (success && response != null)
                {
                    // Set access token as session cookie (NO Expires - cleared when browser closes)
                    // JWT has its own 15-minute expiration enforced server-side
                    var accessCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                        // NO Expires - session cookie only
                    };
                    context.Response.Cookies.Append("auth-token", response.Token, accessCookieOptions);

                    // Set refresh token cookie scoped to auth endpoints only (minimizes exposure)
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Path = "/api/auth" // SCOPED: Only sent to auth endpoints
                    };
                    // Only set Expires for "remember me" - makes it a persistent cookie
                    // Without Expires, it's a session cookie (cleared when browser closes)
                    if (response.RememberMe && response.RefreshTokenExpiresAt.HasValue)
                    {
                        refreshCookieOptions.Expires = response.RefreshTokenExpiresAt.Value;
                    }
                    context.Response.Cookies.Append("refresh-token", response.RefreshToken!, refreshCookieOptions);

                    // CRITICAL: Regenerate CSRF token after successful login
                    // This prevents session fixation attacks and ensures fresh token for authenticated session
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                        new CookieOptions
                        {
                            HttpOnly = false, // JavaScript must read this token
                            SameSite = SameSiteMode.Lax,
                            Secure = context.Request.IsHttps,
                            Path = "/"
                        });

                    // Return user info with validated return URL (BFF pattern)
                    // Frontend should redirect to returnUrl if present, otherwise /dashboard
                    return Results.Ok(new
                    {
                        Success = true,
                        User = response.User,
                        ReturnUrl = response.ReturnUrl, // Null if not provided or validation failed
                        Message = "Login successful"
                    });
                }

                // Determine appropriate HTTP status code based on error type
                // 401 Unauthorized: Authentication failures (wrong credentials, locked account)
                // 400 Bad Request: Validation failures (missing required fields)
                // 500 Internal Server Error: System failures
                var statusCode = error switch
                {
                    var e when e.Contains("Invalid email/scene name or password") => 401,
                    var e when e.Contains("locked") => 401,
                    var e when e.Contains("is required") => 400,
                    var e when e.Contains("could not be completed") => 500,
                    _ => 400
                };

                return Results.Problem(
                    title: "Login Failed",
                    detail: error,
                    statusCode: statusCode);
            })
            .DisableAntiforgery() // Login is public - no CSRF token available yet
            .WithName("Login")
            .WithSummary("Authenticate user with email and password (with optional return URL)")
            .WithDescription("Validates user credentials, performs OWASP-compliant return URL validation, and returns JWT token with user information. If returnUrl is provided and valid, it will be included in response for post-login redirect.")
            .WithTags("Authentication")
            .Produces<LoginResponse>(200)
            .Produces(400)
            .Produces(401);

        // User registration endpoint
        // SECURITY: Registration is PUBLIC and should NOT require CSRF token (user doesn't have account yet)
        // Rate limiting and email verification provide spam protection
        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            IAuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await authService.RegisterAsync(request, cancellationToken);

                return success
                    ? Results.Created($"/api/auth/user/{response!.Id}", response)
                    : Results.Problem(
                        title: "Registration Failed",
                        detail: error,
                        statusCode: 400);
            })
            .DisableAntiforgery() // Registration is public - no CSRF token available yet
            .WithName("Register")
            .WithSummary("Register new user account")
            .WithDescription("Creates a new user account with email, password, and scene name")
            .WithTags("Authentication")
            .Produces<AuthUserResponse>(201)
            .Produces(400);

        // Service token generation for service-to-service authentication
        app.MapPost("/api/auth/service-token", async (
            ServiceTokenRequest request,
            IAuthenticationService authService,
            IConfiguration configuration,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Validate service secret from header
                var serviceSecret = context.Request.Headers["X-Service-Secret"].FirstOrDefault();
                var expectedSecret = configuration["ServiceAuth:Secret"];

                if (string.IsNullOrEmpty(serviceSecret) || serviceSecret != expectedSecret)
                {
                    return Results.Problem(
                        title: "Invalid Service Credentials",
                        detail: "Service secret is missing or incorrect",
                        statusCode: 401);
                }

                // Validate request data
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Email))
                {
                    return Results.Problem(
                        title: "Invalid Request",
                        detail: "User ID and email are required",
                        statusCode: 400);
                }

                var (success, response, error) = await authService.GetServiceTokenAsync(
                    request.UserId,
                    request.Email,
                    cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Service Token Generation Failed",
                        detail: error,
                        statusCode: error.Contains("not found") ? 404 : 400);
            })
            .AllowAnonymous() // Authentication is handled via shared secret
            .WithName("GetServiceToken")
            .WithSummary("Generate JWT token for service-to-service authentication")
            .WithDescription("Used by Web Service to get JWT tokens for API calls using service secret authentication")
            .WithTags("Authentication")
            .Produces<LoginResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);

        // Logout endpoint with cookie clearing, token blacklisting, and refresh token revocation
        // CRITICAL SECURITY: CSRF protection REQUIRED to prevent logout CSRF attacks
        // .NET 10 Minimal APIs with JSON do NOT validate CSRF automatically - must inject IAntiforgery and validate manually
        app.MapPost("/api/auth/logout", async (
            HttpContext context,
            IAntiforgery antiforgery,
            ILogger<IAuthenticationService> logger,
            IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
            IRefreshTokenService refreshTokenService,
            CancellationToken cancellationToken) =>
            {
                // CRITICAL: Validate anti-forgery token FIRST before any logout logic
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    logger.LogWarning("CSRF validation failed for logout: {Message}", ex.Message);
                    return Results.Problem(
                        title: "CSRF Validation Failed",
                        detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                        statusCode: 400);
                }

                logger.LogInformation("Logout request received from {RemoteIP}", context.Connection.RemoteIpAddress);

                try
                {
                    // Blacklist the JWT access token to prevent use during remaining lifetime
                    var authCookie = context.Request.Cookies["auth-token"];
                    Guid? userGuid = null;

                    if (!string.IsNullOrEmpty(authCookie))
                    {
                        var jti = jwtService.ExtractJti(authCookie);
                        if (!string.IsNullOrEmpty(jti))
                        {
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            try
                            {
                                var jsonToken = handler.ReadJwtToken(authCookie);
                                var expirationTime = jsonToken.ValidTo;
                                tokenBlacklistService.BlacklistToken(jti, expirationTime);
                                logger.LogDebug("JWT blacklisted: JTI {Jti}, expires {ExpirationTime}", jti, expirationTime);

                                // Extract user ID for refresh token revocation
                                var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
                                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedGuid))
                                {
                                    userGuid = parsedGuid;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to parse JWT for blacklisting, continuing with logout");
                            }
                        }
                    }

                    // Revoke all refresh tokens for this user (terminates all sessions)
                    if (userGuid.HasValue)
                    {
                        await refreshTokenService.RevokeAllUserTokensAsync(userGuid.Value, cancellationToken);
                        logger.LogInformation("Revoked all refresh tokens for user {UserId} on logout", userGuid.Value);
                    }

                    // Clear all authentication cookies
                    ClearAuthCookies(context);

                    logger.LogInformation("Logout completed successfully");

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Logged out successfully"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Logout error occurred");
                    // Still return success - logout should always succeed from user perspective
                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Logged out successfully"
                    });
                }
            })
            .AllowAnonymous() // CRITICAL FIX: Allow logout even with expired/invalid tokens
            // CSRF validation handled via IAntiforgery.ValidateRequestAsync() in endpoint logic above
            .WithName("Logout")
            .WithSummary("Logout current user")
            .WithDescription("Logs out the current user, clears cookies, blacklists JWT, and revokes refresh tokens. Works even with expired tokens.")
            .WithTags("Authentication")
            .Produces<object>(200);

        // Get user information from httpOnly cookie
        app.MapGet("/api/auth/user", async (
            HttpContext context,
            IAuthenticationService authService,
            IJwtService jwtService,
            ILogger<IAuthenticationService> logger,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    // Get token from httpOnly cookie
                    var token = context.Request.Cookies["auth-token"];
                    if (string.IsNullOrEmpty(token))
                    {
                        return Results.Problem(
                            title: "Not Authenticated",
                            detail: "Authentication cookie not found",
                            statusCode: 401);
                    }

                    // Validate token and extract user ID
                    if (!jwtService.ValidateToken(token))
                    {
                        // Clear all auth cookies when token is invalid
                        ClearAuthCookies(context);

                        return Results.Problem(
                            title: "Invalid Token",
                            detail: "Authentication token is invalid or expired",
                            statusCode: 401);
                    }

                    // Extract user ID from token
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(token);
                    var userId = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "sub")?.Value;

                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Problem(
                            title: "Invalid Token",
                            detail: "User ID not found in token",
                            statusCode: 401);
                    }

                    var (success, response, error) = await authService.GetCurrentUserAsync(userId, cancellationToken);

                    return success
                        ? Results.Ok(response)
                        : Results.Problem(
                            title: "Get Current User Failed",
                            detail: error,
                            statusCode: response == null ? 404 : 500);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting user from cookie");
                    return Results.Problem(
                        title: "Authentication Error",
                        detail: "Could not validate authentication",
                        statusCode: 500);
                }
            })
            .AllowAnonymous() // Uses cookie-based authentication
            .WithName("GetUserFromCookie")
            .WithSummary("Get current user information from httpOnly cookie")
            .WithDescription("BFF pattern - validates httpOnly cookie and returns user info")
            .WithTags("Authentication")
            .Produces<AuthUserResponse>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Refresh token endpoint using database-backed refresh tokens with rotation
        // SECURITY: CSRF protection REQUIRED for defense-in-depth
        // .NET 10 Minimal APIs with JSON do NOT validate CSRF automatically - must inject IAntiforgery and validate manually
        app.MapPost("/api/auth/refresh", async (
            HttpContext context,
            IAntiforgery antiforgery,
            IRefreshTokenService refreshTokenService,
            IAuthenticationService authService,
            ILogger<IAuthenticationService> logger,
            CancellationToken cancellationToken) =>
            {
                // CSRF validation for defense-in-depth
                // NOTE: On wake-from-sleep, CSRF token may be stale. Frontend handles retry.
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    logger.LogWarning("CSRF validation failed for token refresh: {Message}", ex.Message);
                    return Results.Problem(
                        title: "CSRF Validation Failed",
                        detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                        statusCode: 400);
                }

                try
                {
                    // Read refresh token from scoped httpOnly cookie
                    var refreshTokenCookie = context.Request.Cookies["refresh-token"];
                    if (string.IsNullOrEmpty(refreshTokenCookie))
                    {
                        return Results.Problem(
                            title: "No Refresh Token",
                            detail: "No refresh token found",
                            statusCode: 401);
                    }

                    var deviceInfo = context.Request.Headers.UserAgent.ToString();

                    // Rotate the refresh token (validates, revokes old, creates new)
                    var (newToken, userId, error) = await refreshTokenService.RotateRefreshTokenAsync(
                        refreshTokenCookie, deviceInfo, cancellationToken);

                    if (newToken is null || userId is null)
                    {
                        // Token invalid, expired, or reuse detected - clear all auth cookies
                        ClearAuthCookies(context);

                        logger.LogWarning("Refresh token rotation failed: {Error}", error);
                        return Results.Problem(
                            title: "Refresh Failed",
                            detail: error,
                            statusCode: 401);
                    }

                    // Look up user to get email for JWT generation
                    var (userSuccess, userResponse, userError) = await authService.GetCurrentUserAsync(
                        userId.Value.ToString(), cancellationToken);

                    if (!userSuccess || userResponse is null)
                    {
                        ClearAuthCookies(context);
                        return Results.Problem(
                            title: "User Not Found",
                            detail: "Could not find user for token refresh",
                            statusCode: 401);
                    }

                    // Generate new JWT access token via service token endpoint
                    var (tokenSuccess, tokenResponse, tokenError) = await authService.GetServiceTokenAsync(
                        userId.Value.ToString(), userResponse.Email, cancellationToken);

                    if (!tokenSuccess || tokenResponse is null)
                    {
                        ClearAuthCookies(context);
                        return Results.Problem(
                            title: "Token Generation Failed",
                            detail: "Could not generate new access token",
                            statusCode: 500);
                    }

                    // Set new access token cookie (session cookie - no Expires)
                    var accessCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    };
                    context.Response.Cookies.Append("auth-token", tokenResponse.Token, accessCookieOptions);

                    // Set new refresh token cookie (persistent if remember-me, session otherwise)
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Path = "/api/auth"
                    };
                    // Determine if this is a remember-me session based on token expiration window
                    var isRememberMe = (newToken.ExpiresAt - newToken.CreatedAt).TotalHours > 25;
                    if (isRememberMe)
                    {
                        refreshCookieOptions.Expires = newToken.ExpiresAt;
                    }
                    context.Response.Cookies.Append("refresh-token", newToken.Token, refreshCookieOptions);

                    logger.LogDebug("Token refreshed successfully for user {UserId}", userId);

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Token refreshed successfully",
                        ExpiresAt = tokenResponse.ExpiresAt
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Token refresh failed");
                    return Results.Problem(
                        title: "Refresh Error",
                        detail: "Could not refresh authentication token",
                        statusCode: 500);
                }
            })
            .AllowAnonymous() // Uses cookie-based authentication (refresh token in httpOnly cookie)
            // CSRF validation handled via IAntiforgery.ValidateRequestAsync() in endpoint logic above
            .WithName("RefreshToken")
            .WithSummary("Refresh authentication token using database-backed refresh token")
            .WithDescription("BFF pattern - validates refresh token cookie, rotates token, and issues new JWT access token")
            .WithTags("Authentication")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(500);

        // Phase 2: Verify email endpoint
        app.MapPost("/api/auth/verify-email", async (
            VerifyEmailRequest request,
            IAuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await authService.VerifyEmailAsync(request.UserId, request.Token, cancellationToken);

                return success
                    ? Results.Ok(new
                    {
                        Success = true,
                        Message = "Your email has been verified successfully. You can now log in."
                    })
                    : Results.Problem(
                        title: "Email Verification Failed",
                        detail: error,
                        statusCode: 400);
            })
            .AllowAnonymous()
            .DisableAntiforgery() // Public endpoint - verification token in URL provides security
            .WithName("VerifyEmail")
            .WithSummary("Verify user email with token")
            .WithDescription("Confirms user email address using verification token sent during registration")
            .WithTags("Authentication")
            .Produces<object>(200)
            .Produces(400);

        // Phase 2: Resend verification email endpoint
        app.MapPost("/api/auth/resend-verification", async (
            ResendVerificationRequest request,
            IAuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await authService.ResendVerificationEmailAsync(request.Email, cancellationToken);

                // Always return success for security (prevent email enumeration)
                return Results.Ok(new
                {
                    Success = true,
                    Message = "If an account exists with this email and is not yet verified, a verification email has been sent."
                });
            })
            .AllowAnonymous()
            .DisableAntiforgery() // Public endpoint - rate limiting provides spam protection
            .WithName("ResendVerification")
            .WithSummary("Resend email verification email")
            .WithDescription("Sends a new verification email to the user. Returns generic success message to prevent email enumeration.")
            .WithTags("Authentication")
            .Produces<object>(200);

        // Phase 3: Forgot password endpoint
        app.MapPost("/api/auth/forgot-password", async (
            ForgotPasswordRequest request,
            IAuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await authService.ForgotPasswordAsync(request.Email, cancellationToken);

                // Always return success for security (prevent email enumeration)
                return Results.Ok(new
                {
                    Success = true,
                    Message = "If an account exists with this email, a password reset link has been sent."
                });
            })
            .AllowAnonymous()
            .DisableAntiforgery() // Public endpoint - rate limiting provides spam protection
            .WithName("ForgotPassword")
            .WithSummary("Initiate password reset process")
            .WithDescription("Sends a password reset email to the user. Returns generic success message to prevent email enumeration.")
            .WithTags("Authentication")
            .Produces<object>(200);

        // Phase 3: Reset password endpoint
        app.MapPost("/api/auth/reset-password", async (
            ResetPasswordRequest request,
            IAuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await authService.ResetPasswordAsync(
                    request.UserId,
                    request.Token,
                    request.NewPassword,
                    cancellationToken);

                return success
                    ? Results.Ok(new
                    {
                        Success = true,
                        Message = "Your password has been reset successfully. You can now log in with your new password."
                    })
                    : Results.Problem(
                        title: "Password Reset Failed",
                        detail: error,
                        statusCode: 400);
            })
            .AllowAnonymous()
            .DisableAntiforgery() // Public endpoint - reset token in URL provides security
            .WithName("ResetPassword")
            .WithSummary("Reset password with token")
            .WithDescription("Resets user password using the token from the password reset email")
            .WithTags("Authentication")
            .Produces<object>(200)
            .Produces(400);

    }

    /// <summary>
    /// Clear all authentication cookies (access token + refresh token).
    /// Used on refresh failure, logout, and invalid token detection to ensure clean state.
    /// </summary>
    private static void ClearAuthCookies(HttpContext context)
    {
        // Clear access token cookie
        context.Response.Cookies.Delete("auth-token", new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        // Clear refresh token cookie (must match the Path used when setting it)
        context.Response.Cookies.Delete("refresh-token", new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }
}
