using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.Safety.Endpoints;

/// <summary>
/// Safety incident reporting minimal API endpoints
/// Follows simplified vertical slice pattern with direct service injection
/// Extended with all 12 endpoints for comprehensive incident management
/// </summary>
public static class SafetyEndpoints
{
    /// <summary>
    /// Register safety endpoints using minimal API pattern
    /// </summary>
    public static void MapSafetyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/safety")
            .WithTags("Safety");

        #region Public Endpoints

        // Public endpoint for incident submission (anonymous or authenticated)
        group.MapPost("/incidents", async (
            CreateIncidentRequest request,
            [FromServices] ISafetyService safetyService,
            [FromServices] IValidator<CreateIncidentRequest> validator,
            CancellationToken cancellationToken) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await safetyService.SubmitIncidentAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Incident Submission Failed");
        })
        .WithName("SubmitIncident")
        .WithSummary("Submit safety incident report")
        .WithDescription("Submit a new safety incident report (anonymous or identified)")
        .Produces<SubmissionResponse>(200)
        .ProducesProblem(400)
        .ProducesProblem(422)
        .DisableAntiforgery(); // Public endpoint - no CSRF token available

        // Public endpoint for anonymous incident tracking
        group.MapGet("/incidents/{referenceNumber}/status", async (
            string referenceNumber,
            [FromServices] ISafetyService safetyService,
            CancellationToken cancellationToken) =>
        {
            var result = await safetyService.GetIncidentStatusAsync(referenceNumber, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Resource Not Found");
        })
        .WithName("GetIncidentStatus")
        .WithSummary("Get incident status for tracking")
        .WithDescription("Get current status of incident by reference number (public access)")
        .Produces<IncidentStatusResponse>(200)
        .ProducesProblem(404);

        #endregion

        #region Admin Dashboard & List (Phase 2)

        // Admin/Coordinator paginated incident list with filters
        group.MapGet("/admin/incidents", async (
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            [AsParameters] AdminIncidentListRequest request,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetIncidentsAsync(request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Incident List Retrieval Failed");
        })
        .WithName("GetAdminIncidentsList")
        .WithSummary("Get paginated incident list with filters")
        .WithDescription("Get filtered and paginated list of incidents (Admin: all, Coordinator: assigned only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<PaginatedIncidentListResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(500);

        // Dashboard statistics
        group.MapGet("/admin/dashboard/statistics", async (
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetDashboardStatisticsAsync(userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Dashboard Statistics Failed");
        })
        .WithName("GetDashboardStatistics")
        .WithSummary("Get dashboard statistics")
        .WithDescription("Get unassigned count, old unassigned flag, and recent incidents")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<DashboardStatisticsResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(500);

        // Get all users for coordinator assignment dropdown
        group.MapGet("/admin/users/coordinators", async (
            [FromServices] ISafetyServiceExtended safetyService,
            CancellationToken cancellationToken) =>
        {
            var result = await safetyService.GetAllUsersForAssignmentAsync(cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("User List Retrieval Failed");
        })
        .WithName("GetCoordinatorsList")
        .WithSummary("Get all users for coordinator assignment")
        .WithDescription("Get list of all users with active incident counts for assignment dropdown")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IEnumerable<UserCoordinatorDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(500);

        // OLD Dashboard endpoint (kept for compatibility)
        group.MapGet("/admin/dashboard", async (
            [FromServices] ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetDashboardDataAsync(userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Dashboard Load Failed");
        })
        .WithName("GetSafetyDashboard")
        .WithSummary("Get safety team dashboard data (legacy)")
        .WithDescription("Get dashboard statistics and recent incidents for safety team")
        .RequireAuthorization()
        .Produces<AdminDashboardResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(500);

        #endregion

        #region Incident Detail & Management (Phase 3)

        // Safety team incident detail endpoint
        group.MapGet("/admin/incidents/{incidentId:guid}", async (
            Guid incidentId,
            [FromServices] ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetIncidentDetailAsync(incidentId, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Incident Retrieval Failed");
        })
        .WithName("GetIncidentDetail")
        .WithSummary("Get detailed incident information")
        .WithDescription("Get full incident details with decrypted data for safety team")
        .RequireAuthorization()
        .Produces<IncidentResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404);

        // Assign coordinator to incident
        group.MapPost("/admin/incidents/{incidentId:guid}/assign", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            AssignCoordinatorRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<AssignCoordinatorRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.AssignCoordinatorAsync(incidentId, request, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Assignment Failed");
        })
        .WithName("AssignCoordinator")
        .WithSummary("Assign coordinator to incident")
        .WithDescription("Assign or unassign coordinator (Admin only)")
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
        .Produces<IncidentSummaryDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update incident status
        group.MapPut("/admin/incidents/{incidentId:guid}/status", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            UpdateStatusRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<UpdateStatusRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.UpdateStatusAsync(incidentId, request, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Status Update Failed");
        })
        .WithName("UpdateIncidentStatus")
        .WithSummary("Update incident status")
        .WithDescription("Update status with optional reason (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<StatusUpdateResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update Google Drive links
        group.MapPut("/admin/incidents/{incidentId:guid}/google-drive", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            UpdateGoogleDriveRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<UpdateGoogleDriveRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.UpdateGoogleDriveLinksAsync(incidentId, request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Google Drive Update Failed");
        })
        .WithName("UpdateGoogleDriveLinks")
        .WithSummary("Update Google Drive links")
        .WithDescription("Update folder and final report URLs (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<GoogleDriveUpdateResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update involved parties and witnesses
        group.MapPut("/admin/incidents/{incidentId:guid}/people", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            UpdatePeopleRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<UpdatePeopleRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.UpdatePeopleAsync(incidentId, request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("People Update Failed");
        })
        .WithName("UpdateIncidentPeople")
        .WithSummary("Update involved parties and witnesses")
        .WithDescription("Update involved parties and/or witnesses information (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<UpdatePeopleResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update incident title
        group.MapPut("/admin/incidents/{incidentId:guid}/title", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            UpdateTitleRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<UpdateTitleRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.UpdateTitleAsync(incidentId, request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Title Update Failed");
        })
        .WithName("UpdateIncidentTitle")
        .WithSummary("Update incident title")
        .WithDescription("Update the title of an incident (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<UpdateTitleResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        #endregion

        #region Notes System (Phase 4)

        // Get all notes for incident
        group.MapGet("/admin/incidents/{incidentId:guid}/notes", async (
            Guid incidentId,
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetNotesAsync(incidentId, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Notes Retrieval Failed");
        })
        .WithName("GetIncidentNotes")
        .WithSummary("Get all notes for incident")
        .WithDescription("Get notes with privacy filtering (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<NotesListResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Add manual note to incident
        group.MapPost("/admin/incidents/{incidentId:guid}/notes", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid incidentId,
            AddNoteRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<AddNoteRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.AddNoteAsync(incidentId, request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Note Addition Failed");
        })
        .WithName("AddIncidentNote")
        .WithSummary("Add manual note to incident")
        .WithDescription("Add coordinator/admin note to incident")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IncidentNoteDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update manual note
        group.MapPut("/admin/notes/{noteId:guid}", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid noteId,
            UpdateNoteRequest request,
            [FromServices] ISafetyServiceExtended safetyService,
            [FromServices] IValidator<UpdateNoteRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.UpdateNoteAsync(noteId, request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Note Update Failed");
        })
        .WithName("UpdateIncidentNote")
        .WithSummary("Update manual note")
        .WithDescription("Update existing manual note (author or admin only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IncidentNoteDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete manual note
        group.MapDelete("/admin/notes/{noteId:guid}", async (
            HttpContext context,
            IAntiforgery antiforgery,
            Guid noteId,
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate CSRF token
            var csrfResult = await antiforgery.ValidateAsync(context);
            if (!csrfResult.IsSuccess)
                return csrfResult.ToProblem("CSRF Validation Failed");

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.DeleteNoteAsync(noteId, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToProblem("Note Deletion Failed");
        })
        .WithName("DeleteIncidentNote")
        .WithSummary("Delete manual note")
        .WithDescription("Delete manual note (author or admin only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces(204)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        #endregion

        #region My Reports (Phase 5)

        // Get user's own reports with pagination
        group.MapGet("/my-reports", async (
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var result = await safetyService.GetMyReportsAsync(userId, page, pageSize, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("My Reports Retrieval Failed");
        })
        .WithName("GetMyReports")
        .WithSummary("Get user's own reports (paginated)")
        .WithDescription("Get list of current user's incident reports with limited view")
        .RequireAuthorization()
        .Produces<MyReportsPaginatedResponse>(200)
        .ProducesProblem(401)
        .ProducesProblem(500);

        // Get user's own report detail
        group.MapGet("/my-reports/{incidentId:guid}", async (
            Guid incidentId,
            [FromServices] ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var result = await safetyService.GetMyReportDetailAsync(incidentId, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem("Report Detail Retrieval Failed");
        })
        .WithName("GetMyReportDetail")
        .WithSummary("Get user's own report detail")
        .WithDescription("Get detailed view of user's own incident report (limited fields)")
        .RequireAuthorization()
        .Produces<MyReportDetailDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        #endregion
    }
}
