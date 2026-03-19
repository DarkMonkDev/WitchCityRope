using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Features.TicketAssignment.Services;
using WitchCityRope.Api.Features.Users.Constants;

namespace WitchCityRope.Api.Features.TicketAssignment.Endpoints;

/// <summary>
/// Minimal API endpoints for ticket assignment operations.
/// Includes assign, accept, decline, reassign (EP-08 through EP-11)
/// and dashboard queries (EP-16, EP-17).
/// All endpoints require authentication. State-changing endpoints require CSRF validation.
/// </summary>
public static class TicketAssignmentEndpoints
{
    /// <summary>
    /// Register all ticket assignment endpoints using minimal API pattern.
    /// Follows the same patterns as AuthorizedContactEndpoints and ParticipationEndpoints.
    /// </summary>
    public static void MapTicketAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        // ============================================================================
        // EP-08: POST /api/events/{eventId}/tickets/{attendanceId}/assign
        // Assigns a ticket to an authorized contact (post-purchase assignment)
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/tickets/{attendanceId:guid}/assign",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                AssignTicketRequest request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first for state-changing operations
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

                var result = await assignmentService.AssignTicketAsync(
                    attendanceId, userId, request.AssignToUserId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Attendance not found" => 404,
                    "Assignee not found" => 404,
                    "Invalid attendance type" => 400,
                    "Ticket not assignable" => 400,
                    "Not authorized to assign" => 403,
                    "Not authorized by assignee" => 403,
                    "Assignee not vetted" => 403,
                    "Assignee already has ticket" => 409,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("AssignTicket")
            .WithSummary("Assign a ticket to an authorized contact")
            .WithDescription("Post-purchase assignment. The assignee (principal) must have authorized the caller (delegate). Ticket transitions to PendingAcceptance. BR-020, BR-012, BR-035.")
            .WithTags("TicketAssignment")
            .Produces<TicketAssignmentDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // EP-09: POST /api/events/{eventId}/tickets/{attendanceId}/accept
        // Accept an assigned ticket (waiver + ToS acceptance required)
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/tickets/{attendanceId:guid}/accept",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                AcceptAssignmentRequest request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation
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

                var result = await assignmentService.AcceptAssignmentAsync(
                    attendanceId, userId, request, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Assignment not found" => 404,
                    "Not the assigned user" => 403,
                    "Waiver not accepted" => 400,
                    "Vetting required" => 403,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("AcceptAssignment")
            .WithSummary("Accept an assigned ticket or proxy RSVP")
            .WithDescription("Transitions PendingAcceptance to Active. Requires waiver acceptance (AD-003). Re-checks vetting for vetted-only events (BR-036). Auto-creates RSVP for social events.")
            .WithTags("TicketAssignment")
            .Produces<TicketAssignmentDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // ============================================================================
        // EP-10: POST /api/events/{eventId}/tickets/{attendanceId}/decline
        // Decline an assigned ticket (optional reason)
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/tickets/{attendanceId:guid}/decline",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                DeclineAssignmentRequest? request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation
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

                var result = await assignmentService.DeclineAssignmentAsync(
                    attendanceId, userId, request?.Reason, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Assignment not found" => 404,
                    "Not the assigned user" => 403,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("DeclineAssignment")
            .WithSummary("Decline an assigned ticket")
            .WithDescription("Ticket reverts to Active status owned by original purchaser (AD-015). DeclinedAt is set for reassignment eligibility. Purchaser is notified.")
            .WithTags("TicketAssignment")
            .Produces<TicketAssignmentDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // ============================================================================
        // EP-11: POST /api/events/{eventId}/tickets/{attendanceId}/reassign
        // Reassign a previously declined ticket to a new authorized contact
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/tickets/{attendanceId:guid}/reassign",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                AssignTicketRequest request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation
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

                var result = await assignmentService.ReassignTicketAsync(
                    attendanceId, userId, request.AssignToUserId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Ticket not eligible for reassignment" => 400,
                    "Not the original purchaser" => 403,
                    "Not authorized by assignee" => 403,
                    "Assignee not found" => 404,
                    "Assignee not vetted" => 403,
                    "Assignee already has ticket" => 409,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("ReassignTicket")
            .WithSummary("Reassign a previously declined ticket to a new authorized contact")
            .WithDescription("Works on tickets that reverted to purchaser after decline (Status=Active, DeclinedAt not null). UC-008, BR-020, BR-012, BR-035.")
            .WithTags("TicketAssignment")
            .Produces<TicketAssignmentDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // EP-16: GET /api/user/pending-assignments
        // Dashboard: pending assignments for the current user
        // ============================================================================
        app.MapGet("/api/user/pending-assignments",
            [Authorize] async (
                ITicketAssignmentService assignmentService,
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

                var result = await assignmentService.GetPendingAssignmentsAsync(userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: result.Error,
                        detail: result.Details,
                        statusCode: 500);
            })
            .RequireAuthorization()
            .WithName("GetPendingAssignments")
            .WithSummary("Get pending ticket/RSVP assignments for the current user's dashboard")
            .WithDescription("Returns all EventAttendance records where UserId = current user AND Status = PendingAcceptance. Includes both ticket and RSVP types. Ordered by event date ascending.")
            .WithTags("TicketAssignment")
            .Produces<List<PendingAssignmentDto>>(200)
            .Produces(401)
            .Produces(500);

        // ============================================================================
        // EP-17: GET /api/user/assigned-tickets
        // Dashboard: tickets purchased by current user that were assigned to others
        // ============================================================================
        app.MapGet("/api/user/assigned-tickets",
            [Authorize] async (
                ITicketAssignmentService assignmentService,
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

                var result = await assignmentService.GetAssignedTicketsAsync(userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: result.Error,
                        detail: result.Details,
                        statusCode: 500);
            })
            .RequireAuthorization()
            .WithName("GetAssignedTickets")
            .WithSummary("Get tickets purchased by the current user that were assigned to others")
            .WithDescription("Shows assignment status and whether each ticket can be reassigned. CanReassign = true when Status=Active and DeclinedAt is not null (AD-015).")
            .WithTags("TicketAssignment")
            .Produces<List<AssignedTicketStatusDto>>(200)
            .Produces(401)
            .Produces(500);

        // ============================================================================
        // POST /api/tickets/{ticketPurchaseId}/assign
        // Assign an unassigned "assign later" ticket to an authorized contact.
        // These tickets have a TicketPurchase but no EventAttendance yet.
        // Creates EventAttendance records during assignment.
        // ============================================================================
        app.MapPost("/api/tickets/{ticketPurchaseId:guid}/assign",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid ticketPurchaseId,
                AssignTicketRequest request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation
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

                var result = await assignmentService.AssignUnassignedTicketAsync(
                    ticketPurchaseId, userId, request.AssignToUserId, cancellationToken);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                var statusCode = result.Error switch
                {
                    "Ticket not found" => 404,
                    "Assignee not found" => 404,
                    "Not authorized" => 403,
                    "Not authorized by contact" => 403,
                    "Ticket already assigned" => 409,
                    "Vetting required" => 403,
                    "Duplicate ticket" => 400,
                    "Ticket configuration error" => 400,
                    _ => 500
                };

                return Results.Problem(
                    title: "Assignment Failed",
                    detail: result.Details ?? result.Error ?? "Assignment failed",
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("AssignUnassignedTicket")
            .WithSummary("Assign an unassigned 'assign later' ticket to an authorized contact")
            .WithDescription("For tickets purchased with 'Assign later' that have no EventAttendance yet. Creates attendance records and sets PendingAcceptance status.")
            .WithTags("TicketAssignment")
            .Produces<TicketAssignmentDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409);

        // ============================================================================
        // EP-18: POST /api/admin/events/{eventId}/assign-ticket
        // Admin assigns a comp ticket to any user (bypasses authorized contacts)
        // ============================================================================
        app.MapPost("/api/admin/events/{eventId:guid}/assign-ticket",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                AdminAssignTicketRequest request,
                ITicketAssignmentService assignmentService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first for state-changing operations
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

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var adminUserId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await assignmentService.AdminAssignTicketAsync(
                    eventId, adminUserId, request, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created(
                        $"/api/admin/events/{eventId}/assignments",
                        result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Event not found" => 404,
                    "Ticket type not found" => 404,
                    "User not found" => 404,
                    "User not vetted" => 400,
                    "User already has ticket" => 409,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("AdminAssignTicket")
            .WithSummary("Admin assigns a comp ticket to any user (bypasses authorized contacts)")
            .WithDescription("Creates a comp TicketPurchase (TotalPrice=0, PaymentMethod='admin-comp') and PendingAcceptance EventAttendance. Recipient must still accept waiver (BR-041). Admin audit trail recorded (BR-042). BR-040 bypasses authorized contacts.")
            .WithTags("AdminTicketAssignment")
            .Produces<TicketAssignmentDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // EP-19: GET /api/admin/events/{eventId}/assignments
        // Admin view of all assignments for an event with full audit info
        // ============================================================================
        app.MapGet("/api/admin/events/{eventId:guid}/assignments",
            [Authorize] async (
                Guid eventId,
                ITicketAssignmentService assignmentService,
                CancellationToken cancellationToken) =>
            {
                var result = await assignmentService.GetEventAssignmentsAsync(eventId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Event not found" => 404,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("AdminGetEventAssignments")
            .WithSummary("Get all ticket/RSVP assignments for an event (admin view)")
            .WithDescription("Returns all EventAttendance records where AssignedByUserId IS NOT NULL. Includes all statuses (Active, PendingAcceptance, Cancelled). Full audit info with attendee/assigner scene names.")
            .WithTags("AdminTicketAssignment")
            .Produces<List<AdminEventAssignmentDto>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }
}
