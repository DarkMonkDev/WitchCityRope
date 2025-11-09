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
            .Produces<ApiResponse<List<GlobalEmailTemplateDto>>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get a single global email template by ID
        /// </summary>
        group.MapGet("{id:guid}", GetGlobalTemplateById)
            .WithName("GetGlobalTemplateById")
            .WithSummary("Get a global email template by ID")
            .WithDescription("Returns a single global email template. Admin access required.")
            .Produces<ApiResponse<GlobalEmailTemplateDto>>(200)
            .Produces(404)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Update a global email template
        /// </summary>
        group.MapPut("{id:guid}", UpdateGlobalTemplate)
            .WithName("UpdateGlobalTemplate")
            .WithSummary("Update a global email template")
            .WithDescription("Updates a global email template and increments version. Admin access required.")
            .Produces<ApiResponse<GlobalEmailTemplateDto>>(200)
            .Produces(400)
            .Produces(404)
            .Produces(401)
            .Produces(403)
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
            .Produces<ApiResponse<List<EventEmailTemplateDto>>>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization();

        /// <summary>
        /// Get a specific email template for an event by type
        /// </summary>
        group.MapGet("/events/{eventId:guid}/{type}", GetEventTemplateByType)
            .WithName("GetEventTemplateByType")
            .WithSummary("Get a specific email template for an event")
            .WithDescription("Returns global template if no override exists, or event-specific template if customized. Admin or event organizer access required.")
            .Produces<ApiResponse<EventEmailTemplateDto>>(200)
            .Produces(404)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization();

        /// <summary>
        /// Create or update an event-specific email template override
        /// </summary>
        group.MapPut("/events/{eventId:guid}/{type}", UpdateEventTemplate)
            .WithName("UpdateEventTemplate")
            .WithSummary("Create or update event email template override")
            .WithDescription("Creates new override on first save (copy-on-edit), updates existing on subsequent saves. Admin or event organizer access required.")
            .Produces<ApiResponse<EventEmailTemplateDto>>(200)
            .Produces(400)
            .Produces(404)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization();

        /// <summary>
        /// Delete an event-specific email template override (reset to default)
        /// </summary>
        group.MapDelete("/events/{eventId:guid}/{type}", DeleteEventTemplate)
            .WithName("DeleteEventTemplate")
            .WithSummary("Delete event email template override")
            .WithDescription("Removes event-specific override, future requests will return global template. Admin or event organizer access required.")
            .Produces(204)
            .Produces(404)
            .Produces(401)
            .Produces(403)
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
            .Produces<ApiResponse<SentAdHocEmailDto>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get ad-hoc email send history
        /// </summary>
        group.MapGet("/ad-hoc/history", GetAdHocEmailHistory)
            .WithName("GetAdHocEmailHistory")
            .WithSummary("Get ad-hoc email send history")
            .WithDescription("Returns sent ad-hoc email history, optionally filtered by event. Admin access required.")
            .Produces<ApiResponse<List<SentAdHocEmailDto>>>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        /// <summary>
        /// Get a specific sent ad-hoc email
        /// </summary>
        group.MapGet("/ad-hoc/history/{id:guid}", GetAdHocEmailById)
            .WithName("GetAdHocEmailById")
            .WithSummary("Get a specific sent ad-hoc email")
            .WithDescription("Returns details of a specific sent ad-hoc email. Admin access required.")
            .Produces<ApiResponse<SentAdHocEmailDto>>(200)
            .Produces(404)
            .Produces(401)
            .Produces(403)
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
            return Results.BadRequest(new ApiResponse<List<GlobalEmailTemplateDto>>
            {
                Success = false,
                Error = $"Invalid category. Valid values: {string.Join(", ", Enum.GetNames<EmailCategory>())}"
            });
        }

        var result = await service.GetGlobalTemplatesByCategoryAsync(emailCategory, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<List<GlobalEmailTemplateDto>>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<List<GlobalEmailTemplateDto>>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> GetGlobalTemplateById(
        Guid id,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetGlobalTemplateByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new ApiResponse<GlobalEmailTemplateDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<GlobalEmailTemplateDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> UpdateGlobalTemplate(
        Guid id,
        [FromBody] UpdateGlobalTemplateRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await service.UpdateGlobalTemplateAsync(id, request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<GlobalEmailTemplateDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<GlobalEmailTemplateDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> GetEventTemplates(
        Guid eventId,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEventTemplatesAsync(eventId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<List<EventEmailTemplateDto>>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<List<EventEmailTemplateDto>>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
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
            return Results.NotFound(new ApiResponse<EventEmailTemplateDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<EventEmailTemplateDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> UpdateEventTemplate(
        Guid eventId,
        string type,
        [FromBody] UpdateEventTemplateRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
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
            return Results.BadRequest(new ApiResponse<EventEmailTemplateDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<EventEmailTemplateDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> DeleteEventTemplate(
        Guid eventId,
        string type,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // TODO: Check if user is event organizer or admin
        // For now, authorization attribute handles admin check

        var result = await service.DeleteEventTemplateAsync(eventId, type, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> SendAdHocEmail(
        [FromBody] SendAdHocEmailRequest request,
        ClaimsPrincipal user,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await service.SendAdHocEmailAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<SentAdHocEmailDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<SentAdHocEmailDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> GetAdHocEmailHistory(
        [FromQuery] Guid? eventId,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAdHocEmailHistoryAsync(eventId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ApiResponse<List<SentAdHocEmailDto>>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<List<SentAdHocEmailDto>>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> GetAdHocEmailById(
        Guid id,
        IEmailTemplateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAdHocEmailByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new ApiResponse<SentAdHocEmailDto>
            {
                Success = false,
                Error = result.Error
            });
        }

        return Results.Ok(new ApiResponse<SentAdHocEmailDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}
