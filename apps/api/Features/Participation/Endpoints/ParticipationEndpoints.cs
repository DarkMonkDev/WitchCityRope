using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Payments;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Endpoints;

/// <summary>
/// RSVP and participation minimal API endpoints
/// Follows vertical slice architecture with direct service injection
/// </summary>
public static class ParticipationEndpoints
{
    /// <summary>
    /// Register participation endpoints using minimal API pattern
    /// </summary>
    public static void MapParticipationEndpoints(this IEndpointRouteBuilder app)
    {
        // Get user's participation status for a specific event
        app.MapGet("/api/events/{eventId:guid}/participation",
            [Authorize] async (
                Guid eventId,
                [FromServices] IAttendanceService attendanceService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await attendanceService.GetParticipationStatusAsync(eventId, userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Failed to get participation status",
                        detail: result.Error,
                        statusCode: 500);
            })
            .WithName("GetParticipationStatus")
            .WithSummary("Get user's participation status for an event")
            .WithDescription("Returns enhanced participation status with hasRSVP/hasTicket flags, nested RSVP/ticket details, and capacity information")
            .WithTags("Participation")
            .Produces<EnhancedParticipationStatusDto?>(200)
            .Produces(401)
            .Produces(500);

        // ============================================================================
        // CREATE RSVP ENDPOINT
        // ============================================================================
        //
        // BUSINESS RULE: Creates standalone RSVP record (AttendanceType=RSVP)
        //
        // IMPORTANT: This creates ONLY an RSVP, NOT a ticket
        // - User can have BOTH active Ticket AND active RSVP
        // - Cancelled RSVPs do NOT block new RSVPs (allows re-registration)
        // - Query checks for ACTIVE RSVPs only (AttendanceType=RSVP + Status=Active)
        //
        // RELATIONSHIP TO TICKETS:
        // - If user already has a ticket, they can still RSVP separately
        // - If user later cancels ticket, the RSVP is also cancelled
        // - User can then manually RSVP again (creates NEW record)
        // SECURITY: CSRF protection prevents unauthorized event registrations
        app.MapPost("/api/events/{eventId:guid}/rsvp",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                CreateRSVPRequest request,
                [FromServices] IAttendanceService attendanceService,
                [FromServices] IVettingAccessControlService vettingAccessControlService,
                ClaimsPrincipal user,
                [FromServices] ILogger<IAttendanceService> logger,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.Problem(
                        title: "CSRF Validation Failed",
                        detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                        statusCode: 400);
                }

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                // Check vetting access control BEFORE processing RSVP
                var accessCheckResult = await vettingAccessControlService.CanUserRsvpAsync(userId, eventId, cancellationToken);

                if (!accessCheckResult.IsSuccess)
                {
                    logger.LogError(
                        "Vetting access check failed for user {UserId} on event {EventId}: {Error}",
                        userId, eventId, accessCheckResult.Error);

                    return Results.Problem(
                        title: "Access check failed",
                        detail: "Unable to verify RSVP eligibility at this time",
                        statusCode: 500);
                }

                var accessControl = accessCheckResult.Value!;

                if (!accessControl.IsAllowed)
                {
                    logger.LogWarning(
                        "User {UserId} denied RSVP access to event {EventId}. Reason: {Reason}, Status: {VettingStatus}",
                        userId, eventId, accessControl.DenialReason ?? "Unknown", accessControl.VettingStatus);

                    return Results.Json(new
                    {
                        error = accessControl.DenialReason,
                        message = accessControl.UserMessage,
                        vettingStatus = accessControl.VettingStatus?.ToString()
                    }, statusCode: 403);
                }

                // Ensure the eventId in URL matches the request
                request.EventId = eventId;

                logger.LogInformation(
                    "ENDPOINT DIAGNOSTIC: About to call CreateRSVPAsync for user {UserId} on event {EventId}",
                    userId, eventId);

                var result = await attendanceService.CreateRSVPAsync(request, userId, cancellationToken);

                logger.LogInformation(
                    "ENDPOINT DIAGNOSTIC: CreateRSVPAsync returned - IsSuccess: {IsSuccess}, Error: {Error}",
                    result.IsSuccess, result.Error ?? "none");

                if (!result.IsSuccess)
                {
                    // Check for specific business rule violations
                    if (result.Error.Contains("not found"))
                    {
                            return Results.Problem(
                            title: "Resource Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("already"))
                    {
                        return Results.Problem(
                            title: "Conflict",
                            detail: result.Error,
                            statusCode: 409);
                    }
                    if (result.Error.Contains("vetted") || result.Error.Contains("capacity") || result.Error.Contains("window"))
                    {
                        return Results.Problem(
                            title: "Bad Request",
                            detail: result.Error,
                            statusCode: 400);
                    }

                    return Results.Problem(
                        title: "Failed to create RSVP",
                        detail: result.Error,
                        statusCode: 500);
                }

                logger.LogInformation(
                    "ENDPOINT DIAGNOSTIC: Returning 201 Created for RSVP {AttendanceId} for user {UserId} on event {EventId}",
                    result.Value?.EventId, userId, eventId);

                return Results.Created($"/api/events/{eventId}/participation", result.Value);
            })
            .WithName("CreateRSVP")
            .WithSummary("Create RSVP for social event")
            .WithDescription("Creates an RSVP for a social event. Blocked for users with OnHold, Denied, or Withdrawn vetting status.")
            .WithTags("Participation")
            .Produces<ParticipationStatusDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // PURCHASE TICKET ENDPOINT
        // ============================================================================
        //
        // BUSINESS RULE: Creates ticket record (AttendanceType=Ticket)
        //
        // CRITICAL: For social events, also auto-creates RSVP record
        // - Creates TWO EventAttendances records:
        //   1. Ticket record (AttendanceType=Ticket, Status=Active)
        //   2. RSVP record (AttendanceType=RSVP, Status=Active) - for social events only
        //
        // WHY AUTO-RSVP:
        // - Users need to be on attendance roster for check-in
        // - Capacity tracking includes all attendees
        //
        // RESULT: User has BOTH active Ticket AND active RSVP simultaneously
        //
        // CANCELLATION:
        // - Cancelling ticket also cancels the associated RSVP
        // - User can manually RSVP again after cancelling (creates NEW record)
        //
        // ENDPOINT: POST /api/events/{eventId}/tickets - purchase ticket for class event
        // SECURITY: CSRF protection for financial transactions (ticket purchases)
        app.MapPost("/api/events/{eventId:guid}/tickets",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                CreateTicketPurchaseRequest request,
                [FromServices] IAttendanceService attendanceService,
                [FromServices] IVettingAccessControlService vettingAccessControlService,
                ClaimsPrincipal user,
                [FromServices] ILogger<IAttendanceService> logger,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first (financial transaction)
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.Problem(
                        title: "CSRF Validation Failed",
                        detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                        statusCode: 400);
                }

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                // Check vetting access control BEFORE processing ticket purchase
                var accessCheckResult = await vettingAccessControlService.CanUserPurchaseTicketAsync(userId, eventId, cancellationToken);

                if (!accessCheckResult.IsSuccess)
                {
                    logger.LogError(
                        "Vetting access check failed for user {UserId} on event {EventId}: {Error}",
                        userId, eventId, accessCheckResult.Error);

                    return Results.Problem(
                        title: "Access check failed",
                        detail: "Unable to verify ticket purchase eligibility at this time",
                        statusCode: 500);
                }

                var accessControl = accessCheckResult.Value!;

                if (!accessControl.IsAllowed)
                {
                    logger.LogWarning(
                        "User {UserId} denied ticket purchase access to event {EventId}. Reason: {Reason}, Status: {VettingStatus}",
                        userId, eventId, accessControl.DenialReason ?? "Unknown", accessControl.VettingStatus);

                    return Results.Json(new
                    {
                        error = accessControl.DenialReason,
                        message = accessControl.UserMessage,
                        vettingStatus = accessControl.VettingStatus?.ToString()
                    }, statusCode: 403);
                }

                // Ensure the eventId in URL matches the request
                request.EventId = eventId;

                var result = await attendanceService.CreateTicketPurchaseAsync(request, userId, cancellationToken);

                if (!result.IsSuccess)
                {
                    // Check for specific business rule violations
                    if (result.Error.Contains("not found"))
                    {
                            return Results.Problem(
                            title: "Resource Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("already"))
                    {
                        return Results.Problem(
                            title: "Conflict",
                            detail: result.Error,
                            statusCode: 409);
                    }
                    if (result.Error.Contains("capacity") || result.Error.Contains("only allowed") || result.Error.Contains("window") || result.Error.Contains("sessions") || result.Error.Contains("vetted"))
                    {
                        return Results.Problem(
                            title: "Bad Request",
                            detail: result.Error,
                            statusCode: 400);
                    }

                    return Results.Problem(
                        title: "Failed to purchase ticket",
                        detail: result.Error,
                        statusCode: 500);
                }

                return Results.Created($"/api/events/{eventId}/participation", result.Value);
            })
            .WithName("PurchaseTicket")
            .WithSummary("Purchase ticket for class event")
            .WithDescription("Purchases a ticket for a class event. Blocked for users with OnHold, Denied, or Withdrawn vetting status.")
            .WithTags("Participation")
            .Produces<ParticipationStatusDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // CANCEL PARTICIPATION ENDPOINT
        // ============================================================================
        //
        // BUSINESS RULE: Cancels attendance record(s)
        //
        // SUPPORTS TWO MODES:
        // 1. Ticket mode (ticketPurchaseIds in body): Cancels ALL sessions for specified ticket purchases
        //    - Also handles fallback when type=ticket but no IDs: looks up all active ticket purchases
        // 2. RSVP mode (type=rsvp or no type): Cancels RSVP only (no refunds, no ticket concerns)
        //
        // CRITICAL: Cancelling a TICKET also cancels associated RSVP
        // - Ticket purchases auto-create RSVP for social events
        // - Cancelling ticket removes RSVP to prevent orphaned RSVPs
        //
        // AFTER CANCELLATION:
        // - User can manually RSVP again (creates NEW record)
        // - Cancelled records remain in database for audit history
        app.MapDelete("/api/events/{eventId:guid}/participation",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                [FromServices] IAttendanceService attendanceService,
                [FromServices] ApplicationDbContext dbContext,
                ClaimsPrincipal user,
                [FromBody] CancelTicketRequest? request = null,
                string? type = null,
                string? reason = null,
                CancellationToken cancellationToken = default) =>
            {
                // CSRF validation - MUST be first
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.Problem(
                        title: "CSRF Validation Failed",
                        detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                        statusCode: 400);
                }

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                // Ticket cancellation: ticketPurchaseIds provided in body
                if (request?.TicketPurchaseIds != null && request.TicketPurchaseIds.Count > 0)
                {
                    var result = await attendanceService.CancelTicketPurchasesAsync(
                        eventId,
                        userId,
                        request.TicketPurchaseIds,
                        request.Reason,
                        cancellationToken);

                    if (!result.IsSuccess)
                    {
                        if (result.Error.Contains("not found") || result.Error.Contains("No active"))
                        {
                            return Results.Problem(
                                title: "Not Found",
                                detail: result.Error,
                                statusCode: 404);
                        }
                        if (result.Error.Contains("cannot be cancelled") || result.Error.Contains("Cancellation window"))
                        {
                            return Results.Problem(
                                title: "Cancellation Not Allowed",
                                detail: result.Error,
                                statusCode: 400);
                        }

                        return Results.Problem(
                            title: "Cancellation Failed",
                            detail: result.Error,
                            statusCode: 500);
                    }

                    return Results.NoContent();
                }

                // Ticket cancellation fallback: type=ticket but no IDs provided
                // Look up all active ticket purchase IDs for user+event and delegate to CancelTicketPurchasesAsync
                if (string.Equals(type, "ticket", StringComparison.OrdinalIgnoreCase))
                {
                    var activeTicketPurchaseIds = await dbContext.EventAttendances
                        .Where(ea => ea.EventId == eventId &&
                                    ea.UserId == userId &&
                                    ea.Status == AttendanceStatus.Active &&
                                    ea.AttendanceType == AttendanceType.Ticket &&
                                    ea.TicketPurchaseId.HasValue)
                        .Select(ea => ea.TicketPurchaseId!.Value)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    if (activeTicketPurchaseIds.Count == 0)
                    {
                        return Results.Problem(
                            title: "Not Found",
                            detail: "No active ticket purchases found for this event",
                            statusCode: 404);
                    }

                    var ticketResult = await attendanceService.CancelTicketPurchasesAsync(
                        eventId,
                        userId,
                        activeTicketPurchaseIds,
                        reason,
                        cancellationToken);

                    if (!ticketResult.IsSuccess)
                    {
                        if (ticketResult.Error.Contains("not found") || ticketResult.Error.Contains("No active"))
                        {
                            return Results.Problem(
                                title: "Not Found",
                                detail: ticketResult.Error,
                                statusCode: 404);
                        }
                        if (ticketResult.Error.Contains("cannot be cancelled") || ticketResult.Error.Contains("Cancellation window"))
                        {
                            return Results.Problem(
                                title: "Cancellation Not Allowed",
                                detail: ticketResult.Error,
                                statusCode: 400);
                        }

                        return Results.Problem(
                            title: "Cancellation Failed",
                            detail: ticketResult.Error,
                            statusCode: 500);
                    }

                    return Results.NoContent();
                }

                // RSVP cancellation: type=rsvp or no type specified
                var rsvpResult = await attendanceService.CancelRsvpAsync(eventId, userId, reason, cancellationToken);

                if (!rsvpResult.IsSuccess)
                {
                    if (rsvpResult.Error.Contains("not found") || rsvpResult.Error.Contains("No active"))
                    {
                        return Results.Problem(
                            title: "Not Found",
                            detail: rsvpResult.Error,
                            statusCode: 404);
                    }
                    if (rsvpResult.Error.Contains("cannot be cancelled") || rsvpResult.Error.Contains("Cancellation window") || rsvpResult.Error.Contains("not currently open"))
                    {
                        return Results.Problem(
                            title: "Cancellation Not Allowed",
                            detail: rsvpResult.Error,
                            statusCode: 400);
                    }

                    return Results.Problem(
                        title: "Cancellation Failed",
                        detail: rsvpResult.Error,
                        statusCode: 500);
                }

                return Results.NoContent();
            })
            .WithName("CancelParticipation")
            .WithSummary("Cancel participation in event")
            .WithDescription("Cancels the user's participation (RSVP or ticket) in the specified event")
            .WithTags("Participation")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Cancel attendance by attendance ID (for tests compatibility)
        app.MapDelete("/api/attendance/{attendanceId:guid}",
            [Authorize] async (
                Guid attendanceId,
                [FromServices] ApplicationDbContext context,
                [FromServices] IAttendanceService attendanceService,
                ClaimsPrincipal user,
                [FromServices] ILogger<IAttendanceService> logger,
                string? reason = null,
                CancellationToken cancellationToken = default) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                // Get the attendance record to find eventId
                var attendance = await context.EventAttendances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ea => ea.Id == attendanceId, cancellationToken);

                if (attendance == null)
                {
                    return Results.Problem(
                        title: "Not Found",
                        detail: "Attendance record not found",
                        statusCode: 404);
                }

                // Verify user owns this attendance
                if (attendance.UserId != userId)
                {
                    return Results.Problem(
                        title: "Forbidden",
                        detail: "You can only cancel your own attendance",
                        statusCode: 403);
                }

                // Route to the correct service method based on attendance type
                WitchCityRope.Api.Features.Shared.Models.Result result;

                if (attendance.AttendanceType == AttendanceType.Ticket)
                {
                    // For tickets, get the TicketPurchaseId and delegate to CancelTicketPurchasesAsync
                    if (!attendance.TicketPurchaseId.HasValue)
                    {
                        return Results.Problem(
                            title: "Bad Request",
                            detail: "Ticket attendance has no associated ticket purchase",
                            statusCode: 400);
                    }

                    result = await attendanceService.CancelTicketPurchasesAsync(
                        attendance.EventId,
                        userId,
                        new List<Guid> { attendance.TicketPurchaseId.Value },
                        reason,
                        cancellationToken);
                }
                else
                {
                    // For RSVPs, use the focused CancelRsvpAsync
                    result = await attendanceService.CancelRsvpAsync(
                        attendance.EventId,
                        userId,
                        reason,
                        cancellationToken);
                }

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found") || result.Error.Contains("No active"))
                    {
                        return Results.Problem(
                            title: "Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("cannot be cancelled") || result.Error.Contains("not currently open") || result.Error.Contains("Cancellation window"))
                    {
                        return Results.Problem(
                            title: "Cancellation Not Allowed",
                            detail: result.Error,
                            statusCode: 400);
                    }

                    return Results.Problem(
                        title: "Cancellation Failed",
                        detail: result.Error,
                        statusCode: 500);
                }

                return Results.NoContent();
            })
            .WithName("CancelAttendanceById")
            .WithSummary("Cancel attendance by ID")
            .WithDescription("Cancels an attendance record by its ID (RSVP or ticket)")
            .WithTags("Participation")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Get user's participations
        app.MapGet("/api/user/participations",
            [Authorize] async (
                [FromServices] IAttendanceService attendanceService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await attendanceService.GetUserParticipationsAsync(userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Failed to get user participations",
                        detail: result.Error,
                        statusCode: 500);
            })
            .WithName("GetUserParticipations")
            .WithSummary("Get user's event participations")
            .WithDescription("Returns all current participations (RSVPs and tickets) for the authenticated user")
            .WithTags("Participation")
            .Produces<List<UserParticipationDto>>(200)
            .Produces(401)
            .Produces(500);

        // Cancel RSVP endpoint
        app.MapDelete("/api/events/{eventId:guid}/rsvp",
            [Authorize] async (
                Guid eventId,
                [FromServices] IAttendanceService attendanceService,
                ClaimsPrincipal user,
                string? reason = null,
                CancellationToken cancellationToken = default) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await attendanceService.CancelRsvpAsync(eventId, userId, reason, cancellationToken);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found") || result.Error.Contains("No active"))
                    {
                        return Results.Problem(
                            title: "Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("cannot be cancelled") || result.Error.Contains("not currently open"))
                    {
                        return Results.Problem(
                            title: "Cancellation Not Allowed",
                            detail: result.Error,
                            statusCode: 400);
                    }

                    return Results.Problem(
                        title: "Cancellation Failed",
                        detail: result.Error,
                        statusCode: 500);
                }

                return Results.NoContent();
            })
            .WithName("CancelRSVP")
            .WithSummary("Cancel RSVP for event")
            .WithDescription("Cancels the user's RSVP for the specified event. RSVP-only, no refunds or ticket concerns.")
            .WithTags("Participation")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Admin endpoint: Get all participations for an event
        // Allows Administrator and EventOrganizer to view event participation data
        app.MapGet("/api/admin/events/{eventId:guid}/participations",
            [Authorize(Roles = "Administrator,EventOrganizer")] async (
                Guid eventId,
                [FromServices] IAttendanceService attendanceService,
                CancellationToken cancellationToken) =>
            {
                var result = await attendanceService.GetEventParticipationsAsync(eventId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value); // Direct DTO list
                }

                return Results.Problem(
                    title: "Failed to get event participations",
                    detail: result.Error ?? "Failed to get event participations",
                    statusCode: 500);
            })
            .WithName("GetEventParticipations")
            .WithSummary("Get all participations for an event (admin only)")
            .WithDescription("Returns all RSVPs and ticket purchases for the specified event. Admin role required.")
            .WithTags("Admin", "Participation")
            .Produces<List<EventParticipationDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // Admin endpoint: Remove user's attendance (RSVP or Ticket) - Simple removal without cascading
        // Allows Administrator and EventOrganizer — removal is audited via UpdatedBy and CancellationReason fields
        app.MapDelete("/api/admin/events/{eventId:guid}/participations/{userId:guid}",
            [Authorize(Roles = "Administrator,EventOrganizer")] async (
                Guid eventId,
                Guid userId,
                [FromServices] ApplicationDbContext context,
                ClaimsPrincipal user,
                [FromServices] ILogger<IAttendanceService> logger,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var adminUserId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "Admin authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                logger.LogInformation(
                    "Admin {AdminUserId} removing attendance for user {UserId} from event {EventId}",
                    adminUserId, userId, eventId);

                // Find any active attendance for this user and event
                var attendance = await context.EventAttendances
                    .FirstOrDefaultAsync(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.Status == AttendanceStatus.Active,
                        cancellationToken);

                if (attendance == null)
                {
                    return Results.Problem(
                        title: "Attendance Not Found",
                        detail: "No active attendance found for this user and event",
                        statusCode: 404);
                }

                // Mark attendance as cancelled
                attendance.Status = AttendanceStatus.Cancelled;
                attendance.CancelledAt = DateTime.UtcNow;
                attendance.CancellationReason = $"Removed by admin {adminUserId}";
                attendance.UpdatedBy = adminUserId;
                attendance.UpdatedAt = DateTime.UtcNow;
                context.EventAttendances.Update(attendance);

                // Update EventAttendee status if no OTHER active attendances remain
                // Use Local to check change tracker since we haven't saved yet
                var hasOtherActiveAttendance = context.EventAttendances.Local
                    .Any(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.Id != attendance.Id &&
                        ea.Status == AttendanceStatus.Active) ||
                    await context.EventAttendances
                        .AnyAsync(ea =>
                            ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Id != attendance.Id &&
                            ea.Status == AttendanceStatus.Active,
                            cancellationToken);

                if (!hasOtherActiveAttendance)
                {
                    var eventAttendee = await context.EventAttendees
                        .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

                    if (eventAttendee != null)
                    {
                        eventAttendee.RegistrationStatus = "cancelled";
                        eventAttendee.UpdatedAt = DateTime.UtcNow;
                        context.EventAttendees.Update(eventAttendee);
                    }
                }

                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Admin {AdminUserId} successfully removed {AttendanceType} attendance for user {UserId} from event {EventId}",
                    adminUserId, attendance.AttendanceType, userId, eventId);

                return Results.NoContent();
            })
            .WithName("AdminRemoveParticipation")
            .WithSummary("Remove user's attendance (admin only)")
            .WithDescription("Removes user's attendance (RSVP or ticket) from event. Does not process refunds - use refund endpoint separately for paid tickets. Admin role required.")
            .WithTags("Admin", "Participation")
            .Produces(204)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Admin endpoint: Remove RSVP with cascading effects
        // Allows Administrator and EventOrganizer — removal is audited via logger and database fields
        app.MapDelete("/api/admin/events/{eventId:guid}/participations/{userId:guid}/remove",
            [Authorize(Roles = "Administrator,EventOrganizer")] async (
                Guid eventId,
                Guid userId,
                [FromServices] ApplicationDbContext context,
                [FromServices] IRefundService refundService,
                [FromServices] IVolunteerAssignmentService volunteerAssignmentService,
                ClaimsPrincipal user,
                [FromServices] ILogger<IAttendanceService> logger,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var adminUserId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "Admin authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                logger.LogInformation(
                    "Admin {AdminUserId} removing RSVP for user {UserId} from event {EventId}",
                    adminUserId, userId, eventId);

                // Find RSVP participation
                var rsvpParticipation = await context.EventAttendances
                    .FirstOrDefaultAsync(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.AttendanceType == AttendanceType.RSVP &&
                        ea.Status == AttendanceStatus.Active,
                        cancellationToken);

                if (rsvpParticipation == null)
                {
                    return Results.Problem(
                        title: "RSVP Not Found",
                        detail: "No active RSVP found for this user and event",
                        statusCode: 404);
                }

                var response = new AdminRemoveRsvpResponse
                {
                    RsvpRemoved = false,
                    TicketRefunded = false,
                    VolunteerShiftsRemoved = false,
                    VolunteerShiftNames = new List<string>()
                };

                // Fetch volunteer shift names BEFORE cancellation (for response details)
                var volunteerSignups = await context.VolunteerSignups
                    .Include(vs => vs.VolunteerPosition)
                    .Where(vs => vs.UserId == userId &&
                                vs.VolunteerPosition.EventId == eventId &&
                                vs.Status == VolunteerSignupStatus.Confirmed)
                    .ToListAsync(cancellationToken);

                response.VolunteerShiftNames = volunteerSignups
                    .Select(vs => vs.VolunteerPosition.Title)
                    .ToList();

                // Check if user also has a ticket
                var ticketParticipation = await context.EventAttendances
                    .FirstOrDefaultAsync(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.AttendanceType == AttendanceType.Ticket &&
                        ea.Status == AttendanceStatus.Active,
                        cancellationToken);

                // If ticket exists, process refund (which will auto-cancel volunteer shifts)
                if (ticketParticipation != null)
                {
                    // Find associated ticket purchase via EventAttendance link
                    if (ticketParticipation.TicketPurchaseId.HasValue)
                    {
                        var ticketPurchase = await context.TicketPurchases
                            .FirstOrDefaultAsync(tp => tp.Id == ticketParticipation.TicketPurchaseId.Value,
                                cancellationToken);

                        if (ticketPurchase != null && ticketPurchase.IsPaymentCompleted)
                        {
                            // Process refund (RefundService auto-cancels volunteer shifts)
                            var refundRequest = new ProcessRefundRequest
                            {
                                TicketPurchaseId = ticketPurchase.Id,
                                RefundAmount = Money.Create(ticketPurchase.TotalPrice, PaymentConstants.Currency),
                                RefundReason = $"Admin removed RSVP for user {userId}",
                                ProcessedByUserId = adminUserId,
                                IpAddress = "admin-action"
                            };

                            var refundResult = await refundService.ProcessRefundAsync(refundRequest, cancellationToken);

                            if (!refundResult.IsSuccess)
                            {
                                logger.LogError(
                                    "Failed to process refund for ticketPurchase {TicketPurchaseId}: {Error}",
                                    ticketPurchase.Id, refundResult.ErrorMessage);

                                return Results.Problem(
                                    title: "Refund Failed",
                                    detail: $"Failed to process ticket refund: {refundResult.ErrorMessage}",
                                    statusCode: 500);
                            }

                            // Update TicketPurchase.PaymentStatus (RefundService delegates this to callers)
                            ticketPurchase.PaymentStatus = TicketPurchasePaymentStatus.Refunded;
                            ticketPurchase.UpdatedAt = DateTime.UtcNow;

                            response.TicketRefunded = true;
                            response.RefundAmount = ticketPurchase.TotalPrice;
                            response.VolunteerShiftsRemoved = volunteerSignups.Count > 0;
                        }
                    }
                }
                else
                {
                    // No ticket - manually cancel volunteer shifts
                    var cancellationResult = await volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                        userId,
                        eventId,
                        "Admin removed RSVP",
                        cancellationToken);

                    if (cancellationResult.success)
                    {
                        response.VolunteerShiftsRemoved = cancellationResult.cancelledCount > 0;
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to cancel volunteer signups for user {UserId} at event {EventId}: {Error}",
                            userId, eventId, cancellationResult.error);
                    }
                }

                // Remove RSVP
                rsvpParticipation.Status = AttendanceStatus.Cancelled;
                rsvpParticipation.CancelledAt = DateTime.UtcNow;
                rsvpParticipation.CancellationReason = $"Removed by admin {adminUserId}";
                rsvpParticipation.UpdatedBy = adminUserId;
                context.EventAttendances.Update(rsvpParticipation);

                // Update EventAttendee status if no active attendances remain
                var hasActiveAttendance = await context.EventAttendances
                    .AnyAsync(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.Status == AttendanceStatus.Active,
                        cancellationToken);

                if (!hasActiveAttendance)
                {
                    var eventAttendee = await context.EventAttendees
                        .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

                    if (eventAttendee != null)
                    {
                        eventAttendee.RegistrationStatus = "cancelled";
                        eventAttendee.UpdatedAt = DateTime.UtcNow;
                        context.EventAttendees.Update(eventAttendee);
                    }
                }

                await context.SaveChangesAsync(cancellationToken);

                response.RsvpRemoved = true;

                logger.LogInformation(
                    "Admin {AdminUserId} successfully removed RSVP for user {UserId} from event {EventId}. " +
                    "Ticket refunded: {TicketRefunded}, Volunteer shifts removed: {VolunteerShiftsRemoved}",
                    adminUserId, userId, eventId, response.TicketRefunded, response.VolunteerShiftsRemoved);

                return Results.Ok(response);
            })
            .WithName("AdminRemoveRsvp")
            .WithSummary("Remove RSVP with cascading effects (admin only)")
            .WithDescription("Removes user's RSVP and processes ticket refund if exists. Auto-cancels volunteer shifts. Admin role required.")
            .WithTags("Admin", "Participation")
            .Produces<AdminRemoveRsvpResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }
}