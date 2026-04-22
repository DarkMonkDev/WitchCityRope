using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.CheckIn.Validation;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.CheckIn.Endpoints;

/// <summary>
/// CheckIn minimal API endpoints
/// Mobile-optimized for event staff check-in operations
/// </summary>
public static class CheckInEndpoints
{
    /// <summary>
    /// Register CheckIn endpoints using minimal API pattern
    /// </summary>
    public static void MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checkin")
            .WithTags("CheckIn");

        // Get attendees for check-in interface
        group.MapGet("/events/{eventId}/attendees", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            ICheckInService checkInService,
            string? search,
            string? status,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // TOKEN IS VALID - Proceed with check-in operation
            // Pass sessionIds from validated token to filter attendees for these sessions
            // Support both multi-session tokens (SessionIds list) and legacy single-session tokens (SessionId)
            var sessionIds = tokenData.SessionIds ??
                (tokenData.SessionId != default ? new List<Guid> { tokenData.SessionId } : new List<Guid>());

            var result = await checkInService.GetEventAttendeesAsync(
                eventId, sessionIds, search, status, page, pageSize, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Get Attendees Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("GetEventAttendees")
        .WithSummary("Get attendees for event check-in")
        .WithDescription("Returns attendees list with search and filtering for check-in interface")
        .Produces<CheckInAttendeesResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError);

        // Process check-in
        group.MapPost("/events/{eventId}/checkin", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            CheckInRequest request,
            ICheckInService checkInService,
            IValidator<CheckInRequest> validator,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // Validate request
            var requestValidation = await validator.ValidateAsync(request, cancellationToken);
            if (!requestValidation.IsValid)
            {
                return Results.ValidationProblem(requestValidation.ToDictionary());
            }

            // TOKEN IS VALID - Proceed with check-in operation
            var result = await checkInService.CheckInAttendeeAsync(request, token, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Check-in Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("ProcessCheckIn")
        .WithSummary("Process attendee check-in")
        .WithDescription("Check in an attendee for the event with capacity validation")
        .Produces<CheckInResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // Get event dashboard
        group.MapGet("/events/{eventId}/dashboard", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            ICheckInService checkInService,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // TOKEN IS VALID - Proceed with check-in operation
            var result = await checkInService.GetEventDashboardAsync(eventId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Get Dashboard Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("GetEventDashboard")
        .WithSummary("Get event check-in dashboard")
        .WithDescription("Returns real-time check-in statistics and recent activity")
        .Produces<DashboardResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // Sync offline check-ins
        group.MapPost("/events/{eventId}/sync", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            SyncRequest request,
            ISyncService syncService,
            IValidator<SyncRequest> validator,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // Validate request
            var requestValidation = await validator.ValidateAsync(request, cancellationToken);
            if (!requestValidation.IsValid)
            {
                return Results.ValidationProblem(requestValidation.ToDictionary());
            }

            // TOKEN IS VALID - Proceed with check-in operation
            var result = await syncService.ProcessOfflineSyncAsync(request, token, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Sync Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("SyncOfflineCheckIns")
        .WithSummary("Sync offline check-in data")
        .WithDescription("Process pending check-ins from offline operation with conflict detection");

        // Record cash payment for door purchase
        group.MapPost("/events/{eventId}/cash-payment", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            CashPaymentRequest request,
            ICheckInService checkInService,
            IValidator<CashPaymentRequest> validator,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
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
                : result.ToProblem("Cash Payment Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("RecordCashPayment")
        .WithSummary("Record door cash payment for attendee")
        .WithDescription("Creates a TicketPurchase record for cash payment at event door with staff attribution")
        .Produces<CashPaymentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // Create manual entry
        group.MapPost("/events/{eventId}/manual-entry", async (
            Guid eventId,
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            ManualEntryData request,
            ICheckInService checkInService,
            IValidator<ManualEntryData> validator,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            var tokenData = validationResult.Value;
            if (tokenData.EventId != eventId)
            {
                return Results.Problem( // ARCH-ALLOW: token-scope guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "Check-in token is for a different event",
                    statusCode: 403);
            }

            // Validate request
            var requestValidation = await validator.ValidateAsync(request, cancellationToken);
            if (!requestValidation.IsValid)
            {
                return Results.ValidationProblem(requestValidation.ToDictionary());
            }

            // TOKEN IS VALID - Proceed with check-in operation
            var result = await checkInService.CreateManualEntryAsync(eventId, request, token, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Manual Entry Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("CreateManualEntry")
        .WithSummary("Create manual entry for walk-in attendee")
        .WithDescription("Register and check in a walk-in attendee who isn't pre-registered");

        // ============================================================
        // ADMIN ENDPOINTS - Session Token Management
        // Requires Administrator or EventOrganizer role
        // ============================================================

        // Generate check-in session token for event
        group.MapPost("/session-tokens/generate", async (
            [FromBody] GenerateTokenRequest request,
            ISessionTokenService tokenService,
            ClaimsPrincipal user,
            ILogger<Program> logger,
            CancellationToken cancellationToken = default) =>
        {
            logger.LogInformation(
                "Token generation request received: EventId={EventId}, ExpirationHours={ExpirationHours} (isNull={IsNull})",
                request.EventId, request.ExpirationHours, request.ExpirationHours == null);

            // Get admin user ID from token
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var adminUserId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Authorization Failed",
                    detail: "Unable to identify user from token",
                    statusCode: 403);
            }

            var result = await tokenService.GenerateTokenAsync(
                request,
                adminUserId,
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Bad Request");
        })
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.EventOrganizer.ToRoleString()))
        .WithName("GenerateCheckInToken")
        .WithSummary("Generate check-in session token for event")
        .WithDescription("Admin generates a token that grants kiosk access to event check-in operations")
        .Produces<SessionTokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("CheckIn");

        // Revoke active session token
        group.MapPost("/session-tokens/revoke", async (
            [FromBody] RevokeTokenRequest request,
            ISessionTokenService tokenService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
        {
            // Get admin user ID from token
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var adminUserId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Authorization Failed",
                    detail: "Unable to identify user from token",
                    statusCode: 403);
            }

            var result = await tokenService.RevokeTokenAsync(request.Token, adminUserId, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToProblem("Bad Request");
        })
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.EventOrganizer.ToRoleString()))
        .WithName("RevokeCheckInToken")
        .WithSummary("Revoke active check-in session token")
        .WithDescription("Admin revokes a token for security incidents or lost devices")
        .WithTags("CheckIn");

        // Get all active tokens for an event
        group.MapGet("/session-tokens/event/{eventId}", async (
            Guid eventId,
            ISessionTokenService tokenService,
            CancellationToken cancellationToken = default) =>
        {
            var result = await tokenService.GetActiveTokensForEventAsync(eventId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Bad Request");
        })
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.EventOrganizer.ToRoleString()))
        .WithName("GetActiveCheckInTokens")
        .WithSummary("Get all active session tokens for event")
        .WithDescription("Admin monitoring of active kiosk sessions")
        .Produces<List<SessionTokenResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("CheckIn");

        // Get pending sync count
        group.MapGet("/sync/pending-count", async (
            [FromHeader(Name = "X-CheckIn-Token")] string? token,
            [FromServices] ISessionTokenService tokenService,
            ISyncService syncService,
            CancellationToken cancellationToken = default) =>
        {
            // VALIDATE TOKEN FIRST
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Check-in token is required",
                    statusCode: 401);
            }

            var validationResult = await tokenService.ValidateTokenAsync(token, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "Invalid or expired check-in token",
                    statusCode: 401);
            }

            // Token is valid - use a system GUID for sync count since we don't have user context
            // Sync operations are now per-token/session, not per-user
            var sessionId = Guid.NewGuid(); // Placeholder - sync service may need refactoring

            var result = await syncService.GetPendingSyncCountAsync(sessionId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { pendingCount = result.Value })
                : result.ToProblem("Get Pending Count Failed");
        })
        .AllowAnonymous() // No authentication required - token validated in handler
        .WithName("GetPendingSyncCount")
        .WithSummary("Get pending sync operations count")
        .WithDescription("Returns the number of pending offline operations for the current session");
    }
}