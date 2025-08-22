using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;

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

        // User login endpoint
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AuthenticationService authService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await authService.LoginAsync(request, cancellationToken);

                return success 
                    ? Results.Ok(response)
                    : Results.Problem(
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

        // Logout endpoint (placeholder for future implementation)
        app.MapPost("/api/auth/logout", () =>
            {
                // For throwaway implementation, just return success
                // Real implementation would clear cookies and invalidate tokens
                return Results.Ok(new { Success = true, Message = "Logged out successfully" });
            })
            .WithName("Logout")
            .WithSummary("Logout current user")
            .WithDescription("Placeholder logout endpoint for future implementation")
            .WithTags("Authentication")
            .Produces<object>(200);
    }
}