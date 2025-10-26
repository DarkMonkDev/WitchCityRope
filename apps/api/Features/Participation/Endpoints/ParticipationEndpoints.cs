using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Vetting.Services;
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
                IParticipationService participationService,
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
            .WithDescription("Returns enhanced participation status with hasRSVP/hasTicket flags, nested RSVP/ticket details, and capacity information")
            .WithTags("Participation")
            .Produces<EnhancedParticipationStatusDto?>(200)
            .Produces(401)
            .Produces(500);

        // Create RSVP for social event
        app.MapPost("/api/events/{eventId:guid}/rsvp",
            [Authorize] async (
                Guid eventId,
                CreateRSVPRequest request,
                IParticipationService participationService,
                IVettingAccessControlService vettingAccessControlService,
                ClaimsPrincipal user,
                ILogger<IParticipationService> logger,
                CancellationToken cancellationToken) =>
            {
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

                var result = await participationService.CreateRSVPAsync(request, userId, cancellationToken);

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
                    if (result.Error.Contains("vetted") || result.Error.Contains("capacity"))
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

        // Purchase ticket for class event
        app.MapPost("/api/events/{eventId:guid}/tickets",
            [Authorize] async (
                Guid eventId,
                CreateTicketPurchaseRequest request,
                IParticipationService participationService,
                IVettingAccessControlService vettingAccessControlService,
                ClaimsPrincipal user,
                ILogger<IParticipationService> logger,
                CancellationToken cancellationToken) =>
            {
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

                var result = await participationService.CreateTicketPurchaseAsync(request, userId, cancellationToken);

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
                    if (result.Error.Contains("capacity") || result.Error.Contains("only allowed"))
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

        // Cancel participation (both RSVPs and tickets)
        app.MapDelete("/api/events/{eventId:guid}/participation",
            [Authorize] async (
                Guid eventId,
                IParticipationService participationService,
                ClaimsPrincipal user,
                string? type = null,
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

                // Parse participation type if provided (rsvp, ticket, or null for most recent)
                ParticipationType? participationType = type?.ToLower() switch
                {
                    "rsvp" => ParticipationType.RSVP,
                    "ticket" => ParticipationType.Ticket,
                    _ => null
                };

                var result = await participationService.CancelParticipationAsync(eventId, userId, participationType, reason, cancellationToken);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found"))
                    {
                            return Results.Problem(
                            title: "Resource Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("cannot be cancelled"))
                    {
                        return Results.Problem(
                            title: "Bad Request",
                            detail: result.Error,
                            statusCode: 400);
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
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
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
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                // Backward compatibility: explicitly cancel RSVP type
                var result = await participationService.CancelParticipationAsync(eventId, userId, ParticipationType.RSVP, reason, cancellationToken);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("not found"))
                    {
                            return Results.Problem(
                            title: "Resource Not Found",
                            detail: result.Error,
                            statusCode: 404);
                    }
                    if (result.Error.Contains("cannot be cancelled"))
                    {
                        return Results.Problem(
                            title: "Bad Request",
                            detail: result.Error,
                            statusCode: 400);
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

        // Admin endpoint: Get all participations for an event
        app.MapGet("/api/admin/events/{eventId:guid}/participations",
            [Authorize(Roles = "Administrator")] async (
                Guid eventId,
                IParticipationService participationService,
                CancellationToken cancellationToken) =>
            {
                var result = await participationService.GetEventParticipationsAsync(eventId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(new ApiResponse<List<EventParticipationDto>>
                    {
                        Success = true,
                        Data = result.Value,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Results.Json(new ApiResponse<List<EventParticipationDto>>
                {
                    Success = false,
                    Data = null,
                    Error = "Failed to get event participations",
                    Details = result.Error,
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            })
            .WithName("GetEventParticipations")
            .WithSummary("Get all participations for an event (admin only)")
            .WithDescription("Returns all RSVPs and ticket purchases for the specified event. Admin role required.")
            .WithTags("Admin", "Participation")
            .Produces<ApiResponse<List<EventParticipationDto>>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);
    }
}