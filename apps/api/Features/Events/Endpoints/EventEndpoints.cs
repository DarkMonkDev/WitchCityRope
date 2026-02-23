using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Events minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class EventEndpoints
{
    /// <summary>
    /// Register events endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all events with optional admin access and past events filter
        app.MapGet("/api/events", async (
            [FromServices] IEventService eventService,
            HttpContext context,
            bool? includeUnpublished,
            bool? includePastEvents,
            CancellationToken cancellationToken) =>
            {
                // Check if user is requesting unpublished events
                var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);
                var shouldIncludePastEvents = includePastEvents.GetValueOrDefault(false);

                // If requesting unpublished events, verify admin role
                if (shouldIncludeUnpublished)
                {
                    var user = context.User;
                    if (user.Identity?.IsAuthenticated != true)
                    {
                        return Results.Problem(
                            title: "Authentication Required",
                            detail: "Authentication required to access unpublished events",
                            statusCode: 401);
                    }

                    var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (userRole != "Administrator")
                    {
                        return Results.Problem(
                            title: "Insufficient Permissions",
                            detail: "Administrator role required to access unpublished events",
                            statusCode: 403);
                    }
                }

                var (success, response, error) = await eventService.GetEventsAsync(shouldIncludeUnpublished, shouldIncludePastEvents, cancellationToken);

                if (success)
                {
                    return Results.Ok(response); // Direct DTO list
                }

                // Return proper error - NO FALLBACK DATA
                return Results.Problem(
                    title: "Failed to retrieve events",
                    detail: error ?? "Unable to retrieve events. Please check if the API database connection is working.",
                    statusCode: 500);
            })
            .WithName("GetEvents")
            .WithSummary("Get all events")
            .WithDescription("Returns events from the database. Use ?includeUnpublished=true for admin access to draft events (requires Administrator role). Use ?includePastEvents=true to include past events from the last 90 days.")
            .WithTags("Events")
            .Produces<List<EventDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // Get lightweight event list for admin table
        app.MapGet("/api/events/list", async (
            [FromServices] IEventService eventService,
            HttpContext context,
            bool? includeUnpublished,
            bool? includePastEvents,
            CancellationToken cancellationToken) =>
            {
                var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);
                var shouldIncludePastEvents = includePastEvents.GetValueOrDefault(false);

                if (shouldIncludeUnpublished)
                {
                    var user = context.User;
                    if (user.Identity?.IsAuthenticated != true)
                    {
                        return Results.Problem(
                            title: "Authentication Required",
                            detail: "Authentication required to access unpublished events",
                            statusCode: 401);
                    }

                    var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (userRole != "Administrator")
                    {
                        return Results.Problem(
                            title: "Insufficient Permissions",
                            detail: "Administrator role required to access unpublished events",
                            statusCode: 403);
                    }
                }

                var (success, response, error) = await eventService.GetEventListAsync(shouldIncludeUnpublished, shouldIncludePastEvents, cancellationToken);

                if (success)
                {
                    return Results.Ok(response);
                }

                return Results.Problem(
                    title: "Failed to retrieve event list",
                    detail: error ?? "Unable to retrieve event list.",
                    statusCode: 500);
            })
            .WithName("GetEventList")
            .WithSummary("Get lightweight event list for admin table")
            .WithDescription("Returns a lightweight list of events with SQL-level count projections. Optimized for the admin events table view.")
            .WithTags("Events")
            .Produces<List<EventListItemDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // Get single event by ID
        app.MapGet("/api/events/{id}", async (
            string id,
            [FromServices] IEventService eventService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await eventService.GetEventAsync(id, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(response); // Direct DTO
                }

                // Return proper error - NO FALLBACK DATA
                return Results.Problem(
                    title: response == null ? "Event Not Found" : "Failed to retrieve event",
                    detail: error ?? (response == null ? "Event not found" : "Failed to retrieve event from database"),
                    statusCode: response == null ? 404 : 500);
            })
            .WithName("GetEvent")
            .WithSummary("Get single event by ID")
            .WithDescription("Returns a specific event by its unique identifier")
            .WithTags("Events")
            .Produces<EventDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // Create new event
        // SECURITY: CSRF protection + Authorization required
        app.MapPost("/api/events", async (
            HttpContext context,
            IAntiforgery antiforgery,
            CreateEventRequest request,
            [FromServices] IEventService eventService,
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

                // Call service to create event
                var (success, response, error) = await eventService.CreateEventAsync(request, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(response); // Direct DTO - Pattern B
                }

                // Determine appropriate HTTP status code based on error message
                var statusCode = error switch
                {
                    string msg when msg.Contains("null") => 400,
                    string msg when msg.Contains("date") => 400,
                    string msg when msg.Contains("Invalid event type") => 400,
                    _ => 500
                };

                return Results.Problem(
                    title: "Failed to create event",
                    detail: error ?? "Failed to create event. Please try again.",
                    statusCode: statusCode);
            })
            .RequireAuthorization() // Requires JWT authentication
            .WithName("CreateEvent")
            .WithSummary("Create a new event")
            .WithDescription("Creates a new event with optional sessions, ticket types, volunteer positions, and organizers. " +
                "New events are created as drafts (IsPublished = false) by default. Supports full event creation with all related entities in a single request.")
            .WithTags("Events")
            .Produces<EventDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500);

        // Update existing event by ID
        // SECURITY: CSRF protection prevents unauthorized event modifications
        app.MapPut("/api/events/{id}", async (
            HttpContext context,
            IAntiforgery antiforgery,
            string id,
            UpdateEventRequest request,
            [FromServices] IEventService eventService,
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

                var (success, response, error) = await eventService.UpdateEventAsync(id, request, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(response); // Direct DTO
                }

                // Determine appropriate HTTP status code based on error message
                var statusCode = error switch
                {
                    string msg when msg.Contains("not found") => 404,
                    string msg when msg.Contains("Invalid event ID") => 400,
                    string msg when msg.Contains("started more than") => 400, // Grace period exceeded
                    string msg when msg.Contains("capacity") => 400,
                    string msg when msg.Contains("date") => 400,
                    string msg when msg.Contains("null") => 400,
                    _ => 500
                };

                return Results.Problem(
                    title: "Failed to update event",
                    detail: error ?? "Failed to update event",
                    statusCode: statusCode);
            })
            .RequireAuthorization() // Requires JWT authentication
            .WithName("UpdateEvent")
            .WithSummary("Update an existing event")
            .WithDescription("Updates an event with the provided data. Supports partial updates (only non-null fields will be updated). " +
                "Business rules: Cannot update past events, cannot reduce capacity below current attendance.")
            .WithTags("Events")
            .Produces<EventDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // Copy an existing event with new date and title
        // SECURITY: CSRF protection + Admin authorization required
        app.MapPost("/api/events/{id}/copy", async (
            HttpContext context,
            IAntiforgery antiforgery,
            string id,
            CopyEventRequest request,
            [FromServices] IEventService eventService,
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

                // Call service to perform copy operation
                var (success, response, error) = await eventService.CopyEventAsync(id, request, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(response); // Direct DTO - Pattern B
                }

                // Determine appropriate HTTP status code based on error message
                var statusCode = error switch
                {
                    string msg when msg.Contains("not found") || msg.Contains("not found") => 404,
                    string msg when msg.Contains("Invalid event ID") => 400,
                    _ => 500
                };

                return Results.Problem(
                    title: statusCode == 404 ? "Event Not Found" : "Failed to copy event",
                    detail: error ?? "Failed to copy event. Please try again.",
                    statusCode: statusCode);
            })
            .RequireAuthorization() // Requires JWT authentication and Admin role
            .WithName("CopyEvent")
            .WithSummary("Copy an existing event")
            .WithDescription("Creates a copy of an event with a new date and title. " +
                "Deep copies all related entities (sessions, ticket types, volunteer positions, organizers, email templates). " +
                "Excludes attendance and transaction data. New event created as draft (IsPublished = false).")
            .WithTags("Events")
            .Produces<EventDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // Get ticket types for an event (used by check-in kiosk for door payments)
        app.MapGet("/api/events/{id}/ticket-types", async (
            string id,
            HttpContext context,
            [FromServices] IEventService eventService,
            CancellationToken cancellationToken) =>
            {
                // Kiosk mode: Check for X-CheckIn-Token header
                var sessionToken = context.Request.Headers["X-CheckIn-Token"].FirstOrDefault();
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return Results.Problem(
                        title: "Missing check-in session token",
                        detail: "X-CheckIn-Token header required for kiosk access",
                        statusCode: 401);
                }

                // Get event with ticket types
                var (success, response, error) = await eventService.GetEventAsync(id, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(response.TicketTypes); // Direct DTO list
                }

                // Return proper error
                return Results.Problem(
                    title: response == null ? "Event not found" : "Failed to retrieve ticket types",
                    detail: error ?? (response == null ? "Event not found" : "Failed to retrieve ticket types from database"),
                    statusCode: response == null ? 404 : 500);
            })
            .WithName("GetEventTicketTypes")
            .WithSummary("Get ticket types for an event")
            .WithDescription("Returns all ticket types for a specific event. Used by check-in kiosk for door payment processing. Requires X-CheckIn-Token header.")
            .WithTags("Events")
            .Produces<List<TicketTypeDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // Record cash payment for event attendee (kiosk mode door payment)
        app.MapPost("/api/events/{eventId}/checkin/cash-payment", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            CashPaymentRequest request,
            [FromServices] ICheckInService checkInService,
            [FromServices] IValidator<CashPaymentRequest> validator,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // EXTRACT STAFF ID FROM TOKEN - Frontend can't provide this
            request.RecordedByStaffId = tokenData.CreatedByStaffId;

            // Validate request (now includes RecordedByStaffId from token)
            var requestValidation = await validator.ValidateAsync(request, cancellationToken);
            if (!requestValidation.IsValid)
            {
                return Results.ValidationProblem(requestValidation.ToDictionary());
            }

            // TOKEN IS VALID - Proceed with cash payment recording
            var result = await checkInService.RecordCashPaymentAsync(eventId, request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Cash Payment Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("not found") ? 404 :
                               result.Error.Contains("already has a ticket") ? 409 : 500);
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("RecordEventCashPayment")
        .WithSummary("Record door cash payment for event attendee")
        .WithDescription("Creates a TicketPurchase record for cash payment at event door with staff attribution. Uses event-centric routing pattern.")
        .WithTags("Events", "CheckIn")
        .Produces<CashPaymentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // Get all sessions for an event
        app.MapGet("/api/events/{eventId}/sessions", async (
            string eventId,
            [FromServices] WitchCityRope.Api.Data.ApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(eventId, out var eventGuid))
            {
                return Results.Problem(
                    title: "Invalid Event ID",
                    detail: "The event ID must be a valid GUID",
                    statusCode: 400);
            }

            var eventExists = await context.Events.AnyAsync(e => e.Id == eventGuid, cancellationToken);
            if (!eventExists)
            {
                return Results.Problem(
                    title: "Event Not Found",
                    detail: $"Event with ID {eventId} was not found",
                    statusCode: 404);
            }

            var sessions = await context.Sessions
                .AsNoTracking()
                .Where(s => s.EventId == eventGuid)
                .OrderBy(s => s.StartTime)
                .Select(s => new SessionDto(s))
                .ToListAsync(cancellationToken);

            return Results.Ok(sessions);
        })
        .WithName("GetEventSessions")
        .WithSummary("Get all sessions for an event")
        .WithDescription("Returns a list of sessions for the specified event, ordered by start time")
        .WithTags("Events", "Sessions")
        .Produces<List<SessionDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(404);

        // Check if session can be deleted
        app.MapGet("/api/events/{eventId}/sessions/{sessionId}/can-delete", async (
            string eventId,
            string sessionId,
            [FromServices] IEventService eventService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require Event Organizer or Admin role
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be authenticated to check session deletion",
                    statusCode: 401);
            }

            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Administrator" && userRole != "EventOrganizer")
            {
                return Results.Problem(
                    title: "Insufficient Permissions",
                    detail: "Administrator or Event Organizer role required",
                    statusCode: 403);
            }

            var (success, response, error) = await eventService.CheckSessionDeletionAsync(eventId, sessionId, cancellationToken);

            if (!success || response == null)
            {
                return Results.Problem(
                    title: "Failed to check session deletion",
                    detail: error,
                    statusCode: error.Contains("not found") ? 404 : 500);
            }

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("CheckSessionDeletion")
        .WithSummary("Check if a session can be deleted")
        .WithDescription("Returns information about what would be affected if the session is deleted")
        .WithTags("Events", "Sessions")
        .Produces<DeleteSessionCheckDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete session
        app.MapDelete("/api/events/{eventId}/sessions/{sessionId}", async (
            string eventId,
            string sessionId,
            [FromServices] IEventService eventService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require Event Organizer or Admin role
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be authenticated to delete sessions",
                    statusCode: 401);
            }

            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Administrator" && userRole != "EventOrganizer")
            {
                return Results.Problem(
                    title: "Insufficient Permissions",
                    detail: "Administrator or Event Organizer role required",
                    statusCode: 403);
            }

            var (success, response, error) = await eventService.DeleteSessionAsync(eventId, sessionId, cancellationToken);

            if (!success || response == null)
            {
                var statusCode = error.Contains("not found") ? 404 :
                                error.Contains("Cannot delete") ? 400 : 500;

                return Results.Problem(
                    title: "Failed to delete session",
                    detail: error,
                    statusCode: statusCode);
            }

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("DeleteSession")
        .WithSummary("Delete a session")
        .WithDescription("Deletes a session and cancels associated RSVPs and volunteer signups. Deletes single-session ticket types.")
        .WithTags("Events", "Sessions")
        .Produces<DeleteSessionResultDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Check if ticket type can be deleted
        app.MapGet("/api/events/{eventId}/ticket-types/{ticketTypeId}/can-delete", async (
            string eventId,
            string ticketTypeId,
            [FromServices] IEventService eventService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require Event Organizer or Admin role
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be authenticated to check ticket type deletion",
                    statusCode: 401);
            }

            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Administrator" && userRole != "EventOrganizer")
            {
                return Results.Problem(
                    title: "Insufficient Permissions",
                    detail: "Administrator or Event Organizer role required",
                    statusCode: 403);
            }

            var (success, response, error) = await eventService.CheckTicketTypeDeletionAsync(eventId, ticketTypeId, cancellationToken);

            if (!success || response == null)
            {
                return Results.Problem(
                    title: "Failed to check ticket type deletion",
                    detail: error,
                    statusCode: error.Contains("not found") ? 404 : 500);
            }

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("CheckTicketTypeDeletion")
        .WithSummary("Check if a ticket type can be deleted")
        .WithDescription("Returns information about whether the ticket type has sales")
        .WithTags("Events", "TicketTypes")
        .Produces<DeleteTicketTypeCheckDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete ticket type
        app.MapDelete("/api/events/{eventId}/ticket-types/{ticketTypeId}", async (
            string eventId,
            string ticketTypeId,
            [FromServices] IEventService eventService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require Event Organizer or Admin role
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be authenticated to delete ticket types",
                    statusCode: 401);
            }

            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Administrator" && userRole != "EventOrganizer")
            {
                return Results.Problem(
                    title: "Insufficient Permissions",
                    detail: "Administrator or Event Organizer role required",
                    statusCode: 403);
            }

            var (success, response, error) = await eventService.DeleteTicketTypeAsync(eventId, ticketTypeId, cancellationToken);

            if (!success || response == null)
            {
                var statusCode = error.Contains("not found") ? 404 :
                                error.Contains("Cannot delete") ? 400 : 500;

                return Results.Problem(
                    title: "Failed to delete ticket type",
                    detail: error,
                    statusCode: statusCode);
            }

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("DeleteTicketType")
        .WithSummary("Delete a ticket type")
        .WithDescription("Deletes a ticket type if no tickets have been sold")
        .WithTags("Events", "TicketTypes")
        .Produces<DeleteTicketTypeResultDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);
    }
}