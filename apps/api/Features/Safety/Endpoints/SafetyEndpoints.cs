using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Users.Constants;

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
            ISafetyService safetyService,
            IValidator<CreateIncidentRequest> validator,
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
                : Results.Problem(
                    title: "Incident Submission Failed",
                    detail: result.Error,
                    statusCode: 400);
        })
        .WithName("SubmitIncident")
        .WithSummary("Submit safety incident report")
        .WithDescription("Submit a new safety incident report (anonymous or identified)")
        .Produces<SubmissionResponse>(200)
        .Produces(400)
        .Produces(422);

        // Public endpoint for anonymous incident tracking
        group.MapGet("/incidents/{referenceNumber}/status", async (
            string referenceNumber,
            ISafetyService safetyService,
            CancellationToken cancellationToken) =>
        {
            var result = await safetyService.GetIncidentStatusAsync(referenceNumber, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Resource Not Found",
                    detail: result.Error,
                    statusCode: 404);
        })
        .WithName("GetIncidentStatus")
        .WithSummary("Get incident status for tracking")
        .WithDescription("Get current status of incident by reference number (public access)")
        .Produces<IncidentStatusResponse>(200)
        .Produces(404);

        #endregion

        #region Admin Dashboard & List (Phase 2)

        // Admin/Coordinator paginated incident list with filters
        group.MapGet("/admin/incidents", async (
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            [AsParameters] AdminIncidentListRequest request,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetIncidentsAsync(request, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Incident List Retrieval Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetAdminIncidentsList")
        .WithSummary("Get paginated incident list with filters")
        .WithDescription("Get filtered and paginated list of incidents (Admin: all, Coordinator: assigned only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<PaginatedIncidentListResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // Dashboard statistics
        group.MapGet("/admin/dashboard/statistics", async (
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetDashboardStatisticsAsync(userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Dashboard Statistics Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetDashboardStatistics")
        .WithSummary("Get dashboard statistics")
        .WithDescription("Get unassigned count, old unassigned flag, and recent incidents")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<DashboardStatisticsResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // Get all users for coordinator assignment dropdown
        group.MapGet("/admin/users/coordinators", async (
            ISafetyServiceExtended safetyService,
            CancellationToken cancellationToken) =>
        {
            var result = await safetyService.GetAllUsersForAssignmentAsync(cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "User List Retrieval Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetCoordinatorsList")
        .WithSummary("Get all users for coordinator assignment")
        .WithDescription("Get list of all users with active incident counts for assignment dropdown")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IEnumerable<UserCoordinatorDto>>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // OLD Dashboard endpoint (kept for compatibility)
        group.MapGet("/admin/dashboard", async (
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetDashboardDataAsync(userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Dashboard Load Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : 500);
        })
        .WithName("GetSafetyDashboard")
        .WithSummary("Get safety team dashboard data (legacy)")
        .WithDescription("Get dashboard statistics and recent incidents for safety team")
        .RequireAuthorization()
        .Produces<AdminDashboardResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        #endregion

        #region Incident Detail & Management (Phase 3)

        // Safety team incident detail endpoint
        group.MapGet("/admin/incidents/{incidentId:guid}", async (
            Guid incidentId,
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetIncidentDetailAsync(incidentId, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Incident Retrieval Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : 404);
        })
        .WithName("GetIncidentDetail")
        .WithSummary("Get detailed incident information")
        .WithDescription("Get full incident details with decrypted data for safety team")
        .RequireAuthorization()
        .Produces<IncidentResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Assign coordinator to incident
        group.MapPost("/admin/incidents/{incidentId:guid}/assign", async (
            Guid incidentId,
            AssignCoordinatorRequest request,
            ISafetyServiceExtended safetyService,
            IValidator<AssignCoordinatorRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.AssignCoordinatorAsync(incidentId, request, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Assignment Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("AssignCoordinator")
        .WithSummary("Assign coordinator to incident")
        .WithDescription("Assign or unassign coordinator (Admin only)")
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
        .Produces<IncidentSummaryDto>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(422)
        .Produces(500);

        // Update incident status
        group.MapPut("/admin/incidents/{incidentId:guid}/status", async (
            Guid incidentId,
            UpdateStatusRequest request,
            ISafetyServiceExtended safetyService,
            IValidator<UpdateStatusRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.UpdateStatusAsync(incidentId, request, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Status Update Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("UpdateIncidentStatus")
        .WithSummary("Update incident status")
        .WithDescription("Update status with optional reason (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<StatusUpdateResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(422)
        .Produces(500);

        // Update Google Drive links
        group.MapPut("/admin/incidents/{incidentId:guid}/google-drive", async (
            Guid incidentId,
            UpdateGoogleDriveRequest request,
            ISafetyServiceExtended safetyService,
            IValidator<UpdateGoogleDriveRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
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
                : Results.Problem(
                    title: "Google Drive Update Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("UpdateGoogleDriveLinks")
        .WithSummary("Update Google Drive links")
        .WithDescription("Update folder and final report URLs (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<GoogleDriveUpdateResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(422)
        .Produces(500);

        #endregion

        #region Notes System (Phase 4)

        // Get all notes for incident
        group.MapGet("/admin/incidents/{incidentId:guid}/notes", async (
            Guid incidentId,
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.GetNotesAsync(incidentId, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Notes Retrieval Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("GetIncidentNotes")
        .WithSummary("Get all notes for incident")
        .WithDescription("Get notes with privacy filtering (Admin/Coordinator)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<NotesListResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // Add manual note to incident
        group.MapPost("/admin/incidents/{incidentId:guid}/notes", async (
            Guid incidentId,
            AddNoteRequest request,
            ISafetyServiceExtended safetyService,
            IValidator<AddNoteRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
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
                : Results.Problem(
                    title: "Note Addition Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("AddIncidentNote")
        .WithSummary("Add manual note to incident")
        .WithDescription("Add coordinator/admin note to incident")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IncidentNoteDto>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(422)
        .Produces(500);

        // Update manual note
        group.MapPut("/admin/notes/{noteId:guid}", async (
            Guid noteId,
            UpdateNoteRequest request,
            ISafetyServiceExtended safetyService,
            IValidator<UpdateNoteRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
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
                : Results.Problem(
                    title: "Note Update Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("only edit your own") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("UpdateIncidentNote")
        .WithSummary("Update manual note")
        .WithDescription("Update existing manual note (author or admin only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces<IncidentNoteDto>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(422)
        .Produces(500);

        // Delete manual note
        group.MapDelete("/admin/notes/{noteId:guid}", async (
            Guid noteId,
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var isAdmin = user.IsInRole("Administrator");

            var result = await safetyService.DeleteNoteAsync(noteId, userId, isAdmin, cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: "Note Deletion Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("only delete your own") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("DeleteIncidentNote")
        .WithSummary("Delete manual note")
        .WithDescription("Delete manual note (author or admin only)")
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString(),
            UserRole.SafetyTeam.ToRoleString())) // SafetyTeam members are coordinators
        .Produces(204)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        #endregion

        #region My Reports (Phase 5)

        // Get user's own reports with pagination
        group.MapGet("/my-reports", async (
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var result = await safetyService.GetMyReportsAsync(userId, page, pageSize, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "My Reports Retrieval Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetMyReports")
        .WithSummary("Get user's own reports (paginated)")
        .WithDescription("Get list of current user's incident reports with limited view")
        .RequireAuthorization()
        .Produces<MyReportsPaginatedResponse>(200)
        .Produces(401)
        .Produces(500);

        // Get user's own report detail
        group.MapGet("/my-reports/{incidentId:guid}", async (
            Guid incidentId,
            ISafetyServiceExtended safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var result = await safetyService.GetMyReportDetailAsync(incidentId, userId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Report Detail Retrieval Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("only view your own") ? 403 : result.Error.Contains("not found") ? 404 : 500);
        })
        .WithName("GetMyReportDetail")
        .WithSummary("Get user's own report detail")
        .WithDescription("Get detailed view of user's own incident report (limited fields)")
        .RequireAuthorization()
        .Produces<MyReportDetailDto>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        #endregion
    }
}