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
            AuthenticationService authService,
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

        // User login endpoint with httpOnly cookie support
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AuthenticationService authService,
            HttpContext context,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await authService.LoginAsync(request, cancellationToken);

                if (success && response != null)
                {
                    // Set httpOnly cookie with JWT token for BFF pattern
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps, // Use HTTPS in production
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                        Expires = response.ExpiresAt
                    };

                    context.Response.Cookies.Append("auth-token", response.Token, cookieOptions);

                    // Return user info without token (BFF pattern)
                    return Results.Ok(new { 
                        Success = true,
                        User = response.User,
                        Message = "Login successful" 
                    });
                }

                return Results.Problem(
                    title: "Login Failed",
                    detail: error,
                    statusCode: error.Contains("Invalid email or password") ? 401 : 400);
            })
            .WithName("Login")
            .WithSummary("Authenticate user with email and password")
            .WithDescription("Validates user credentials and returns JWT token with user information")
            .WithTags("Authentication")
            .Produces<LoginResponse>(200)
            .Produces(400)
            .Produces(401);

        // User registration endpoint
        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            AuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await authService.RegisterAsync(request, cancellationToken);

                return success 
                    ? Results.Created($"/api/auth/user/{response.Id}", response)
                    : Results.Problem(
                        title: "Registration Failed",
                        detail: error,
                        statusCode: 400);
            })
            .WithName("Register")
            .WithSummary("Register new user account")
            .WithDescription("Creates a new user account with email, password, and scene name")
            .WithTags("Authentication")
            .Produces<AuthUserResponse>(201)
            .Produces(400);

        // Service token generation for service-to-service authentication
        app.MapPost("/api/auth/service-token", async (
            ServiceTokenRequest request,
            AuthenticationService authService,
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
        app.MapPost("/api/auth/logout", async (
            HttpContext context,
            ILogger<AuthenticationService> logger,
            IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    // Log logout attempt and blacklist the token
                    var authCookie = context.Request.Cookies["auth-token"];
                    if (!string.IsNullOrEmpty(authCookie))
                    {
                        // Extract JTI and add to blacklist to invalidate the token server-side
                        var jti = jwtService.ExtractJti(authCookie);
                        if (!string.IsNullOrEmpty(jti))
                        {
                            // Get token expiration time
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            try
                            {
                                var jsonToken = handler.ReadJwtToken(authCookie);
                                var expirationTime = jsonToken.ValidTo;

                                // Add token to blacklist
                                tokenBlacklistService.BlacklistToken(jti, expirationTime);
                                logger.LogInformation("User logged out - token with JTI {Jti} blacklisted until {ExpirationTime}", jti, expirationTime);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to parse token for blacklisting, but continuing with logout");
                            }
                        }
                        else
                        {
                            logger.LogWarning("Could not extract JTI from token for blacklisting");
                        }
                    }

                    // Clear the httpOnly authentication cookie with EXACTLY the same options as when set
                    // CRITICAL: Use same options as login to ensure proper deletion
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps, // Use HTTPS in production
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(-1) // Set to past date for deletion
                    };

                    // Method 1: Explicitly set cookie to empty with past expiration
                    context.Response.Cookies.Append("auth-token", "", cookieOptions);

                    // Method 2: Also use Delete method as backup
                    context.Response.Cookies.Delete("auth-token", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    });

                    return Results.Ok(new {
                        Success = true,
                        Message = "Logged out successfully"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Logout error occurred");
                    // Still return success - logout should always succeed from user perspective
                    return Results.Ok(new {
                        Success = true,
                        Message = "Logged out successfully"
                    });
                }
            })
            .RequireAuthorization() // Require JWT Bearer token
            .WithName("Logout")
            .WithSummary("Logout current user")
            .WithDescription("Logs out the current user and provides instruction to clear stored tokens")
            .WithTags("Authentication")
            .Produces<object>(200)
            .Produces(401);

        // Get user information from httpOnly cookie
        app.MapGet("/api/auth/user", async (
            HttpContext context,
            AuthenticationService authService,
            IJwtService jwtService,
            ILogger<AuthenticationService> logger,
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
                            SameSite = SameSiteMode.Strict,
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
                            SameSite = SameSiteMode.Strict,
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
        app.MapPost("/api/auth/refresh", async (
            HttpContext context,
            AuthenticationService authService,
            IJwtService jwtService,
            ILogger<AuthenticationService> logger,
            CancellationToken cancellationToken) =>
            {
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
                        
                        return Results.Ok(new { 
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
            .WithName("RefreshToken")
            .WithSummary("Refresh authentication token silently")
            .WithDescription("BFF pattern - refreshes httpOnly cookie with new JWT token")
            .WithTags("Authentication")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(500);
    }
}