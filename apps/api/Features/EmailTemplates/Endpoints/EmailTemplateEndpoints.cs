using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Shared.Services;

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

        /// <summary>
        /// Update trigger configuration for a global template (Events category only)
        /// </summary>
        group.MapPut("{id:guid}/trigger-config", UpdateTriggerConfig)
            .WithName("UpdateTriggerConfig")
            .WithSummary("Update trigger configuration for a global template")
            .WithDescription("Updates trigger configuration (type, enabled, timing, recipient group). Events category only. Admin access required.")
            .Produces<GlobalEmailTemplateDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Toggle sending enabled/disabled for any global template (all categories).
        /// Unified mechanism — replaces the need for separate enable/disable per category.
        /// </summary>
        group.MapPatch("{id:guid}/sending-enabled", ToggleSendingEnabled)
            .WithName("ToggleSendingEnabled")
            .WithSummary("Toggle sending enabled for a global template")
            .WithDescription("Enables or disables email sending for a template. Works for all categories. When disabled, emails are silently suppressed. Admin access required.")
            .Produces<GlobalEmailTemplateDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get all time-based templates for scheduler
        /// </summary>
        group.MapGet("time-based", GetTimeBasedTemplates)
            .WithName("GetTimeBasedTemplates")
            .WithSummary("Get all time-based templates")
            .WithDescription("Returns all enabled time-based templates for EmailSchedulerJob. Admin access required.")
            .Produces<List<GlobalEmailTemplateDto>>(200)
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

        // ========================================
        // Ad Hoc Templates (Save/Delete/Reuse)
        // ========================================

        /// <summary>
        /// Get all saved ad-hoc templates
        /// </summary>
        group.MapGet("/ad-hoc/templates", GetAdHocTemplates)
            .WithName("GetAdHocTemplates")
            .WithSummary("Get all saved ad-hoc templates")
            .WithDescription("Returns all saved ad-hoc email templates for reuse. Admin access required.")
            .Produces<List<AdHocEmailTemplateDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Save an ad-hoc email as a reusable template
        /// </summary>
        group.MapPost("/ad-hoc/templates", SaveAsTemplate)
            .WithName("SaveAsTemplate")
            .WithSummary("Save an ad-hoc email as a template")
            .WithDescription("Saves an ad-hoc email as a reusable template. Admin access required.")
            .Produces<AdHocEmailTemplateDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Delete a saved ad-hoc template
        /// </summary>
        group.MapDelete("/ad-hoc/templates/{id:guid}", DeleteAdHocTemplate)
            .WithName("DeleteAdHocTemplate")
            .WithSummary("Delete a saved ad-hoc template")
            .WithDescription("Deletes a saved ad-hoc email template. Admin access required.")
            .Produces(204)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // Email Trigger Logs (Admin Visibility)
        // ========================================

        /// <summary>
        /// Get email trigger log entries with optional filtering
        /// </summary>
        group.MapGet("/trigger-logs", GetTriggerLogs)
            .WithName("GetTriggerLogs")
            .WithSummary("Get email trigger log entries")
            .WithDescription("Returns email trigger log entries with optional filtering by event, status, template type, and date range. Admin access required.")
            .Produces<List<EmailTriggerLogDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // Scheduled Ad Hoc Emails
        // ========================================

        /// <summary>
        /// Schedule an ad-hoc email for future delivery
        /// </summary>
        group.MapPost("/ad-hoc/schedule", ScheduleAdHocEmail)
            .WithName("ScheduleAdHocEmail")
            .WithSummary("Schedule an ad-hoc email for future delivery")
            .WithDescription("Schedules an ad-hoc email for future delivery via EmailSchedulerJob. Admin access required.")
            .Produces<SentAdHocEmailDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // Variable Registry (Admin-only)
        // ========================================

        /// <summary>
        /// Get available variables for a template type from the code registry
        /// </summary>
        group.MapGet("/variables/{category}/{templateType}", GetTemplateVariables)
            .WithName("GetTemplateVariables")
            .WithSummary("Get available variables for a template type")
            .WithDescription("Returns the list of available template variables from the code registry. Used by the admin UI to show which variables can be used in templates.")
            .Produces<string[]>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get all unique variables grouped by category from the code registry.
        /// Used by the test data editor to show input fields for every possible variable.
        /// </summary>
        group.MapGet("/variables", GetAllTemplateVariables)
            .WithName("GetAllTemplateVariables")
            .WithSummary("Get all template variables grouped by category")
            .WithDescription("Returns all unique variables across all templates, grouped by category. Used by the test data editor.")
            .Produces<Dictionary<string, string[]>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        // ========================================
        // Email Template Testing (Admin-only)
        // ========================================

        group.MapGet("/test-data", GetEmailTestData)
            .WithName("GetEmailTestData")
            .WithSummary("Get all email template test data variable values")
            .WithDescription("Returns saved default values for all email template variables, used for test sends")
            .Produces<Dictionary<string, string>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        group.MapPut("/test-data", SaveEmailTestData)
            .WithName("SaveEmailTestData")
            .WithSummary("Save email template test data variable values")
            .WithDescription("Creates or updates default variable values used for test email sends")
            .Produces<object>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        group.MapPost("/{id:guid}/send-test", SendTestEmail)
            .WithName("SendTestEmail")
            .WithSummary("Send a test email for a specific template")
            .WithDescription("Sends a test email with test data variable substitution to a specified email address")
            .Produces<object>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(500)
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
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
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
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
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
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
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

    private static async Task<IResult> UpdateTriggerConfig(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        [FromBody] UpdateTriggerConfigRequest request,
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

        var result = await service.UpdateTriggerConfigAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Update Trigger Configuration",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ToggleSendingEnabled(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        [FromBody] ToggleSendingEnabledRequest request,
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

        var result = await service.ToggleSendingEnabledAsync(id, request.Enabled, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Toggle Sending Enabled",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetTimeBasedTemplates(
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetTimeBasedTemplatesAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Time-Based Templates",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAdHocTemplates(
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAdHocTemplatesAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Ad-Hoc Templates",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> SaveAsTemplate(
        HttpContext context,
        IAntiforgery antiforgery,
        [FromBody] SaveAsTemplateRequest request,
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
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var result = await service.SaveAsTemplateAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Save Template",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DeleteAdHocTemplate(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
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

        var result = await service.DeleteAdHocTemplateAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Delete Template",
                detail: result.Error,
                statusCode: 404);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetTriggerLogs(
        [FromQuery] Guid? eventId,
        [FromQuery] string? status,
        [FromQuery] string? templateType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? limit,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetTriggerLogsAsync(
            eventId, status, templateType, fromDate, toDate, limit ?? 50, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Retrieve Trigger Logs",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ScheduleAdHocEmail(
        HttpContext context,
        IAntiforgery antiforgery,
        [FromBody] ScheduleAdHocEmailRequest request,
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
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var result = await service.ScheduleAdHocEmailAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to Schedule Email",
                detail: result.Error,
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    // ========================================
    // Variable Registry Handlers
    // ========================================

    private static Task<IResult> GetTemplateVariables(string category, string templateType)
    {
        if (!Enum.TryParse<EmailCategory>(category, ignoreCase: true, out var emailCategory))
        {
            return Task.FromResult(Results.Problem(
                title: "Invalid Category",
                detail: $"Invalid category. Valid values: {string.Join(", ", Enum.GetNames<EmailCategory>())}",
                statusCode: 400));
        }

        var variables = EmailTemplateVariableRegistry.GetVariables(emailCategory, templateType);
        return Task.FromResult(Results.Ok(variables));
    }

    private static Task<IResult> GetAllTemplateVariables()
    {
        var grouped = EmailTemplateVariableRegistry.GetAllVariablesGroupedByCategory();
        return Task.FromResult(Results.Ok(grouped));
    }

    // ========================================
    // Email Template Testing Handlers
    // ========================================

    private static async Task<IResult> GetEmailTestData(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var allSettings = await settingsService.GetAllSettingsAsync(cancellationToken);

        // Filter to only EmailTestData: prefixed settings and strip the prefix for the response
        var testData = allSettings
            .Where(kvp => kvp.Key.StartsWith("EmailTestData:"))
            .ToDictionary(
                kvp => kvp.Key.Replace("EmailTestData:", ""),
                kvp => kvp.Value);

        return Results.Ok(testData);
    }

    private static async Task<IResult> SaveEmailTestData(
        HttpContext context,
        IAntiforgery antiforgery,
        [FromBody] Dictionary<string, string> testData,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        // CSRF validation
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

        // Prefix all keys with "EmailTestData:" for storage
        var prefixedData = testData.ToDictionary(
            kvp => $"EmailTestData:{kvp.Key}",
            kvp => kvp.Value);

        var (success, error) = await settingsService.UpsertMultipleSettingsAsync(
            prefixedData, cancellationToken);

        if (!success)
        {
            return Results.Problem(
                title: "Save Failed",
                detail: error,
                statusCode: 500);
        }

        return Results.Ok(new { message = "Test data saved successfully" });
    }

    private static async Task<IResult> SendTestEmail(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid id,
        [FromBody] SendTestEmailRequest request,
        ApplicationDbContext dbContext,
        ISettingsService settingsService,
        IEmailService emailService,
        ILogger<EmailService> logger,
        CancellationToken cancellationToken)
    {
        // CSRF validation
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

        // Validate email address
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { error = "Email address is required" });
        }

        // Fetch the template from the database
        var template = await dbContext.GlobalEmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null)
        {
            return Results.NotFound(new { error = "Template not found" });
        }

        // Load saved test data defaults from Settings
        var allSettings = await settingsService.GetAllSettingsAsync(cancellationToken);
        var testData = allSettings
            .Where(kvp => kvp.Key.StartsWith("EmailTestData:"))
            .ToDictionary(
                kvp => kvp.Key.Replace("EmailTestData:", ""),
                kvp => kvp.Value);

        // Merge with overrides (overrides take precedence)
        if (request.VariableOverrides != null)
        {
            foreach (var kvp in request.VariableOverrides)
            {
                testData[kvp.Key] = kvp.Value;
            }

            // Auto-save overrides back to defaults
            var prefixedOverrides = request.VariableOverrides.ToDictionary(
                kvp => $"EmailTestData:{kvp.Key}",
                kvp => kvp.Value);

            await settingsService.UpsertMultipleSettingsAsync(prefixedOverrides, cancellationToken);
        }

        // Substitute variables in template content
        var subject = EmailService.SubstituteVariables(template.Subject, testData);
        var htmlBody = EmailService.SubstituteVariables(template.HtmlBody, testData);
        var plainTextBody = EmailService.SubstituteVariables(template.PlainTextBody, testData);

        // Send the email using the raw send method (not templated, since we already resolved the template)
        var result = await emailService.SendEmailAsync(
            request.Email,
            subject,
            htmlBody,
            plainTextBody,
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Test email sent: TemplateId={TemplateId}, TemplateType={TemplateType}, To={Email}",
                template.Id, template.TemplateType, request.Email);

            return Results.Ok(new
            {
                message = "Test email sent successfully",
                templateType = template.TemplateType,
                sentTo = request.Email
            });
        }

        return Results.Problem(
            title: "Send Failed",
            detail: result.Error,
            statusCode: 500);
    }
}
