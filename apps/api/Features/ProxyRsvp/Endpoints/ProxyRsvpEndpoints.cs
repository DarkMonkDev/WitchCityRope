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

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: MapErrorToStatusCode(result.Error));
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

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: MapErrorToStatusCode(result.Error));
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

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: MapErrorToStatusCode(result.Error));
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

    // Single source for error-string → HTTP-status mapping across all three ProxyRsvp
    // endpoints. Uses case-insensitive substring matching rather than an exact switch for
    // two reasons, both learned from a 2026-04-12 production incident (see
    // docs/functional-areas/production-incidents/01-health-check-2026-04-12.md):
    //   1. The previous exact-switch had "Already has RSVP" => 409, but the service actually
    //      returns "Already participating". Mismatch fell through to 500. Real users saw it.
    //   2. Two other service error strings ("Per-person limit reached", "Acceptance window
    //      expired") were never listed in the switch at all and also fell through to 500.
    // Substring matching is brittle against silent string edits for a DIFFERENT reason — a
    // refactor that renames "Event not found" to something without "not found" would now
    // map to 500 without warning. Accepted tradeoff: most service strings will stay stable,
    // and the coverage here is explicit enough that a grep for each keyword will surface
    // a change during review.
    //
    // Order matters. The 403 matchers for "Not authorized" and "Vetting required" must run
    // BEFORE the 400 matcher that contains "required" — otherwise "Vetting required" would
    // be misclassified as 400 because it contains the substring "required".
    private static int MapErrorToStatusCode(string? error)
    {
        if (string.IsNullOrEmpty(error)) return 500;

        // 404: "Event not found", "User not found", "RSVP not found"
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return 404;

        // 403: authorization / vetting failures. Must precede the 400 "required" matcher
        // because "Vetting required" contains that substring.
        if (error.Contains("not authorized", StringComparison.OrdinalIgnoreCase))
            return 403;
        if (error.Contains("vetting required", StringComparison.OrdinalIgnoreCase))
            return 403;

        // 409: duplicate participation / already in a terminal state
        if (error.Contains("already", StringComparison.OrdinalIgnoreCase))
            return 409;

        // 400: business-rule violations on otherwise well-formed requests.
        //   "capacity"       — Event at capacity
        //   "not allowed"    — RSVPs not allowed (event isn't a social, etc.)
        //   "limit reached"  — Per-person limit reached
        //   "window expired" — Acceptance window expired
        //   "required"       — Waiver required (Vetting required already handled above)
        if (error.Contains("capacity", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not allowed", StringComparison.OrdinalIgnoreCase)
            || error.Contains("limit reached", StringComparison.OrdinalIgnoreCase)
            || error.Contains("window expired", StringComparison.OrdinalIgnoreCase)
            || error.Contains("required", StringComparison.OrdinalIgnoreCase))
            return 400;

        // 500 fallback: unexpected internal errors (e.g., "Failed to create proxy RSVP"
        // from the service's catch-all exception handler).
        return 500;
    }
}
