using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Features.VettingHold.Models;
using WitchCityRope.Api.Features.VettingHold.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.VettingHold.Endpoints;

/// <summary>
/// Endpoints for membership hold and reinstatement functionality
/// Allows users to voluntarily place their membership on hold and request reinstatement
/// </summary>
public static class VettingHoldEndpoints
{
    public static void MapVettingHoldEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/{userId}/vetting")
            .WithTags("Vetting - Hold/Reinstatement")
            .RequireAuthorization(); // All endpoints require authentication

        // PUT: Place membership on hold
        group.MapPut("/hold", PlaceMembershipOnHold)
            .WithName("PlaceMembershipOnHold")
            .WithSummary("Place membership on hold (user action)")
            .WithDescription("Allows approved users to voluntarily place their membership on hold. " +
                           "Cancels all future social event RSVPs and changes status to OnHold.")
            .Produces<MembershipHoldResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // PUT: Request reinstatement
        group.MapPut("/reinstate", RequestReinstatement)
            .WithName("RequestReinstatement")
            .WithSummary("Request membership reinstatement (user action)")
            .WithDescription("Allows users on hold to request reinstatement. " +
                           "Changes status to FinalReview for admin approval.")
            .Produces<MembershipHoldResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // GET: Get hold status
        group.MapGet("/hold-status", GetHoldStatus)
            .WithName("GetVettingHoldStatus")
            .WithSummary("Get current hold/reinstatement status")
            .WithDescription("Returns current vetting status and available actions (can place on hold, can request reinstatement).")
            .Produces<VettingHoldStatusResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);
    }

    /// <summary>
    /// Place membership on hold
    /// User must be operating on their own profile
    /// User must be currently Approved
    /// </summary>
    private static async Task<IResult> PlaceMembershipOnHold(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid userId,
        PlaceMembershipOnHoldRequest request,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "You can only place your own membership on hold",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.PlaceMembershipOnHoldAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToProblem("Failed to place membership on hold");
            }

            return Results.Ok(result.Value); // Direct DTO
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Request membership reinstatement
    /// User must be operating on their own profile
    /// User must be currently OnHold
    /// </summary>
    private static async Task<IResult> RequestReinstatement(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid userId,
        RequestReinstatementRequest request,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "You can only request reinstatement for your own membership",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.RequestReinstatementAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToProblem("Failed to request reinstatement");
            }

            return Results.Ok(result.Value); // Direct DTO
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get current hold/reinstatement status
    /// User must be operating on their own profile
    /// </summary>
    private static async Task<IResult> GetHoldStatus(
        Guid userId,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem( // ARCH-ALLOW: ownership guard — forbidden, not a service Result
                    title: "Forbidden",
                    detail: "You can only view your own hold status",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.GetHoldStatusAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToProblem("Hold status not found");
            }

            return Results.Ok(result.Value); // Direct DTO
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
