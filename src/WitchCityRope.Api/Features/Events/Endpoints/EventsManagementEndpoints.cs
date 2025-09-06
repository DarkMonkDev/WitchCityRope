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
}