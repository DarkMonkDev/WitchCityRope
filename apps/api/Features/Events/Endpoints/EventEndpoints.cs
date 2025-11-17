using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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
        // Get all events with optional admin access
        app.MapGet("/api/events", async (
            [FromServices] IEventService eventService,
            HttpContext context,
            bool? includeUnpublished,
            CancellationToken cancellationToken) =>
            {
                // Check if user is requesting unpublished events
                var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);

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

                var (success, response, error) = await eventService.GetEventsAsync(shouldIncludeUnpublished, cancellationToken);

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
            .WithDescription("Returns events from the database. Use ?includeUnpublished=true for admin access to draft events. Requires Administrator role for unpublished events.")
            .WithTags("Events")
            .Produces<List<EventDto>>(200)
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

        // Update existing event by ID
        app.MapPut("/api/events/{id}", async (
            string id,
            UpdateEventRequest request,
            [FromServices] IEventService eventService,
            CancellationToken cancellationToken) =>
            {
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
                    string msg when msg.Contains("past events") => 400,
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
                return Results.Unauthorized();
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Unauthorized();
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Forbid(); // 403 - Token is for different event
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
    }
}