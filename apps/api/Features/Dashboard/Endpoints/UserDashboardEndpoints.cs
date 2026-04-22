using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Dashboard.Services;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Dashboard.Endpoints;

/// <summary>
/// User Dashboard API endpoints for wireframe v4
/// Provides user-specific event data and profile management
/// All endpoints require authentication and return only current user's data
/// </summary>
public static class UserDashboardEndpoints
{
    /// <summary>
    /// Register user dashboard endpoints using minimal API pattern
    /// </summary>
    public static void MapUserDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // ========== ENDPOINT 1: Get User Events ==========
        app.MapGet("/api/users/{userId:guid}/events", async (
            Guid userId,
            [FromQuery] bool includePast = false,
            IUserDashboardProfileService service = null!,
            ClaimsPrincipal user = null!,
            CancellationToken cancellationToken = default) =>
            {
                // Verify user is accessing their own data
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
                {
                    return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                        title: "Unauthorized",
                        detail: "You can only access your own events",
                        statusCode: 403);
                }

                var result = await service.GetUserEventsAsync(userId, includePast, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value) // Direct DTO list
                    : result.ToProblem("Failed to retrieve events");
            })
            .RequireAuthorization()
            .WithName("GetUserRegisteredEvents")
            .WithSummary("Get user's registered events")
            .WithDescription("Returns list of events the user has registered for or purchased tickets for. Excludes past events by default.")
            .WithTags("Dashboard")
            .Produces<List<UserEventDto>>(200)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // ========== ENDPOINT 2: Get Vetting Status ==========
        app.MapGet("/api/users/{userId:guid}/vetting-status", async (
            Guid userId,
            IUserDashboardProfileService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Verify user is accessing their own data
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
                {
                    return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                        title: "Unauthorized",
                        detail: "You can only access your own vetting status",
                        statusCode: 403);
                }

                var result = await service.GetVettingStatusAsync(userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value) // Direct DTO
                    : result.ToProblem("Failed to retrieve vetting status");
            })
            .RequireAuthorization()
            .WithName("GetUserDashboardVettingStatus")
            .WithSummary("Get user's vetting status")
            .WithDescription("Returns the user's current vetting status with display message for alert box")
            .WithTags("Dashboard")
            .Produces<VettingStatusDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // ========== ENDPOINT 3: Get User Profile ==========
        app.MapGet("/api/users/{userId:guid}/profile", async (
            Guid userId,
            IUserDashboardProfileService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Verify user is accessing their own data
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
                {
                    return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                        title: "Unauthorized",
                        detail: "You can only access your own profile",
                        statusCode: 403);
                }

                var result = await service.GetUserProfileAsync(userId, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToProblem("Profile not found");
                }

                return Results.Ok(result.Value); // Direct DTO
            })
            .RequireAuthorization()
            .WithName("GetUserDashboardProfile")
            .WithSummary("Get user profile")
            .WithDescription("Returns the user's profile information for settings page")
            .WithTags("Dashboard")
            .Produces<UserProfileDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // ========== ENDPOINT 4: Update User Profile ==========
        // SECURITY: CSRF protection enabled automatically via middleware
        // Prevents unauthorized profile modifications via CSRF attacks
        app.MapPut("/api/users/{userId:guid}/profile", async (
            Guid userId,
            UpdateProfileDto request,
            IUserDashboardProfileService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Verify user is accessing their own data
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
                {
                    return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                        title: "Unauthorized",
                        detail: "You can only update your own profile",
                        statusCode: 403);
                }

                var result = await service.UpdateUserProfileAsync(userId, request, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToProblem("Failed to update profile");
                }

                return Results.Ok(result.Value); // Direct DTO
            })
            .RequireAuthorization()
            .WithName("UpdateUserDashboardProfile")
            .WithSummary("Update user profile")
            .WithDescription("Updates the user's profile information")
            .WithTags("Dashboard")
            .Produces<UserProfileDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // ========== ENDPOINT 5: Change Password ==========
        // SECURITY: CSRF protection enabled automatically via middleware
        // CRITICAL: Password changes must validate CSRF tokens to prevent account takeover
        app.MapPost("/api/users/{userId:guid}/change-password", async (
            Guid userId,
            ChangePasswordDto request,
            IUserDashboardProfileService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Verify user is accessing their own data
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
                {
                    return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                        title: "Unauthorized",
                        detail: "You can only change your own password",
                        statusCode: 403);
                }

                var result = await service.ChangePasswordAsync(userId, request, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToProblem("Failed to change password");
                }

                return Results.Ok(true); // Direct boolean success response
            })
            .RequireAuthorization()
            .WithName("ChangeUserDashboardPassword")
            .WithSummary("Change user password")
            .WithDescription("Changes the user's password after verifying current password")
            .WithTags("Dashboard")
            .Produces<bool>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(500);
    }
}
