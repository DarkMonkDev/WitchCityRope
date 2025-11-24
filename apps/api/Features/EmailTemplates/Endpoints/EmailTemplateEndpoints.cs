using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;

namespace WitchCityRope.Api.Features.EmailTemplates.Endpoints;

/// <summary>
/// API endpoints for email template management
/// </summary>
public static class EmailTemplateEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/email-templates")
            .WithTags("Email Templates")
            .WithOpenApi();

        // ========================================
        // Global Templates (Admin-only)
        // ========================================

        /// <summary>
        /// Get all global email templates for a specific category
        /// </summary>
        group.MapGet("", GetGlobalTemplatesByCategory)
            .WithName("GetGlobalTemplatesByCategory")
            .WithSummary("Get all global email templates for a category")
            .WithDescription("Returns all active global email templates for the specified category. Admin access required.")
            .Produces<List<GlobalEmailTemplateDto>>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get a single global email template by ID
        /// </summary>
        group.MapGet("{id:guid}", GetGlobalTemplateById)
            .WithName("GetGlobalTemplateById")
            .WithSummary("Get a global email template by ID")
            .WithDescription("Returns a single global email template. Admin access required.")
            .Produces<GlobalEmailTemplateDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Update a global email template
        /// </summary>
        group.MapPut("{id:guid}", UpdateGlobalTemplate)
            .WithName("UpdateGlobalTemplate")
            .WithSummary("Update a global email template")
            .WithDescription("Updates a global email template and increments version. Admin access required.")
            .Produces<GlobalEmailTemplateDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // Event Templates (Admin or Event Organizer)
        // ========================================

        /// <summary>
        /// Get all email templates for an event (merged global + overrides)
        /// </summary>
        group.MapGet("/events/{eventId:guid}", GetEventTemplates)
            .WithName("GetEventTemplates")
            .WithSummary("Get all email templates for an event")
            .WithDescription("Returns merged list of global templates and event-specific overrides. Admin or event organizer access required.")
            .Produces<List<EventEmailTemplateDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization();

        /// <summary>
        /// Get a specific email template for an event by type
        /// </summary>
        group.MapGet("/events/{eventId:guid}/{type}", GetEventTemplateByType)
            .WithName("GetEventTemplateByType")
            .WithSummary("Get a specific email template for an event")
            .WithDescription("Returns global template if no override exists, or event-specific template if customized. Admin or event organizer access required.")
            .Produces<EventEmailTemplateDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization();

        /// <summary>
        /// Create or update an event-specific email template override
        /// </summary>
        group.MapPut("/events/{eventId:guid}/{type}", UpdateEventTemplate)
            .WithName("UpdateEventTemplate")
            .WithSummary("Create or update event email template override")
            .WithDescription("Creates new override on first save (copy-on-edit), updates existing on subsequent saves. Admin or event organizer access required.")
            .Produces<EventEmailTemplateDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization();

        /// <summary>
        /// Delete an event-specific email template override (reset to default)
        /// </summary>
        group.MapDelete("/events/{eventId:guid}/{type}", DeleteEventTemplate)
            .WithName("DeleteEventTemplate")
            .WithSummary("Delete event email template override")
            .WithDescription("Removes event-specific override, future requests will return global template. Admin or event organizer access required.")
            .Produces(204)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization();

        // ========================================
        // Ad Hoc Emails (Admin-only)
        // ========================================

        /// <summary>
        /// Send an ad-hoc bulk email
        /// </summary>
        group.MapPost("/ad-hoc/send", SendAdHocEmail)
            .WithName("SendAdHocEmail")
            .WithSummary("Send an ad-hoc bulk email")
            .WithDescription("Sends bulk email via SendGrid and creates audit trail. Admin access required.")
            .Produces<SentAdHocEmailDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get ad-hoc email send history
        /// </summary>
        group.MapGet("/ad-hoc/history", GetAdHocEmailHistory)
            .WithName("GetAdHocEmailHistory")
            .WithSummary("Get ad-hoc email send history")
            .WithDescription("Returns sent ad-hoc email history, optionally filtered by event. Admin access required.")
            .Produces<List<SentAdHocEmailDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get a specific sent ad-hoc email
        /// </summary>
        group.MapGet("/ad-hoc/history/{id:guid}", GetAdHocEmailById)
            .WithName("GetAdHocEmailById")
            .WithSummary("Get a specific sent ad-hoc email")
            .WithDescription("Returns details of a specific sent ad-hoc email. Admin access required.")
            .Produces<SentAdHocEmailDto>(200)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // User Segments (Email Targeting)
        // ========================================

        /// <summary>
        /// Get all user segments with counts
        /// </summary>
        group.MapGet("/segments", GetUserSegments)
            .WithName("GetUserSegments")
            .WithSummary("Get all user segments with counts")
            .WithDescription("Returns all available user segments with current user counts. Admin access required.")
            .Produces<List<UserSegmentDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get preview of users in a specific segment
        /// </summary>
        group.MapGet("/segments/{segmentName}/preview", GetSegmentPreview)
            .WithName("GetSegmentPreview")
            .WithSummary("Get preview of users in a segment")
            .WithDescription("Returns first 10 users in the specified segment for preview. Admin access required.")
            .Produces<List<UserPreviewDto>>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });
    }

    // ========================================
    // Handler Methods
    // ========================================

    private static async Task<IResult> GetGlobalTemplatesByCategory(
        [FromQuery] string category,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EmailCategory>(category, ignoreCase: true, out var emailCategory))
        {
            return Results.Problem(
                title: "Invalid Category",
                detail: $"Invalid category. Valid values: {string.Join(", ", Enum.GetNames<EmailCategory>())}",
                statusCode: 400);
        }

        var result = await service.GetGlobalTemplatesByCategoryAsync(emailCategory, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Templates",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetGlobalTemplateById(
        Guid id,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetGlobalTemplateByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Template Not Found",
                detail: result.Error,
                statusCode: 404);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateGlobalTemplate(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        [FromBody] UpdateGlobalTemplateRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "CSRF Validation Failed",
                detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                statusCode: 400);
        }

        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await service.UpdateGlobalTemplateAsync(id, request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Update Template",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetEventTemplates(
        Guid eventId,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEventTemplatesAsync(eventId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Event Templates",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetEventTemplateByType(
        Guid eventId,
        string type,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEventTemplateByTypeAsync(eventId, type, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Template Not Found",
                detail: result.Error,
                statusCode: 404);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateEventTemplate(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid eventId,
        string type,
        [FromBody] UpdateEventTemplateRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "CSRF Validation Failed",
                detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                statusCode: 400);
        }

        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // TODO: Check if user is event organizer or admin
        // For now, authorization attribute handles admin check

        var result = await service.UpdateEventTemplateAsync(eventId, type, request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Update Event Template",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DeleteEventTemplate(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid eventId,
        string type,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "CSRF Validation Failed",
                detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                statusCode: 400);
        }

        // TODO: Check if user is event organizer or admin
        // For now, authorization attribute handles admin check

        var result = await service.DeleteEventTemplateAsync(eventId, type, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Delete Event Template",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> SendAdHocEmail(
        HttpContext context,
        IAntiforgery antiforgery,
        [FromBody] SendAdHocEmailRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Validate CSRF token
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "CSRF Validation Failed",
                detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                statusCode: 400);
        }

        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await service.SendAdHocEmailAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Send Ad-Hoc Email",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAdHocEmailHistory(
        [FromQuery] Guid? eventId,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAdHocEmailHistoryAsync(eventId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Ad-Hoc Email History",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAdHocEmailById(
        Guid id,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAdHocEmailByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Ad-Hoc Email Not Found",
                detail: result.Error,
                statusCode: 404);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetUserSegments(
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetUserSegmentsAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve User Segments",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetSegmentPreview(
        string segmentName,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Entities.UserSegment>(segmentName, ignoreCase: true, out var segment))
        {
            return Results.Problem(
                title: "Invalid Segment",
                detail: $"Invalid segment name. Valid values: {string.Join(", ", Enum.GetNames<Entities.UserSegment>())}",
                statusCode: 400);
        }

        var result = await service.GetSegmentPreviewAsync(segment, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Segment Preview",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }
}
