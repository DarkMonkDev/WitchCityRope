using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.ProxyRsvp.Models;
using WitchCityRope.Api.Features.ProxyRsvp.Services;

namespace WitchCityRope.Api.Features.ProxyRsvp.Endpoints;

/// <summary>
/// Minimal API endpoints for proxy RSVP operations.
/// Allows a delegate to create an RSVP on behalf of a principal,
/// and the principal to accept or decline the proxy RSVP.
///
/// All endpoints require authentication (any authenticated user, no specific role).
/// State-changing endpoints (POST) require CSRF validation.
///
/// Endpoints:
/// - EP-12: POST /api/events/{eventId}/proxy-rsvp - Create proxy RSVP
/// - EP-13: POST /api/events/{eventId}/rsvp/{attendanceId}/accept - Accept proxy RSVP
/// - EP-14: POST /api/events/{eventId}/rsvp/{attendanceId}/decline - Decline proxy RSVP
/// </summary>
public static class ProxyRsvpEndpoints
{
    /// <summary>
    /// Register all proxy RSVP endpoints using minimal API pattern.
    /// Follows the same patterns as AuthorizedContactEndpoints and ParticipationEndpoints.
    /// </summary>
    public static void MapProxyRsvpEndpoints(this IEndpointRouteBuilder app)
    {
        // ============================================================================
        // EP-12: POST /api/events/{eventId:guid}/proxy-rsvp
        // Creates a proxy RSVP for a principal on behalf of the authenticated delegate.
        // Business rules: BR-050, BR-051, BR-054, BR-035
        // Use case: UC-005
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/proxy-rsvp",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                CreateProxyRsvpRequest request,
                IProxyRsvpService proxyRsvpService,
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

                var result = await proxyRsvpService.CreateProxyRsvpAsync(
                    eventId, userId, request.RsvpForUserId, request.Notes, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created(
                        $"/api/events/{eventId}/rsvp/{result.Value!.AttendanceId}",
                        result.Value);
                }

                // Map service error messages to appropriate HTTP status codes
                var statusCode = result.Error switch
                {
                    "Event not found" => 404,
                    "RSVPs not allowed" => 400,
                    "Not authorized" => 403,
                    "Vetting required" => 403,
                    "Event at capacity" => 400,
                    "Already has RSVP" => 409,
                    "User not found" => 404,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("CreateProxyRsvp")
            .WithSummary("Create a proxy RSVP for an authorized contact")
            .WithDescription("Delegate creates an RSVP for a principal who authorized them. The RSVP enters PendingAcceptance until the principal accepts the event waiver. BR-050, BR-051, BR-054.")
            .WithTags("ProxyRsvp")
            .Produces<ProxyRsvpDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // EP-13: POST /api/events/{eventId:guid}/rsvp/{attendanceId:guid}/accept
        // Accepts a proxy RSVP. Only the principal (the person the RSVP was created for) can accept.
        // Business rules: AD-003, BR-030, BR-031, BR-032, BR-036
        // Use case: UC-006
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/rsvp/{attendanceId:guid}/accept",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                AcceptProxyRsvpRequest request,
                IProxyRsvpService proxyRsvpService,
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

                var result = await proxyRsvpService.AcceptProxyRsvpAsync(
                    attendanceId, userId, request, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                // Map service error messages to appropriate HTTP status codes
                var statusCode = result.Error switch
                {
                    "RSVP not found" => 404,
                    "Not authorized" => 403,
                    "Waiver required" => 400,
                    "Vetting required" => 403,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("AcceptProxyRsvp")
            .WithSummary("Accept a proxy RSVP by signing the event waiver")
            .WithDescription("Principal accepts a proxy RSVP created on their behalf. Requires event waiver acceptance (AD-003). Optionally accepts ToS (BR-031). Transitions status to Active (BR-032).")
            .WithTags("ProxyRsvp")
            .Produces<ProxyRsvpDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // ============================================================================
        // EP-14: POST /api/events/{eventId:guid}/rsvp/{attendanceId:guid}/decline
        // Declines a proxy RSVP. Only the principal can decline.
        // Unlike tickets, declined RSVPs are simply cancelled (no reassignment flow).
        // ============================================================================
        app.MapPost("/api/events/{eventId:guid}/rsvp/{attendanceId:guid}/decline",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid eventId,
                Guid attendanceId,
                IProxyRsvpService proxyRsvpService,
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

                var result = await proxyRsvpService.DeclineProxyRsvpAsync(
                    attendanceId, userId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok();
                }

                // Map service error messages to appropriate HTTP status codes
                var statusCode = result.Error switch
                {
                    "RSVP not found" => 404,
                    "Not authorized" => 403,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("DeclineProxyRsvp")
            .WithSummary("Decline a proxy RSVP")
            .WithDescription("Principal declines a proxy RSVP created on their behalf. The RSVP is cancelled (no reassignment flow for RSVPs, unlike tickets). The delegate can create a new proxy RSVP if desired.")
            .WithTags("ProxyRsvp")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }
}
