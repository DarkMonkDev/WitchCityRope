using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Dashboard.Services;

namespace WitchCityRope.Api.Features.Dashboard.Endpoints;

/// <summary>
/// User dashboard minimal API endpoints
/// Provides user-specific dashboard data including profile, events, and statistics
/// Follows vertical slice architecture pattern with direct service injection
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Register dashboard endpoints using minimal API pattern
    /// All endpoints require authentication and return only current user's data
    /// </summary>
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // Get current user's dashboard data (profile, vetting status)
        app.MapGet("/api/dashboard", async (
            IUserDashboardService dashboardService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Extract user ID from JWT token claims
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Problem(
                        title: "Invalid Token",
                        detail: "User ID not found in token claims or invalid format",
                        statusCode: 401);
                }

                var result = await dashboardService.GetUserDashboardAsync(userId, cancellationToken);

                return result.IsSuccess 
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Get Dashboard Failed",
                        detail: result.Error,
                        statusCode: result.Value == null ? 404 : 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetUserDashboard")
            .WithSummary("Get current user's dashboard data")
            .WithDescription("Returns the current user's dashboard including profile info and vetting status")
            .WithTags("Dashboard");

        // Get current user's upcoming events
        app.MapGet("/api/dashboard/events", async (
            IUserDashboardService dashboardService,
            ClaimsPrincipal user,
            int count,
            CancellationToken cancellationToken) =>
            {
                // Extract user ID from JWT token claims
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Problem(
                        title: "Invalid Token",
                        detail: "User ID not found in token claims or invalid format",
                        statusCode: 401);
                }

                // Validate count parameter
                if (count <= 0 || count > 50)
                {
                    count = 3; // Default to 3 if invalid
                }

                var result = await dashboardService.GetUserEventsAsync(userId, count, cancellationToken);

                return result.IsSuccess 
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Get User Events Failed",
                        detail: result.Error,
                        statusCode: 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetUserEvents")
            .WithSummary("Get current user's upcoming events")
            .WithDescription("Returns the current user's upcoming events they are registered for")
            .WithTags("Dashboard");

        // Get current user's membership statistics
        app.MapGet("/api/dashboard/statistics", async (
            IUserDashboardService dashboardService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
            {
                // Extract user ID from JWT token claims
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Problem(
                        title: "Invalid Token",
                        detail: "User ID not found in token claims or invalid format",
                        statusCode: 401);
                }

                var result = await dashboardService.GetUserStatisticsAsync(userId, cancellationToken);

                return result.IsSuccess 
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Get User Statistics Failed",
                        detail: result.Error,
                        statusCode: result.Value == null ? 404 : 500);
            })
            .RequireAuthorization() // Requires JWT Bearer token authentication
            .WithName("GetUserStatistics")
            .WithSummary("Get current user's membership statistics")
            .WithDescription("Returns the current user's attendance history and membership metrics")
            .WithTags("Dashboard");
    }
}