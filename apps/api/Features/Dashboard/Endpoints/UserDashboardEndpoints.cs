using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Dashboard.Services;
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
                    return Results.Json(
                        new ApiResponse<List<UserEventDto>>
                        {
                            Success = false,
                            Error = "Unauthorized",
                            Message = "You can only access your own events",
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 403);
                }

                var result = await service.GetUserEventsAsync(userId, includePast, cancellationToken);

                return Results.Ok(new ApiResponse<List<UserEventDto>>
                {
                    Success = result.IsSuccess,
                    Data = result.Value,
                    Error = result.IsSuccess ? null : result.Error,
                    Message = result.IsSuccess ? "Events retrieved successfully" : result.Details,
                    Timestamp = DateTime.UtcNow
                });
            })
            .RequireAuthorization()
            .WithName("GetUserRegisteredEvents")
            .WithSummary("Get user's registered events")
            .WithDescription("Returns list of events the user has registered for or purchased tickets for. Excludes past events by default.")
            .WithTags("Dashboard")
            .Produces<ApiResponse<List<UserEventDto>>>(200)
            .Produces<ApiResponse<List<UserEventDto>>>(403)
            .Produces<ApiResponse<List<UserEventDto>>>(500);

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
                    return Results.Json(
                        new ApiResponse<VettingStatusDto>
                        {
                            Success = false,
                            Error = "Unauthorized",
                            Message = "You can only access your own vetting status",
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 403);
                }

                var result = await service.GetVettingStatusAsync(userId, cancellationToken);

                return Results.Ok(new ApiResponse<VettingStatusDto>
                {
                    Success = result.IsSuccess,
                    Data = result.Value,
                    Error = result.IsSuccess ? null : result.Error,
                    Message = result.IsSuccess ? "Vetting status retrieved successfully" : result.Details,
                    Timestamp = DateTime.UtcNow
                });
            })
            .RequireAuthorization()
            .WithName("GetUserDashboardVettingStatus")
            .WithSummary("Get user's vetting status")
            .WithDescription("Returns the user's current vetting status with display message for alert box")
            .WithTags("Dashboard")
            .Produces<ApiResponse<VettingStatusDto>>(200)
            .Produces<ApiResponse<VettingStatusDto>>(403)
            .Produces<ApiResponse<VettingStatusDto>>(404)
            .Produces<ApiResponse<VettingStatusDto>>(500);

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
                    return Results.Json(
                        new ApiResponse<UserProfileDto>
                        {
                            Success = false,
                            Error = "Unauthorized",
                            Message = "You can only access your own profile",
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 403);
                }

                var result = await service.GetUserProfileAsync(userId, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Results.Json(
                        new ApiResponse<UserProfileDto>
                        {
                            Success = false,
                            Error = result.Error,
                            Message = result.Details,
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 404);
                }

                return Results.Ok(new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Profile retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            })
            .RequireAuthorization()
            .WithName("GetUserDashboardProfile")
            .WithSummary("Get user profile")
            .WithDescription("Returns the user's profile information for settings page")
            .WithTags("Dashboard")
            .Produces<ApiResponse<UserProfileDto>>(200)
            .Produces<ApiResponse<UserProfileDto>>(403)
            .Produces<ApiResponse<UserProfileDto>>(404)
            .Produces<ApiResponse<UserProfileDto>>(500);

        // ========== ENDPOINT 4: Update User Profile ==========
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
                    return Results.Json(
                        new ApiResponse<UserProfileDto>
                        {
                            Success = false,
                            Error = "Unauthorized",
                            Message = "You can only update your own profile",
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 403);
                }

                var result = await service.UpdateUserProfileAsync(userId, request, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Results.Json(
                        new ApiResponse<UserProfileDto>
                        {
                            Success = false,
                            Error = result.Error,
                            Message = result.Details,
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 400);
                }

                return Results.Ok(new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Profile updated successfully",
                    Timestamp = DateTime.UtcNow
                });
            })
            .RequireAuthorization()
            .WithName("UpdateUserDashboardProfile")
            .WithSummary("Update user profile")
            .WithDescription("Updates the user's profile information")
            .WithTags("Dashboard")
            .Produces<ApiResponse<UserProfileDto>>(200)
            .Produces<ApiResponse<UserProfileDto>>(400)
            .Produces<ApiResponse<UserProfileDto>>(403)
            .Produces<ApiResponse<UserProfileDto>>(404)
            .Produces<ApiResponse<UserProfileDto>>(500);

        // ========== ENDPOINT 5: Change Password ==========
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
                    return Results.Json(
                        new ApiResponse<bool>
                        {
                            Success = false,
                            Error = "Unauthorized",
                            Message = "You can only change your own password",
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 403);
                }

                var result = await service.ChangePasswordAsync(userId, request, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Results.Json(
                        new ApiResponse<bool>
                        {
                            Success = false,
                            Data = false,
                            Error = result.Error,
                            Message = result.Details,
                            Timestamp = DateTime.UtcNow
                        },
                        statusCode: 400);
                }

                return Results.Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Password changed successfully",
                    Timestamp = DateTime.UtcNow
                });
            })
            .RequireAuthorization()
            .WithName("ChangeUserDashboardPassword")
            .WithSummary("Change user password")
            .WithDescription("Changes the user's password after verifying current password")
            .WithTags("Dashboard")
            .Produces<ApiResponse<bool>>(200)
            .Produces<ApiResponse<bool>>(400)
            .Produces<ApiResponse<bool>>(403)
            .Produces<ApiResponse<bool>>(500);
    }
}
