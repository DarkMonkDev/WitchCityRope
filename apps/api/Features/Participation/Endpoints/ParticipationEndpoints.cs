using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;

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
                IParticipationService participationService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await participationService.GetParticipationStatusAsync(eventId, userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Failed to get participation status",
                        detail: result.Error,
                        statusCode: 500);
            })
            .WithName("GetParticipationStatus")
            .WithSummary("Get user's participation status for an event")
            .WithDescription("Returns the user's current participation status (RSVP or ticket) for the specified event")
            .WithTags("Participation")
            .Produces<ParticipationStatusDto?>(200)
            .Produces(401)
            .Produces(500);

        // Create RSVP for social event
        app.MapPost("/api/events/{eventId:guid}/rsvp",
            [Authorize] async (
                Guid eventId,
                CreateRSVPRequest request,
                IParticipationService participationService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Ensure the eventId in URL matches the request
                request.EventId = eventId;

                var result = await participationService.CreateRSVPAsync(request, userId, cancellationToken);

                if (!result.IsSuccess)
                {
                    // Check for specific business rule violations
                    if (result.Error.Contains("not found"))
                    {
                        return Results.NotFound(new { error = result.Error });
                    }
                    if (result.Error.Contains("vetted") || result.Error.Contains("capacity") || result.Error.Contains("already"))
                    {
                        return Results.BadRequest(new { error = result.Error });
                    }

                    return Results.Problem(
                        title: "Failed to create RSVP",
                        detail: result.Error,
                        statusCode: 500);
                }

                return Results.Created($"/api/events/{eventId}/participation", result.Value);
            })
            .WithName("CreateRSVP")
            .WithSummary("Create RSVP for social event")
            .WithDescription("Creates an RSVP for a social event. Only available to vetted members.")
            .WithTags("Participation")
            .Produces<ParticipationStatusDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Purchase ticket for class event
        app.MapPost("/api/events/{eventId:guid}/tickets",
            [Authorize] async (
                Guid eventId,
                CreateTicketPurchaseRequest request,
                IParticipationService participationService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Ensure the eventId in URL matches the request
                request.EventId = eventId;

                var result = await participationService.CreateTicketPurchaseAsync(request, userId, cancellationToken);

                if (!result.IsSuccess)
                {
                    // Check for specific business rule violations
                    if (result.Error.Contains("not found"))
                    {
                        return Results.NotFound(new { error = result.Error });
                    }
                    if (result.Error.Contains("capacity") || result.Error.Contains("already") || result.Error.Contains("only allowed"))
                    {
                        return Results.BadRequest(new { error = result.Error });
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
            .WithDescription("Purchases a ticket for a class event. Available to any authenticated user.")
            .WithTags("Participation")
            .Produces<ParticipationStatusDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // Cancel participation (both RSVPs and tickets)
        app.MapDelete("/api/events/{eventId:guid}/participation",
            [Authorize] async (
                Guid eventId,
                IParticipationService participationService,
                ClaimsPrincipal user,
                string? reason = null,
                CancellationToken cancellationToken = default) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await participationService.CancelParticipationAsync(eventId, userId, reason, cancellationToken);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found"))
                    {
                        return Results.NotFound(new { error = result.Error });
                    }
                    if (result.Error.Contains("cannot be cancelled"))
                    {
                        return Results.BadRequest(new { error = result.Error });
                    }

                    return Results.Problem(
                        title: "Failed to cancel participation",
                        detail: result.Error,
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

        // Get user's participations
        app.MapGet("/api/user/participations",
            [Authorize] async (
                IParticipationService participationService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await participationService.GetUserParticipationsAsync(userId, cancellationToken);

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

        // Backward compatibility: Cancel RSVP (alias for cancelling participation)
        app.MapDelete("/api/events/{eventId:guid}/rsvp",
            [Authorize] async (
                Guid eventId,
                IParticipationService participationService,
                ClaimsPrincipal user,
                string? reason = null,
                CancellationToken cancellationToken = default) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await participationService.CancelParticipationAsync(eventId, userId, reason, cancellationToken);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found"))
                    {
                        return Results.NotFound(new { error = result.Error });
                    }
                    if (result.Error.Contains("cannot be cancelled"))
                    {
                        return Results.BadRequest(new { error = result.Error });
                    }

                    return Results.Problem(
                        title: "Failed to cancel RSVP",
                        detail: result.Error,
                        statusCode: 500);
                }

                return Results.NoContent();
            })
            .WithName("CancelRSVP")
            .WithSummary("Cancel RSVP (backward compatibility)")
            .WithDescription("Cancels the user's RSVP. Alias for cancelling participation.")
            .WithTags("Participation")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);
    }
}