using System.Security.Claims;
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
            .Produces<ApiResponse<MembershipHoldResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(500);

        // PUT: Request reinstatement
        group.MapPut("/reinstate", RequestReinstatement)
            .WithName("RequestReinstatement")
            .WithSummary("Request membership reinstatement (user action)")
            .WithDescription("Allows users on hold to request reinstatement. " +
                           "Changes status to FinalReview for admin approval.")
            .Produces<ApiResponse<MembershipHoldResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(500);

        // GET: Get hold status
        group.MapGet("/hold-status", GetHoldStatus)
            .WithName("GetVettingHoldStatus")
            .WithSummary("Get current hold/reinstatement status")
            .WithDescription("Returns current vetting status and available actions (can place on hold, can request reinstatement).")
            .Produces<ApiResponse<VettingHoldStatusResponse>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);
    }

    /// <summary>
    /// Place membership on hold
    /// User must be operating on their own profile
    /// User must be currently Approved
    /// </summary>
    private static async Task<IResult> PlaceMembershipOnHold(
        Guid userId,
        PlaceMembershipOnHoldRequest request,
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
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Forbidden",
                        Message = "You can only place your own membership on hold",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.PlaceMembershipOnHoldAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = result.Error,
                        Message = result.Details,
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 400);
            }

            return Results.Ok(new ApiResponse<MembershipHoldResponse>
            {
                Success = true,
                Data = result.Value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(
                new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                },
                statusCode: 500);
        }
    }

    /// <summary>
    /// Request membership reinstatement
    /// User must be operating on their own profile
    /// User must be currently OnHold
    /// </summary>
    private static async Task<IResult> RequestReinstatement(
        Guid userId,
        RequestReinstatementRequest request,
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
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Forbidden",
                        Message = "You can only request reinstatement for your own membership",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.RequestReinstatementAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = result.Error,
                        Message = result.Details,
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 400);
            }

            return Results.Ok(new ApiResponse<MembershipHoldResponse>
            {
                Success = true,
                Data = result.Value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(
                new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                },
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
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Forbidden",
                        Message = "You can only view your own hold status",
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.GetHoldStatusAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    new ApiResponse<object>
                    {
                        Success = false,
                        Error = result.Error,
                        Message = result.Details,
                        Timestamp = DateTime.UtcNow
                    },
                    statusCode: 404);
            }

            return Results.Ok(new ApiResponse<VettingHoldStatusResponse>
            {
                Success = true,
                Data = result.Value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(
                new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                },
                statusCode: 500);
        }
    }
}
