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
                    // Set httpOnly cookie with JWT token for BFF pattern
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps, // Use HTTPS in production
                        SameSite = SameSiteMode.Strict, // SECURITY: Maximum CSRF protection (changed from Lax)
                        Path = "/",
                        Expires = response.ExpiresAt
                    };

                    context.Response.Cookies.Append("auth-token", response.Token, cookieOptions);

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

        // Logout endpoint with cookie clearing and token blacklisting
        // CRITICAL SECURITY: CSRF protection REQUIRED to prevent logout CSRF attacks
        // .NET 10 Minimal APIs with JSON do NOT validate CSRF automatically - must inject IAntiforgery and validate manually
        app.MapPost("/api/auth/logout", async (
            HttpContext context,
            IAntiforgery antiforgery,
            ILogger<IAuthenticationService> logger,
            IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
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


                logger.LogInformation("🔐 LOGOUT DEBUG: Logout request received from {RemoteIP}", context.Connection.RemoteIpAddress);

                // DEBUG: Log all incoming cookies
                logger.LogInformation("🔐 LOGOUT DEBUG: All incoming cookies: {Cookies}",
                    string.Join(", ", context.Request.Cookies.Select(c => $"{c.Key}={c.Value?.Substring(0, Math.Min(c.Value.Length, 20))}...")));
                try
                {
                    // Log logout attempt and blacklist the token
                    var authCookie = context.Request.Cookies["auth-token"];
                    if (!string.IsNullOrEmpty(authCookie))
                    {
                        logger.LogInformation("🔐 LOGOUT DEBUG: Found auth-token cookie, length: {Length}", authCookie.Length);

                        // Extract JTI and add to blacklist to invalidate the token server-side
                        var jti = jwtService.ExtractJti(authCookie);
                        if (!string.IsNullOrEmpty(jti))
                        {
                            logger.LogInformation("🔐 LOGOUT DEBUG: Extracted JTI: {Jti}", jti);

                            // Get token expiration time
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            try
                            {
                                var jsonToken = handler.ReadJwtToken(authCookie);
                                var expirationTime = jsonToken.ValidTo;

                                // Add token to blacklist
                                tokenBlacklistService.BlacklistToken(jti, expirationTime);
                                logger.LogInformation("🔐 LOGOUT DEBUG: Token with JTI {Jti} blacklisted until {ExpirationTime}", jti, expirationTime);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "🔐 LOGOUT DEBUG: Failed to parse token for blacklisting, but continuing with logout");
                            }
                        }
                        else
                        {
                            logger.LogWarning("🔐 LOGOUT DEBUG: Could not extract JTI from token for blacklisting");
                        }
                    }
                    else
                    {
                        logger.LogInformation("🔐 LOGOUT DEBUG: No auth-token cookie found - clearing any stale cookies");
                    }

                    // Clear the httpOnly authentication cookie with EXACTLY the same options as when set
                    // CRITICAL: Use same options as login to ensure proper deletion
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps, // Use HTTPS in production
                        SameSite = SameSiteMode.Strict, // Must match login cookie settings (changed to Strict)
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(-1) // Set to past date for deletion
                    };

                    logger.LogInformation("🔐 LOGOUT DEBUG: Clearing cookie with options - HttpOnly: {HttpOnly}, Secure: {Secure}, SameSite: {SameSite}, Path: {Path}, Expires: {Expires}",
                        cookieOptions.HttpOnly, cookieOptions.Secure, cookieOptions.SameSite, cookieOptions.Path, cookieOptions.Expires);

                    // Method 1: Explicitly set cookie to empty with past expiration
                    context.Response.Cookies.Append("auth-token", "", cookieOptions);
                    logger.LogInformation("🔐 LOGOUT DEBUG: Called Append with empty value and past expiration");

                    // Method 2: Also use Delete method as backup
                    context.Response.Cookies.Delete("auth-token", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict, // Must match login cookie settings (changed to Strict)
                        Path = "/"
                    });
                    logger.LogInformation("🔐 LOGOUT DEBUG: Called Delete method as backup");

                    // DEBUG: Log response headers being set
                    context.Response.OnStarting(() =>
                    {
                        var setCookieHeaders = context.Response.Headers["Set-Cookie"];
                        logger.LogInformation("🔐 LOGOUT DEBUG: Set-Cookie headers: {Headers}",
                            string.Join("; ", setCookieHeaders.ToArray()));
                        return Task.CompletedTask;
                    });

                    logger.LogInformation("🔐 LOGOUT DEBUG: Logout completed successfully");

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Logged out successfully"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "🔐 LOGOUT DEBUG: Logout error occurred");
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
            .WithDescription("Logs out the current user, clears cookies, and blacklists tokens. Works even with expired tokens.")
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
                        // Clear invalid cookie with same options as when set
                        var clearCookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = context.Request.IsHttps,
                            SameSite = SameSiteMode.Strict, // Must match login cookie settings (changed to Strict)
                            Path = "/",
                            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Set to past date for deletion
                        };

                        // Method 1: Set to empty with past expiration
                        context.Response.Cookies.Append("auth-token", "", clearCookieOptions);

                        // Method 2: Also use Delete as backup
                        context.Response.Cookies.Delete("auth-token", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = context.Request.IsHttps,
                            SameSite = SameSiteMode.Strict, // Must match login cookie settings (changed to Strict)
                            Path = "/"
                        });

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

        // Refresh token endpoint for silent token refresh
        // SECURITY: CSRF protection REQUIRED to prevent session hijacking via CSRF
        // .NET 10 Minimal APIs with JSON do NOT validate CSRF automatically - must inject IAntiforgery and validate manually
        app.MapPost("/api/auth/refresh", async (
            HttpContext context,
            IAntiforgery antiforgery,
            IAuthenticationService authService,
            IJwtService jwtService,
            ILogger<IAuthenticationService> logger,
            CancellationToken cancellationToken) =>
            {
                // CRITICAL: Validate anti-forgery token FIRST before any refresh logic
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
                    // Get current token from cookie
                    var currentToken = context.Request.Cookies["auth-token"];
                    if (string.IsNullOrEmpty(currentToken))
                    {
                        return Results.Problem(
                            title: "No Token",
                            detail: "No authentication token found for refresh",
                            statusCode: 401);
                    }

                    // Validate current token structure (allow expired for refresh)
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    System.IdentityModel.Tokens.Jwt.JwtSecurityToken jsonToken;

                    try
                    {
                        jsonToken = handler.ReadJwtToken(currentToken);
                    }
                    catch
                    {
                        return Results.Problem(
                            title: "Invalid Token",
                            detail: "Token format is invalid",
                            statusCode: 401);
                    }

                    // Extract user info from token
                    var userId = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "sub")?.Value;
                    var email = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "email")?.Value;

                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                    {
                        return Results.Problem(
                            title: "Invalid Token Claims",
                            detail: "Required user information not found in token",
                            statusCode: 401);
                    }

                    // Generate new token for the user
                    var (success, response, error) = await authService.GetServiceTokenAsync(userId, email, cancellationToken);

                    if (success && response != null)
                    {
                        // Set new httpOnly cookie
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = context.Request.IsHttps,
                            SameSite = SameSiteMode.Strict,
                            Path = "/",
                            Expires = response.ExpiresAt
                        };

                        context.Response.Cookies.Append("auth-token", response.Token, cookieOptions);

                        logger.LogDebug("Token refreshed successfully for user {UserId}", userId);

                        return Results.Ok(new
                        {
                            Success = true,
                            Message = "Token refreshed successfully",
                            ExpiresAt = response.ExpiresAt
                        });
                    }

                    return Results.Problem(
                        title: "Refresh Failed",
                        detail: error,
                        statusCode: 400);
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
            .AllowAnonymous() // Uses cookie-based authentication
            // CSRF validation handled via IAntiforgery.ValidateRequestAsync() in endpoint logic above
            .WithName("RefreshToken")
            .WithSummary("Refresh authentication token silently")
            .WithDescription("BFF pattern - refreshes httpOnly cookie with new JWT token")
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
}