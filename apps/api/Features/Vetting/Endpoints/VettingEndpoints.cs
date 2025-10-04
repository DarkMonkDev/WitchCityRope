using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Vetting.Entities;
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

        // Public endpoints (no authentication required)
        var publicGroup = app.MapGroup("/api/vetting/public")
            .WithTags("Vetting - Public")
            .AllowAnonymous();

        // POST: Submit new vetting application (public)
        publicGroup.MapPost("/applications", SubmitApplication)
            .WithName("SubmitVettingApplication")
            .WithSummary("Submit a new vetting application")
            .Produces<ApiResponse<ApplicationSubmissionResponse>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(500);

        // GET: Check application status by token (public)
        publicGroup.MapGet("/applications/status/{token}", GetApplicationStatusByToken)
            .WithName("GetApplicationStatusByToken")
            .WithSummary("Check application status using status token")
            .Produces<ApiResponse<ApplicationStatusResponse>>(200)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

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

        // Direct action endpoints that frontend expects
        // POST: Approve application
        group.MapPost("/applications/{id}/approve", ApproveApplication)
            .WithName("ApproveApplication")
            .WithSummary("Approve an application")
            .Produces<ApiResponse<ReviewDecisionResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // PUT: Change application status
        group.MapPut("/applications/{id}/status", ChangeApplicationStatus)
            .WithName("ChangeApplicationStatus")
            .WithSummary("Change application status (for OnHold, etc.)")
            .Produces<ApiResponse<ReviewDecisionResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // POST: Add note with simple structure
        group.MapPost("/applications/{id}/notes", AddSimpleApplicationNote)
            .WithName("AddSimpleApplicationNote")
            .WithSummary("Add a simple note to an application")
            .Produces<ApiResponse<NoteResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // POST: Deny application
        group.MapPost("/applications/{id}/deny", DenyApplication)
            .WithName("DenyApplication")
            .WithSummary("Deny an application")
            .Produces<ApiResponse<ReviewDecisionResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // GET: Current user's vetting status
        group.MapGet("/status", GetVettingStatus)
            .WithName("GetVettingStatus")
            .WithSummary("Get current user's vetting status")
            .Produces<ApiResponse<MyApplicationStatusResponse>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(500);

        // GET: Current user's vetting application details
        group.MapGet("/application", GetMyVettingApplication)
            .WithName("GetMyVettingApplication")
            .WithSummary("Get current user's vetting application details")
            .Produces<ApiResponse<ApplicationDetailResponse>>(200)
            .Produces<ApiResponse<object>>(401)
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

    /// <summary>
    /// Simple approve application endpoint
    /// POST /api/vetting/applications/{id}/approve
    /// </summary>
    private static async Task<IResult> ApproveApplication(
        Guid id,
        SimpleReasoningRequest request,
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

            // Call dedicated ApproveApplicationAsync method
            var result = await vettingService.ApproveApplicationAsync(id, reviewerId, request.Reasoning, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationDetailResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application approved successfully",
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
                Error = "Failed to approve application",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Change application status endpoint
    /// PUT /api/vetting/applications/{id}/status
    /// </summary>
    private static async Task<IResult> ChangeApplicationStatus(
        Guid id,
        StatusChangeRequest request,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if user has Administrator role FIRST - before extracting user ID
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Administrator")
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Access denied",
                    Details = "Only administrators can change application status",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 403);
            }

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

            // Parse status enum from string
            if (!Enum.TryParse<VettingStatus>(request.Status, out var targetStatus))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Invalid status",
                    Details = $"'{request.Status}' is not a valid vetting status",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            // Call UpdateApplicationStatusAsync for generic status changes
            var result = await vettingService.UpdateApplicationStatusAsync(
                id,
                targetStatus,
                request.Reasoning,
                reviewerId,
                cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationDetailResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application status updated successfully",
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
                Error = "Failed to change application status",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Add simple note to application endpoint
    /// POST /api/vetting/applications/{id}/notes
    /// </summary>
    private static async Task<IResult> AddSimpleApplicationNote(
        Guid id,
        SimpleNoteRequest request,
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

            var noteRequest = new CreateNoteRequest
            {
                Content = request.Note,
                Type = 1, // General note
                IsPrivate = request.IsPrivate ?? true,
                Tags = request.Tags ?? new List<string>()
            };

            var result = await vettingService.AddApplicationNoteAsync(id, noteRequest, reviewerId, cancellationToken);

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

    /// <summary>
    /// Deny application endpoint
    /// POST /api/vetting/applications/{id}/deny
    /// </summary>
    private static async Task<IResult> DenyApplication(
        Guid id,
        SimpleReasoningRequest request,
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

            // Validate reasoning is provided
            if (string.IsNullOrWhiteSpace(request.Reasoning))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Reasoning required",
                    Details = "A reason must be provided when denying an application",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            // Call dedicated DenyApplicationAsync method
            var result = await vettingService.DenyApplicationAsync(id, request.Reasoning, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationDetailResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application denied successfully",
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
                Error = "Failed to deny application",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get current user's vetting status
    /// GET /api/vetting/status
    /// </summary>
    private static async Task<IResult> GetVettingStatus(
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "User information not found",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 401);
            }

            var result = await vettingService.GetMyApplicationStatusAsync(userId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<MyApplicationStatusResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Vetting status retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve vetting status",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get current user's vetting application details
    /// GET /api/vetting/application
    /// </summary>
    private static async Task<IResult> GetMyVettingApplication(
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "User information not found",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 401);
            }

            var result = await vettingService.GetMyApplicationDetailAsync(userId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationDetailResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application details retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("not found") ? 404 : 500;
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
                Error = "Failed to retrieve application details",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Submit a new vetting application (public endpoint)
    /// POST /api/vetting/public/applications
    /// </summary>
    private static async Task<IResult> SubmitApplication(
        CreateApplicationRequest request,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await vettingService.SubmitApplicationAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Created(
                    $"/api/vetting/public/applications/status/{result.Value.StatusToken}",
                    new ApiResponse<ApplicationSubmissionResponse>
                    {
                        Success = true,
                        Data = result.Value,
                        Message = "Application submitted successfully",
                        Timestamp = DateTime.UtcNow
                    });
            }

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error,
                Details = result.Details,
                Timestamp = DateTime.UtcNow
            }, statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to submit application",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get application status by token (public endpoint)
    /// GET /api/vetting/public/applications/status/{token}
    /// </summary>
    private static async Task<IResult> GetApplicationStatusByToken(
        string token,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await vettingService.GetApplicationStatusByTokenAsync(token, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(new ApiResponse<ApplicationStatusResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application status retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }

            var statusCode = result.Error.Contains("not found") ? 404 : 500;
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
                Error = "Failed to retrieve application status",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }
}