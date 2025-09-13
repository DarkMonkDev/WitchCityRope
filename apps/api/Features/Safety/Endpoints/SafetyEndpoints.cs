using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Safety.Endpoints;

/// <summary>
/// Safety incident reporting minimal API endpoints
/// Follows simplified vertical slice pattern with direct service injection
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
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetIncidentStatus")
        .WithSummary("Get incident status for tracking")
        .WithDescription("Get current status of incident by reference number (public access)")
        .Produces<IncidentStatusResponse>(200)
        .Produces(404);

        // Safety team dashboard endpoint
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
        .WithSummary("Get safety team dashboard data")
        .WithDescription("Get dashboard statistics and recent incidents for safety team")
        .RequireAuthorization()
        .Produces<AdminDashboardResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

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

        // User's personal incident reports endpoint
        group.MapGet("/my-reports", async (
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetUserReportsAsync(userId, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Reports Retrieval Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetUserReports")
        .WithSummary("Get user's incident reports")
        .WithDescription("Get list of incident reports submitted by current user")
        .RequireAuthorization()
        .Produces<IEnumerable<IncidentSummaryResponse>>(200)
        .Produces(401)
        .Produces(500);
    }
}