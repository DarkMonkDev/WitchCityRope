using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Users.Constants;

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

        // POST: Submit new vetting application (public - simplified for E2E testing)
        publicGroup.MapPost("/applications", SubmitPublicApplication)
            .WithName("SubmitPublicVettingApplication")
            .WithSummary("Submit a simplified vetting application (public endpoint)")
            .Produces<ApiResponse<ApplicationSubmissionResponse>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(409)
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

        // POST: Submit simplified vetting application (authenticated user)
        group.MapPost("/applications/simplified", SubmitSimplifiedApplication)
            .WithName("SubmitSimplifiedApplication")
            .WithSummary("Submit a simplified vetting application from authenticated user")
            .Produces<ApiResponse<ApplicationSubmissionResponse>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(409)
            .Produces<ApiResponse<object>>(500);

        // GET: Check if current user already has a submitted application
        group.MapGet("/my-application", GetMyApplication)
            .WithName("GetMyApplication")
            .WithSummary("Check if current user has an existing application")
            .Produces<ApiResponse<SimplifiedApplicationResponse>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // Email Template Management Endpoints (Admin only)
        // GET: Retrieve all email templates
        group.MapGet("/email-templates", GetEmailTemplates)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("GetEmailTemplates")
            .WithSummary("Retrieve all active email templates (Admin only)")
            .Produces<ApiResponse<List<EmailTemplateResponse>>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(500);

        // GET: Retrieve single email template by ID
        group.MapGet("/email-templates/{id}", GetEmailTemplate)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("GetEmailTemplate")
            .WithSummary("Retrieve a single email template by ID (Admin only)")
            .Produces<ApiResponse<EmailTemplateResponse>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // PUT: Update email template
        group.MapPut("/email-templates/{id}", UpdateEmailTemplate)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("UpdateEmailTemplate")
            .WithSummary("Update email template content (Admin only)")
            .Produces<ApiResponse<EmailTemplateResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
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
            if (userRole != UserRole.Administrator.ToRoleString())
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

    /// <summary>
    /// Submit simplified public vetting application (public endpoint)
    /// POST /api/vetting/public/applications
    /// </summary>
    private static async Task<IResult> SubmitPublicApplication(
        PublicApplicationSubmissionRequest request,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicate application by email
            var existingAppResult = await vettingService.GetApplicationByEmailAsync(request.Email, cancellationToken);

            if (existingAppResult.IsSuccess && existingAppResult.Value != null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Duplicate application",
                    Details = "An application already exists for this email",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 409);
            }

            // Create application
            var result = await vettingService.SubmitPublicApplicationAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Json(new ApiResponse<ApplicationSubmissionResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application submitted successfully",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 201);
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
    /// Submit simplified vetting application from authenticated user
    /// POST /api/vetting/applications/simplified
    /// </summary>
    private static async Task<IResult> SubmitSimplifiedApplication(
        SimplifiedApplicationRequest request,
        ClaimsPrincipal user,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user email from claims
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "User email not found",
                    Details = "Unable to determine user email from authentication token",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 401);
            }

            // Check for duplicate application by email
            var existingAppResult = await vettingService.GetApplicationByEmailAsync(request.Email, cancellationToken);

            if (existingAppResult.IsSuccess && existingAppResult.Value != null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Duplicate application",
                    Details = "You already have a submitted application. Only one application is allowed per person.",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 409);
            }

            // Submit simplified application via service
            var result = await vettingService.SubmitSimplifiedApplicationAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Json(new ApiResponse<ApplicationSubmissionResponse>
                {
                    Success = true,
                    Data = result.Value,
                    Message = "Application submitted successfully",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 201);
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
    /// Get current user's application if it exists
    /// GET /api/vetting/my-application
    /// </summary>
    private static async Task<IResult> GetMyApplication(
        ClaimsPrincipal user,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user email from claims
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "User email not found",
                    Details = "Unable to determine user email from authentication token",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 401);
            }

            // Get application by email
            var result = await vettingService.GetApplicationByEmailAsync(userEmail, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                // Map to simplified response
                var application = result.Value;
                var response = new SimplifiedApplicationResponse
                {
                    ApplicationId = application.Id,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    SubmittedAt = application.SubmittedAt,
                    ConfirmationMessage = GetStatusMessage(application.WorkflowStatus),
                    EmailSent = false, // Not tracked in current implementation
                    NextSteps = GetNextStepsMessage(application.WorkflowStatus),
                    Pronouns = application.Pronouns,
                    OtherNames = application.AboutYourself
                };

                return Results.Json(new ApiResponse<SimplifiedApplicationResponse>
                {
                    Success = true,
                    Data = response,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Application not found",
                Details = "No application found for the current user",
                Timestamp = DateTime.UtcNow
            }, statusCode: 404);
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve application",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Helper method to get user-friendly status messages
    /// </summary>
    private static string GetStatusMessage(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.UnderReview => "Your application is currently being reviewed by our team.",
            VettingStatus.InterviewApproved => "You have been approved for an interview! Please check your email for the Calendly link to schedule.",
            VettingStatus.FinalReview => "Your application is under final review. We will contact you with a decision soon.",
            VettingStatus.Approved => "Congratulations! Your application has been approved.",
            VettingStatus.Denied => "Unfortunately, your application was not approved at this time.",
            VettingStatus.OnHold => "Your application is currently on hold. We will contact you with more information.",
            VettingStatus.Withdrawn => "Your application has been withdrawn.",
            _ => "Application status unknown."
        };
    }

    /// <summary>
    /// Helper method to get next steps information
    /// </summary>
    private static string GetNextStepsMessage(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.UnderReview => "Our team is reviewing your application. We'll contact you soon.",
            VettingStatus.InterviewApproved => "Please check your email for the Calendly link to schedule your interview.",
            VettingStatus.FinalReview => "Your application is under final review. We'll contact you with a decision soon.",
            VettingStatus.Approved => "Welcome to Witch City Rope! You can now access member features.",
            VettingStatus.Denied => "You may reapply in the future.",
            VettingStatus.OnHold => "Please address the items mentioned in your notification email.",
            VettingStatus.Withdrawn => "Your application has been withdrawn.",
            _ => "Please contact us for more information."
        };
    }

    /// <summary>
    /// Get all active email templates
    /// GET /api/vetting/email-templates
    /// Admin only
    /// </summary>
    private static async Task<IResult> GetEmailTemplates(
        ApplicationDbContext context,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Query all active templates, ordered by TemplateType
            var templates = await context.VettingEmailTemplates
                .Include(t => t.UpdatedByUser)
                .Where(t => t.IsActive)
                .OrderBy(t => t.TemplateType)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Map to response DTOs
            var response = templates.Select(t => new EmailTemplateResponse
            {
                Id = t.Id,
                TemplateType = (int)t.TemplateType,
                TemplateTypeName = t.TemplateType.ToString(),
                Subject = t.Subject,
                HtmlBody = t.HtmlBody,
                PlainTextBody = t.PlainTextBody,
                Variables = t.Variables,
                IsActive = t.IsActive,
                Version = t.Version,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastModified = t.LastModified,
                UpdatedBy = t.UpdatedBy,
                UpdatedByEmail = t.UpdatedByUser?.Email ?? "System"
            }).ToList();

            return Results.Ok(new ApiResponse<List<EmailTemplateResponse>>
            {
                Success = true,
                Data = response,
                Message = "Email templates retrieved successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve email templates",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get single email template by ID
    /// GET /api/vetting/email-templates/{id}
    /// Admin only
    /// </summary>
    private static async Task<IResult> GetEmailTemplate(
        Guid id,
        ApplicationDbContext context,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find template by ID
            var template = await context.VettingEmailTemplates
                .Include(t => t.UpdatedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (template == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Email template not found",
                    Details = $"No email template found with ID: {id}",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 404);
            }

            // Map to response DTO
            var response = new EmailTemplateResponse
            {
                Id = template.Id,
                TemplateType = (int)template.TemplateType,
                TemplateTypeName = template.TemplateType.ToString(),
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Variables = template.Variables,
                IsActive = template.IsActive,
                Version = template.Version,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                LastModified = template.LastModified,
                UpdatedBy = template.UpdatedBy,
                UpdatedByEmail = template.UpdatedByUser?.Email ?? "System"
            };

            return Results.Ok(new ApiResponse<EmailTemplateResponse>
            {
                Success = true,
                Data = response,
                Message = "Email template retrieved successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to retrieve email template",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }

    /// <summary>
    /// Update email template content
    /// PUT /api/vetting/email-templates/{id}
    /// Admin only
    /// </summary>
    private static async Task<IResult> UpdateEmailTemplate(
        Guid id,
        UpdateEmailTemplateRequest request,
        ApplicationDbContext context,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Invalid request",
                    Details = "Subject is required",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            if (string.IsNullOrWhiteSpace(request.HtmlBody))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Invalid request",
                    Details = "HtmlBody is required",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            if (string.IsNullOrWhiteSpace(request.PlainTextBody))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Invalid request",
                    Details = "PlainTextBody is required",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            // Extract user ID from JWT claims
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "User information not found",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 400);
            }

            // Find template by ID
            var template = await context.VettingEmailTemplates
                .Include(t => t.UpdatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (template == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Email template not found",
                    Details = $"No email template found with ID: {id}",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 404);
            }

            // Update template properties
            template.Subject = request.Subject.Trim();
            template.HtmlBody = request.HtmlBody.Trim();
            template.PlainTextBody = request.PlainTextBody.Trim();
            template.Version++;
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = userId;
            template.LastModified = DateTime.UtcNow;

            // Explicitly mark entity as modified to ensure EF Core tracks the change
            // See backend-developer-lessons-learned-2.md lines 1211-1320 for pattern explanation
            context.VettingEmailTemplates.Update(template);

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);

            // Reload template with updated user information
            var updatedTemplate = await context.VettingEmailTemplates
                .Include(t => t.UpdatedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            // Map to response DTO
            var response = new EmailTemplateResponse
            {
                Id = updatedTemplate!.Id,
                TemplateType = (int)updatedTemplate.TemplateType,
                TemplateTypeName = updatedTemplate.TemplateType.ToString(),
                Subject = updatedTemplate.Subject,
                HtmlBody = updatedTemplate.HtmlBody,
                PlainTextBody = updatedTemplate.PlainTextBody,
                Variables = updatedTemplate.Variables,
                IsActive = updatedTemplate.IsActive,
                Version = updatedTemplate.Version,
                CreatedAt = updatedTemplate.CreatedAt,
                UpdatedAt = updatedTemplate.UpdatedAt,
                LastModified = updatedTemplate.LastModified,
                UpdatedBy = updatedTemplate.UpdatedBy,
                UpdatedByEmail = updatedTemplate.UpdatedByUser?.Email ?? "System"
            };

            return Results.Ok(new ApiResponse<EmailTemplateResponse>
            {
                Success = true,
                Data = response,
                Message = "Email template updated successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "Failed to update email template",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }
}