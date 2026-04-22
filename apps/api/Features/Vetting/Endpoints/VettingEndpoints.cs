using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Shared.Extensions;

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
            .Produces<ApplicationSubmissionResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .ProducesProblem(500)
            .DisableAntiforgery(); // Public endpoint - no CSRF token available

        // GET: Check application status by token (public)
        publicGroup.MapGet("/applications/status/{token}", GetApplicationStatusByToken)
            .WithName("GetApplicationStatusByToken")
            .WithSummary("Check application status using status token")
            .Produces<ApplicationStatusResponse>(200)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // GET: Paginated list of applications for reviewers
        group.MapPost("/reviewer/applications", GetApplicationsForReview)
            .WithName("GetApplicationsForReview")
            .WithSummary("Get paginated list of vetting applications")
            .Produces<PagedResult<ApplicationSummaryDto>>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(500);

        // GET: Application detail for review
        group.MapGet("/reviewer/applications/{id}", GetApplicationDetail)
            .WithName("GetApplicationDetail")
            .WithSummary("Get detailed application information")
            .Produces<ApplicationDetailResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Submit review decision
        group.MapPost("/reviewer/applications/{id}/decisions", SubmitReviewDecision)
            .WithName("SubmitReviewDecision")
            .WithSummary("Submit a review decision for an application")
            .Produces<ReviewDecisionResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Add note to application
        group.MapPost("/reviewer/applications/{id}/notes", AddApplicationNote)
            .WithName("AddApplicationNote")
            .WithSummary("Add a note to an application")
            .Produces<NoteResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // Direct action endpoints that frontend expects
        // POST: Approve application
        // SECURITY: CSRF protection enabled automatically via middleware
        // CRITICAL: Vetting approval must validate CSRF tokens to prevent unauthorized member access
        group.MapPost("/applications/{id}/approve", ApproveApplication)
            .WithName("ApproveApplication")
            .WithSummary("Approve an application")
            .Produces<ReviewDecisionResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // PUT: Change application status
        // SECURITY: CSRF protection enabled automatically via middleware
        // Prevents unauthorized vetting status changes via CSRF attacks
        group.MapPut("/applications/{id}/status", ChangeApplicationStatus)
            .WithName("ChangeApplicationStatus")
            .WithSummary("Change application status (for OnHold, etc.)")
            .Produces<ReviewDecisionResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Add note with simple structure
        group.MapPost("/applications/{id}/notes", AddSimpleApplicationNote)
            .WithName("AddSimpleApplicationNote")
            .WithSummary("Add a simple note to an application")
            .Produces<NoteResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Deny application
        // SECURITY: CSRF protection enabled automatically via middleware
        // CRITICAL: Vetting denial must validate CSRF tokens to prevent unauthorized rejections
        group.MapPost("/applications/{id}/deny", DenyApplication)
            .WithName("DenyApplication")
            .WithSummary("Deny an application")
            .Produces<ReviewDecisionResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Send interview reminder email to applicant
        // SECURITY: CSRF protection enabled automatically via middleware
        // Only available for InterviewApproved applications
        group.MapPost("/reviewer/applications/{id}/send-reminder", SendReminder)
            .WithName("SendReminder")
            .WithSummary("Send interview reminder email to applicant")
            .Produces<SendReminderResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // PUT: Update applicant information on a vetting application (admin only)
        // SECURITY: CSRF protection enabled automatically via middleware
        // Only updates the VettingApplication entity, not the User entity
        group.MapPut("/reviewer/applications/{id}/applicant-info", UpdateApplicantInfo)
            .WithName("UpdateApplicantInfo")
            .WithSummary("Update applicant contact/identity information on a vetting application")
            .Produces<ApplicationDetailResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // DELETE: Soft-delete a vetting application (admin/vetting team only)
        // SECURITY: Requires authentication. Only Administrator and VettingTeam roles can delete.
        // This is a soft delete — the application data is preserved for audit purposes
        // but excluded from all queries. The applicant's email becomes available for resubmission.
        group.MapDelete("/reviewer/applications/{id}", SoftDeleteApplication)
            .WithName("SoftDeleteApplication")
            .WithSummary("Soft-delete a vetting application")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // GET: Current user's vetting status
        group.MapGet("/status", GetVettingStatus)
            .WithName("GetVettingStatus")
            .WithSummary("Get current user's vetting status")
            .Produces<MyApplicationStatusResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);

        // GET: Current user's vetting application details
        group.MapGet("/application", GetMyVettingApplication)
            .WithName("GetMyVettingApplication")
            .WithSummary("Get current user's vetting application details")
            .Produces<ApplicationDetailResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // POST: Submit simplified vetting application (authenticated user)
        group.MapPost("/applications/simplified", SubmitSimplifiedApplication)
            .WithName("SubmitSimplifiedApplication")
            .WithSummary("Submit a simplified vetting application from authenticated user")
            .Produces<ApplicationSubmissionResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(409)
            .ProducesProblem(500);

        // GET: Check if current user already has a submitted application
        group.MapGet("/my-application", GetMyApplication)
            .WithName("GetMyApplication")
            .WithSummary("Check if current user has an existing application")
            .Produces<SimplifiedApplicationResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);
    }

    /// <summary>
    /// Get paginated list of applications for admin/reviewer dashboard
    /// POST /api/vetting/reviewer/applications
    /// </summary>
    private static async Task<IResult> GetApplicationsForReview(
        HttpContext context,
        IAntiforgery antiforgery,
        ApplicationFilterRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.GetApplicationsForReviewAsync(request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 : 500;
            return result.ToProblem("Failed to Retrieve Applications");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Applications",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get detailed application information for review
    /// GET /api/vetting/reviewer/applications/{id}
    /// </summary>
    private static async Task<IResult> GetApplicationDetail(
        Guid id,
        [FromServices] IVettingService vettingService,
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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.GetApplicationDetailAsync(id, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 500;

            return result.ToProblem("Failed to Retrieve Application Detail");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Application Detail",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Submit a review decision for an application
    /// POST /api/vetting/reviewer/applications/{id}/decisions
    /// </summary>
    private static async Task<IResult> SubmitReviewDecision(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        ReviewDecisionRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.SubmitReviewDecisionAsync(id, request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Submit Review Decision");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Submit Review Decision",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Add a note to an application
    /// POST /api/vetting/reviewer/applications/{id}/notes
    /// </summary>
    private static async Task<IResult> AddApplicationNote(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        CreateNoteRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.AddApplicationNoteAsync(id, request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Add Note");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Add Note",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Simple approve application endpoint
    /// POST /api/vetting/applications/{id}/approve
    /// </summary>
    private static async Task<IResult> ApproveApplication(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        SimpleReasoningRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            // Call dedicated ApproveApplicationAsync method
            var result = await vettingService.ApproveApplicationAsync(id, reviewerId, request.Reasoning, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Approve Application");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Approve Application",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Change application status endpoint
    /// PUT /api/vetting/applications/{id}/status
    /// </summary>
    private static async Task<IResult> ChangeApplicationStatus(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        StatusChangeRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        try
        {
            // Check if user has Administrator role FIRST - before extracting user ID
            // Multi-role support: IsInRole checks all role claims in JWT
            if (!user.IsInRole(UserRole.Administrator.ToRoleString()))
            {
                return Results.Problem( // ARCH-ALLOW: role/ownership guard — forbidden, not a service Result
                    title: "Access Denied",
                    detail: "Only administrators can change application status",
                    statusCode: 403);
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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            // Parse status enum from string
            if (!Enum.TryParse<VettingStatus>(request.Status, out var targetStatus))
            {
                return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                    title: "Invalid Status",
                    detail: $"'{request.Status}' is not a valid vetting status",
                    statusCode: 400);
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
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Change Application Status");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Change Application Status",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Add simple note to application endpoint
    /// POST /api/vetting/applications/{id}/notes
    /// </summary>
    private static async Task<IResult> AddSimpleApplicationNote(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        SimpleNoteRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
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
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Add Note");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Add Note",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Deny application endpoint
    /// POST /api/vetting/applications/{id}/deny
    /// </summary>
    private static async Task<IResult> DenyApplication(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        SimpleReasoningRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            // Validate reasoning is provided
            if (string.IsNullOrWhiteSpace(request.Reasoning))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["reasoning"] = ["A reason must be provided when denying an application"]
                });

            // Call dedicated DenyApplicationAsync method
            var result = await vettingService.DenyApplicationAsync(id, request.Reasoning, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Deny Application");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Deny Application",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Send interview reminder email to applicant
    /// POST /api/vetting/reviewer/applications/{id}/send-reminder
    /// Uses the InterviewReminder email template with variables populated from the application
    /// </summary>
    private static async Task<IResult> SendReminder(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        SendReminderRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.SendReminderAsync(id, request.CustomMessage, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Send Reminder");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Send Reminder",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Update applicant contact/identity information on a vetting application.
    /// PUT /api/vetting/reviewer/applications/{id}/applicant-info
    /// Only updates the VettingApplication entity, not the User entity.
    /// </summary>
    private static async Task<IResult> UpdateApplicantInfo(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        UpdateApplicationApplicantInfoRequest request,
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

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
                    return Results.Problem( // ARCH-ALLOW: request-level inline validation — rejecting malformed input before service call
                        title: "User Information Not Found",
                        detail: "User information not found",
                        statusCode: 400);
                }
            }

            var result = await vettingService.UpdateApplicantInfoAsync(
                id, request, reviewerId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Update Applicant Info");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Update Applicant Info",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Soft-delete a vetting application. Preserves data for audit but hides from all views.
    /// Resets the associated user's VettingStatus so they can reapply.
    /// DELETE /api/vetting/reviewer/applications/{id}
    /// </summary>
    private static async Task<IResult> SoftDeleteApplication(
        Guid id,
        IVettingService vettingService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                title: "Authentication Error",
                detail: "Unable to identify the current user.",
                statusCode: 401);
        }

        var result = await vettingService.SoftDeleteApplicationAsync(id, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error.Contains("Access denied") ? 403 :
                           result.Error.Contains("not found") ? 404 : 400;

            return result.ToProblem("Failed to Delete Application");
        }

        return Results.Ok(new { message = "Application deleted successfully" });
    }

    /// <summary>
    /// Get current user's vetting status
    /// GET /api/vetting/status
    /// </summary>
    private static async Task<IResult> GetVettingStatus(
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "User Information Not Found",
                    detail: "User information not found",
                    statusCode: 401);
            }

            var result = await vettingService.GetMyApplicationStatusAsync(userId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            return result.ToProblem("Failed to Retrieve Vetting Status");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Vetting Status",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get current user's vetting application details
    /// GET /api/vetting/application
    /// </summary>
    private static async Task<IResult> GetMyVettingApplication(
        [FromServices] IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "User Information Not Found",
                    detail: "User information not found",
                    statusCode: 401);
            }

            var result = await vettingService.GetMyApplicationDetailAsync(userId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("not found") ? 404 : 500;
            return result.ToProblem("Failed to Retrieve Application Details");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Application Details",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get application status by token (public endpoint)
    /// GET /api/vetting/public/applications/status/{token}
    /// </summary>
    private static async Task<IResult> GetApplicationStatusByToken(
        string token,
        [FromServices] IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await vettingService.GetApplicationStatusByTokenAsync(token, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Ok(result.Value);
            }

            var statusCode = result.Error.Contains("not found") ? 404 : 500;
            return result.ToProblem("Failed to Retrieve Application Status");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Application Status",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Submit simplified public vetting application (public endpoint)
    /// POST /api/vetting/public/applications
    /// </summary>
    private static async Task<IResult> SubmitPublicApplication(
        PublicApplicationSubmissionRequest request,
        [FromServices] IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicate application by email
            var existingAppResult = await vettingService.GetApplicationByEmailAsync(request.Email, cancellationToken);

            if (existingAppResult.IsSuccess && existingAppResult.Value != null)
            {
                return Results.Problem( // ARCH-ALLOW: pre-check Conflict derived from lookup Result — not the primary service Result
                    title: "Duplicate Application",
                    detail: "An application already exists for this email",
                    statusCode: 409);
            }

            // Create application
            var result = await vettingService.SubmitPublicApplicationAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Created($"/api/vetting/public/applications/status/{result.Value.StatusToken}", result.Value);
            }

            return result.ToProblem("Failed to Submit Application");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Submit Application",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Submit simplified vetting application from authenticated user
    /// POST /api/vetting/applications/simplified
    /// </summary>
    private static async Task<IResult> SubmitSimplifiedApplication(
        HttpContext context,
        IAntiforgery antiforgery,
        SimplifiedApplicationRequest request,
        ClaimsPrincipal user,
        [FromServices] IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        try
        {
            // Get user email from claims
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "User Email Not Found",
                    detail: "Unable to determine user email from authentication token",
                    statusCode: 401);
            }

            // Check for duplicate application by email
            var existingAppResult = await vettingService.GetApplicationByEmailAsync(request.Email, cancellationToken);

            if (existingAppResult.IsSuccess && existingAppResult.Value != null)
            {
                return Results.Problem( // ARCH-ALLOW: pre-check Conflict derived from lookup Result — not the primary service Result
                    title: "Duplicate Application",
                    detail: "You already have a submitted application. Only one application is allowed per person.",
                    statusCode: 409);
            }

            // Submit simplified application via service
            var result = await vettingService.SubmitSimplifiedApplicationAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                return Results.Created($"/api/vetting/my-application", result.Value);
            }

            return result.ToProblem("Failed to Submit Application");
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Submit Application",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get current user's application if it exists
    /// GET /api/vetting/my-application
    /// </summary>
    private static async Task<IResult> GetMyApplication(
        ClaimsPrincipal user,
        [FromServices] IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user email from claims
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Results.Problem( // ARCH-ALLOW: unreachable auth guard — [Authorize] already enforces authentication
                    title: "User Email Not Found",
                    detail: "Unable to determine user email from authentication token",
                    statusCode: 401);
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
                    OtherNames = application.OtherNames,
                    FirstName = application.FirstName ?? string.Empty,
                    LastName = application.LastName ?? string.Empty,
                    Email = application.Email,
                    PreferredSceneName = application.SceneName ?? string.Empty,
                    FetLifeHandle = application.FetLifeHandle,
                    WhyJoin = application.WhyJoinCommunity ?? string.Empty,
                    ExperienceWithRope = application.ExperienceDescription ?? string.Empty,
                    AgreeToCommunityStandards = application.AgreesToGuidelines,
                    Status = application.WorkflowStatus.ToString()
                };

                return Results.Ok(response);
            }

            return Results.Problem( // ARCH-ALLOW: service returned Success with null — synthetic 404 outside failure path
                title: "Application Not Found",
                detail: "No application found for the current user",
                statusCode: 404);
        }
        catch (Exception ex)
        {
            return Results.Problem( // ARCH-ALLOW: handler catch-all — TD-BE-EXMESSAGE-LEAK
                title: "Failed to Retrieve Application",
                detail: ex.Message,
                statusCode: 500);
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
}
