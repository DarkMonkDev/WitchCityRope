using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Users.Models;
using WitchCityRope.Api.Features.Users.Services;

namespace WitchCityRope.Api.Features.Users.Endpoints;

/// <summary>
/// User management minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Register user management endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Get current user profile (authenticated users) - plural endpoint
        app.MapGet("/api/users/profile", async (
            UserManagementService userService,
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

                var (success, response, error) = await userService.GetProfileAsync(userId, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Profile Failed",
                        detail: error,
                        statusCode: response == null ? 404 : 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetUserProfile")
            .WithSummary("Get current user profile")
            .WithDescription("Returns the current user's profile information based on JWT token")
            .WithTags("Users")
            .Produces<UserDto>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Get current user profile (authenticated users) - singular endpoint for E2E tests
        app.MapGet("/api/user/profile", async (
            UserManagementService userService,
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

                var (success, response, error) = await userService.GetProfileAsync(userId, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Profile Failed",
                        detail: error,
                        statusCode: response == null ? 404 : 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetUserProfileSingular")
            .WithSummary("Get current user profile (singular endpoint)")
            .WithDescription("Returns the current user's profile information based on JWT token")
            .WithTags("Users")
            .Produces<UserDto>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Update current user profile (authenticated users)
        app.MapPut("/api/users/profile", async (
            UpdateProfileRequest request,
            UserManagementService userService,
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

                var (success, response, error) = await userService.UpdateProfileAsync(userId, request, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Update Profile Failed",
                        detail: error,
                        statusCode: error.Contains("Scene name is already taken") ? 409 : 400);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("UpdateUserProfile")
            .WithSummary("Update current user profile")
            .WithDescription("Updates the current user's profile information (scene name, pronouns)")
            .WithTags("Users")
            .Produces<UserDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(409);

        // Get all users (admin only)
        app.MapGet("/api/admin/users", async (
            [AsParameters] UserSearchRequest request,
            UserManagementService userService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await userService.GetUsersAsync(request, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Users Failed",
                        detail: error,
                        statusCode: 500);
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Admin")) // Administrator or Admin role required
            .WithName("GetUsers")
            .WithSummary("Get paginated list of users (admin only)")
            .WithDescription("Returns a paginated list of users with optional filtering and sorting")
            .WithTags("Admin", "Users")
            .Produces<UserListResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);

        // Get single user by ID (admin only)
        app.MapGet("/api/admin/users/{id}", async (
            string id,
            UserManagementService userService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await userService.GetUserAsync(id, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get User Failed",
                        detail: error,
                        statusCode: response == null ? 404 : 400);
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Admin")) // Administrator or Admin role required
            .WithName("GetUser")
            .WithSummary("Get user by ID (admin only)")
            .WithDescription("Returns detailed user information by user ID")
            .WithTags("Admin", "Users")
            .Produces<UserDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);

        // Update user by ID (admin only)
        app.MapPut("/api/admin/users/{id}", async (
            string id,
            UpdateUserRequest request,
            UserManagementService userService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await userService.UpdateUserAsync(id, request, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Update User Failed",
                        detail: error,
                        statusCode: error.Contains("not found") ? 404 :
                                  error.Contains("already taken") ? 409 : 400);
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Admin")) // Administrator or Admin role required
            .WithName("UpdateUser")
            .WithSummary("Update user by ID (admin only)")
            .WithDescription("Updates user information including role, status, and profile data")
            .WithTags("Admin", "Users")
            .Produces<UserDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409);

        // Update user roles (admin only)
        app.MapPut("/api/admin/users/{userId}/roles", async (
            string userId,
            UpdateUserRolesRequest request,
            UserManagementService userService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await userService.UpdateUserRolesAsync(userId, request, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Update User Roles Failed",
                        detail: error,
                        statusCode: error.Contains("not found") ? 404 :
                                  error.Contains("Invalid") ? 400 : 500);
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Admin")) // Administrator or Admin role required
            .WithName("UpdateUserRoles")
            .WithSummary("Update user roles (admin only)")
            .WithDescription(@"Updates user roles in the admin user management system.
Accepts a list of roles: 'Teacher', 'SafetyTeam', 'Administrator'.
Empty list = Regular member (no special roles).
Roles are stored as comma-separated string (e.g., 'Teacher,SafetyTeam').
Role changes are logged for audit purposes.")
            .WithTags("Admin", "Users")
            .Produces<UserDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Get users by role (for dropdowns)
        app.MapGet("/api/users/by-role/{role}", async (
            string role,
            UserManagementService userService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await userService.GetUsersByRoleAsync(role, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Users By Role Failed",
                        detail: error,
                        statusCode: 500);
            })
            .RequireAuthorization() // Only authenticated users can access
            .WithName("GetUsersByRole")
            .WithSummary("Get users by role")
            .WithDescription("Get list of users filtered by role (e.g., 'Teacher', 'Admin') for dropdown options")
            .WithTags("Users")
            .Produces<IEnumerable<UserOptionDto>>(200)
            .Produces(401)
            .Produces(500);

        // Map member details endpoints (admin-only comprehensive member information)
        app.MapMemberDetailsEndpoints();
    }
}