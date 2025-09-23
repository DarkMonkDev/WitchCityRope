using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Endpoints;

/// <summary>
/// Vetting system endpoints for admin/reviewer functionality
/// Basic implementation to support admin vetting grid requirements
/// </summary>
public static class VettingEndpoints
{
    public static void MapVettingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting")
            .WithTags("Vetting")
            .RequireAuthorization(); // All vetting endpoints require authentication

        // GET: Paginated list of applications for reviewers
        group.MapPost("/reviewer/applications", GetApplicationsForReview)
            .WithName("GetApplicationsForReview")
            .WithSummary("Get paginated list of vetting applications")
            .Produces<ApiResponse<PagedResult<ApplicationSummaryDto>>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(500);

        // GET: Application detail for review
        group.MapGet("/reviewer/applications/{id}", GetApplicationDetail)
            .WithName("GetApplicationDetail")
            .WithSummary("Get detailed application information")
            .Produces<ApiResponse<ApplicationDetailResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // POST: Submit review decision
        group.MapPost("/reviewer/applications/{id}/decisions", SubmitReviewDecision)
            .WithName("SubmitReviewDecision")
            .WithSummary("Submit a review decision for an application")
            .Produces<ApiResponse<ReviewDecisionResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // POST: Add note to application
        group.MapPost("/reviewer/applications/{id}/notes", AddApplicationNote)
            .WithName("AddApplicationNote")
            .WithSummary("Add a note to an application")
            .Produces<ApiResponse<NoteResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);
    }

    /// <summary>
    /// Get paginated list of applications for admin/reviewer dashboard
    /// POST /api/vetting/reviewer/applications
    /// </summary>
    private static async Task<IResult> GetApplicationsForReview(
        ApplicationFilterRequest request,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims with fallback for administrators
            var reviewerIdClaim = user.FindFirst("ReviewerId")?.Value;
            Guid reviewerId;

            if (!string.IsNullOrEmpty(reviewerIdClaim) && Guid.TryParse(reviewerIdClaim, out reviewerId))
            {
                // Use specific reviewer ID if available
            }
            else
            {
                // Fallback: Use user ID for administrators
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out reviewerId))
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "User information not found",
                        Timestamp = DateTime.UtcNow
                    }, statusCode: 400);
                }
            }

            var result = await vettingService.GetApplicationsForReviewAsync(request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<PagedResult<ApplicationSummaryDto>>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Applications retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 : 500;
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve applications",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get detailed application information for review
    /// GET /api/vetting/reviewer/applications/{id}
    /// </summary>
    private static async Task<IResult> GetApplicationDetail(
        Guid id,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims with fallback for administrators
            var reviewerIdClaim = user.FindFirst("ReviewerId")?.Value;
            Guid reviewerId;

            if (!string.IsNullOrEmpty(reviewerIdClaim) && Guid.TryParse(reviewerIdClaim, out reviewerId))
            {
                // Use specific reviewer ID if available
            }
            else
            {
                // Fallback: Use user ID for administrators
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out reviewerId))
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "User information not found",
                        Timestamp = DateTime.UtcNow
                    }, statusCode: 400);
                }
            }

            var result = await vettingService.GetApplicationDetailAsync(id, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationDetailResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application detail retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 500;

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve application detail",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Submit a review decision for an application
    /// POST /api/vetting/reviewer/applications/{id}/decisions
    /// </summary>
    private static async Task<IResult> SubmitReviewDecision(
        Guid id,
        ReviewDecisionRequest request,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims with fallback for administrators
            var reviewerIdClaim = user.FindFirst("ReviewerId")?.Value;
            Guid reviewerId;

            if (!string.IsNullOrEmpty(reviewerIdClaim) && Guid.TryParse(reviewerIdClaim, out reviewerId))
            {
                // Use specific reviewer ID if available
            }
            else
            {
                // Fallback: Use user ID for administrators
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out reviewerId))
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "User information not found",
                        Timestamp = DateTime.UtcNow
                    }, statusCode: 400);
                }
            }

            var result = await vettingService.SubmitReviewDecisionAsync(id, request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ReviewDecisionResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Review decision submitted successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to submit review decision",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Add a note to an application
    /// POST /api/vetting/reviewer/applications/{id}/notes
    /// </summary>
    private static async Task<IResult> AddApplicationNote(
        Guid id,
        CreateNoteRequest request,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims with fallback for administrators
            var reviewerIdClaim = user.FindFirst("ReviewerId")?.Value;
            Guid reviewerId;

            if (!string.IsNullOrEmpty(reviewerIdClaim) && Guid.TryParse(reviewerIdClaim, out reviewerId))
            {
                // Use specific reviewer ID if available
            }
            else
            {
                // Fallback: Use user ID for administrators
                var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out reviewerId))
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "User information not found",
                        Timestamp = DateTime.UtcNow
                    }, statusCode: 400);
                }
            }

            var result = await vettingService.AddApplicationNoteAsync(id, request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<NoteResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Note added successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: statusCode);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to add note",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }
}