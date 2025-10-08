using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FluentValidation;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.CheckIn.Validation;

namespace WitchCityRope.Api.Features.CheckIn.Endpoints;

/// <summary>
/// CheckIn minimal API endpoints
/// Mobile-optimized for event staff check-in operations
/// </summary>
public static class CheckInEndpoints
{
    /// <summary>
    /// Register CheckIn endpoints using minimal API pattern
    /// </summary>
    public static void MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checkin")
            .WithTags("CheckIn")
            .RequireAuthorization(); // All endpoints require authentication

        // Get attendees for check-in interface
        group.MapGet("/events/{eventId}/attendees", async (
            Guid eventId,
            ICheckInService checkInService,
            string? search,
            string? status,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            var result = await checkInService.GetEventAttendeesAsync(
                eventId, search, status, page, pageSize, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Get Attendees Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("GetEventAttendees")
        .WithSummary("Get attendees for event check-in")
        .WithDescription("Returns attendees list with search and filtering for check-in interface");

        // Process check-in
        group.MapPost("/events/{eventId}/checkin", async (
            Guid eventId,
            CheckInRequest request,
            ICheckInService checkInService,
            IValidator<CheckInRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Ensure staff member ID matches authenticated user
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || request.StaffMemberId != userId)
            {
                return Results.Problem(
                    title: "Authorization Failed",
                    detail: "Staff member ID must match authenticated user",
                    statusCode: 403);
            }

            var result = await checkInService.CheckInAttendeeAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Check-in Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("not found") ? 404 :
                               result.Error.Contains("capacity") ? 409 : 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("ProcessCheckIn")
        .WithSummary("Process attendee check-in")
        .WithDescription("Check in an attendee for the event with capacity validation");

        // Get event dashboard
        group.MapGet("/events/{eventId}/dashboard", async (
            Guid eventId,
            ICheckInService checkInService,
            CancellationToken cancellationToken = default) =>
        {
            var result = await checkInService.GetEventDashboardAsync(eventId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Get Dashboard Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("not found") ? 404 : 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("GetEventDashboard")
        .WithSummary("Get event check-in dashboard")
        .WithDescription("Returns real-time check-in statistics and recent activity");

        // Sync offline check-ins
        group.MapPost("/events/{eventId}/sync", async (
            Guid eventId,
            SyncRequest request,
            ISyncService syncService,
            IValidator<SyncRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await syncService.ProcessOfflineSyncAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Sync Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("SyncOfflineCheckIns")
        .WithSummary("Sync offline check-in data")
        .WithDescription("Process pending check-ins from offline operation with conflict detection");

        // Create manual entry
        group.MapPost("/events/{eventId}/manual-entry", async (
            Guid eventId,
            ManualEntryData request,
            ICheckInService checkInService,
            IValidator<ManualEntryData> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Get staff member ID from token
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var staffMemberId))
            {
                return Results.Problem(
                    title: "Authorization Failed",
                    detail: "Unable to identify staff member from token",
                    statusCode: 403);
            }

            var result = await checkInService.CreateManualEntryAsync(eventId, request, staffMemberId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Manual Entry Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("already registered") ? 409 : 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("CreateManualEntry")
        .WithSummary("Create manual entry for walk-in attendee")
        .WithDescription("Register and check in a walk-in attendee who isn't pre-registered");

        // Get pending sync count
        group.MapGet("/sync/pending-count", async (
            ISyncService syncService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
        {
            // Get user ID from token
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Problem(
                    title: "Authorization Failed",
                    detail: "Unable to identify user from token",
                    statusCode: 403);
            }

            var result = await syncService.GetPendingSyncCountAsync(userGuid, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { pendingCount = result.Value })
                : Results.Problem(
                    title: "Get Pending Count Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"))
        .WithName("GetPendingSyncCount")
        .WithSummary("Get pending sync operations count")
        .WithDescription("Returns the number of pending offline operations for the current user");
    }
}