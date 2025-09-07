using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Events.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Events Management endpoints implementing the Event Session Matrix API requirements
/// Following minimal API patterns from lessons learned
/// </summary>
public static class EventsManagementEndpoints
{
    /// <summary>
    /// Maps the Events Management endpoints
    /// </summary>
    public static RouteGroupBuilder MapEventsManagementEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/events - List published events
        group.MapGet("/events", GetPublishedEventsAsync)
            .WithName("GetPublishedEvents")
            .WithTags("Events")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get published events";
                operation.Description = "Retrieves a list of published events with basic information and pricing.";
                return operation;
            })
            .Produces<List<EventSummaryDto>>(200)
            .Produces(500);

        // GET /api/events/{eventId} - Get event details with sessions and ticket types
        group.MapGet("/events/{eventId:guid}", GetEventDetailsAsync)
            .WithName("GetEventDetails")
            .WithTags("Events")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get event details";
                operation.Description = "Retrieves complete event details including sessions and ticket types.";
                return operation;
            })
            .Produces<EventDetailsDto>(200)
            .Produces(404)
            .Produces(500);

        // GET /api/events/{eventId}/availability - Get real-time availability
        group.MapGet("/events/{eventId:guid}/availability", GetEventAvailabilityAsync)
            .WithName("GetEventAvailability")
            .WithTags("Events", "Availability")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get event availability";
                operation.Description = "Retrieves real-time availability for sessions and ticket types.";
                return operation;
            })
            .Produces<AvailabilityDto>(200)
            .Produces(404)
            .Produces(500);

        // POST /api/events/{eventId}/rsvp - Create free RSVP for social events
        group.MapPost("/events/{eventId:guid}/rsvp", CreateRSVPAsync)
            .RequireAuthorization()
            .WithName("CreateRSVP")
            .WithTags("Events", "RSVP")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create RSVP for social event";
                operation.Description = "Creates a free RSVP for a social event. Only available for Social events.";
                return operation;
            })
            .Produces<RSVPDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // GET /api/events/{eventId}/attendance - Get user's attendance status
        group.MapGet("/events/{eventId:guid}/attendance", GetAttendanceStatusAsync)
            .RequireAuthorization()
            .WithName("GetAttendanceStatus")
            .WithTags("Events", "RSVP")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get user's attendance status";
                operation.Description = "Gets user's RSVP and ticket status for an event, showing what actions are available.";
                return operation;
            })
            .Produces<AttendanceStatusDto>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // DELETE /api/events/{eventId}/rsvp - Cancel RSVP
        group.MapDelete("/events/{eventId:guid}/rsvp", CancelRSVPAsync)
            .RequireAuthorization()
            .WithName("CancelRSVP")
            .WithTags("Events", "RSVP")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Cancel RSVP";
                operation.Description = "Cancels the user's RSVP for an event.";
                return operation;
            })
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        return group;
    }

    /// <summary>
    /// Gets published events with optional filters
    /// </summary>
    private static async Task<IResult> GetPublishedEventsAsync(
        [FromQuery] string? eventType,
        [FromQuery] bool showPast,
        [FromQuery] Guid? organizerId,
        EventsManagementService eventsService,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await eventsService.GetPublishedEventsAsync(
            eventType, showPast, organizerId, cancellationToken);

        if (success && response != null)
        {
            return Results.Ok(new { events = response });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to retrieve events");
    }

    /// <summary>
    /// Gets complete event details including sessions and ticket types
    /// </summary>
    private static async Task<IResult> GetEventDetailsAsync(
        [FromRoute] Guid eventId,
        EventsManagementService eventsService,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await eventsService.GetEventDetailsAsync(
            eventId, cancellationToken);

        if (success && response != null)
        {
            return Results.Ok(response);
        }

        if (error == "Event not found")
        {
            return Results.NotFound(new { error });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to retrieve event details");
    }

    /// <summary>
    /// Gets real-time availability for an event
    /// </summary>
    private static async Task<IResult> GetEventAvailabilityAsync(
        [FromRoute] Guid eventId,
        EventsManagementService eventsService,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await eventsService.GetEventAvailabilityAsync(
            eventId, cancellationToken);

        if (success && response != null)
        {
            return Results.Ok(response);
        }

        if (error == "Event not found")
        {
            return Results.NotFound(new { error });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to calculate availability");
    }

    /// <summary>
    /// Creates a free RSVP for a social event
    /// </summary>
    private static async Task<IResult> CreateRSVPAsync(
        [FromRoute] Guid eventId,
        [FromBody] RSVPRequest request,
        EventsManagementService eventsService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Problem(
                detail: "Invalid or missing user authentication",
                statusCode: 401,
                title: "Authentication error");
        }

        var (success, response, error) = await eventsService.CreateRSVPAsync(
            eventId, userId, request, cancellationToken);

        if (success && response != null)
        {
            return Results.Created($"/api/events/{eventId}/rsvp", response);
        }

        // Handle specific error cases
        if (error.Contains("not found"))
        {
            return Results.NotFound(new { error });
        }

        if (error.Contains("not allow RSVP") || error.Contains("already RSVP") || error.Contains("full capacity"))
        {
            return Results.BadRequest(new { error });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to create RSVP");
    }

    /// <summary>
    /// Gets the user's attendance status for an event
    /// </summary>
    private static async Task<IResult> GetAttendanceStatusAsync(
        [FromRoute] Guid eventId,
        EventsManagementService eventsService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Problem(
                detail: "Invalid or missing user authentication",
                statusCode: 401,
                title: "Authentication error");
        }

        var (success, response, error) = await eventsService.GetAttendanceStatusAsync(
            eventId, userId, cancellationToken);

        if (success && response != null)
        {
            return Results.Ok(response);
        }

        if (error == "Event not found")
        {
            return Results.NotFound(new { error });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to get attendance status");
    }

    /// <summary>
    /// Cancels the user's RSVP for an event
    /// </summary>
    private static async Task<IResult> CancelRSVPAsync(
        [FromRoute] Guid eventId,
        EventsManagementService eventsService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Problem(
                detail: "Invalid or missing user authentication",
                statusCode: 401,
                title: "Authentication error");
        }

        var (success, error) = await eventsService.CancelRSVPAsync(
            eventId, userId, "User requested cancellation", cancellationToken);

        if (success)
        {
            return Results.NoContent();
        }

        if (error.Contains("not found"))
        {
            return Results.NotFound(new { error });
        }

        return Results.Problem(
            detail: error,
            statusCode: 500,
            title: "Failed to cancel RSVP");
    }
}