using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Users.Models.MemberDetails;
using WitchCityRope.Api.Features.Users.Services;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.Users.Endpoints;

/// <summary>
/// Endpoints for admin member details and management
/// All endpoints require Admin role authorization
/// </summary>
public static class MemberDetailsEndpoints
{
    public static void MapMemberDetailsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/{userId:guid}")
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithTags("Member Details")
            .WithOpenApi();

        // Endpoint 1: Get comprehensive member details
        group.MapGet("/details", GetMemberDetails)
            .WithName("GetMemberDetails")
            .WithSummary("Get comprehensive member details including participation summary")
            .Produces<MemberDetailsResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 2: Get vetting details and questionnaire
        group.MapGet("/vetting-details", GetVettingDetails)
            .WithName("GetMemberVettingDetails")
            .WithSummary("Get vetting application details and questionnaire responses")
            .Produces<VettingDetailsResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 3: Get paginated event history
        group.MapGet("/event-history", GetEventHistory)
            .WithName("GetMemberEventHistory")
            .WithSummary("Get paginated event registration and attendance history")
            .Produces<EventHistoryResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 4: Get safety incidents
        group.MapGet("/incidents", GetMemberIncidents)
            .WithName("GetMemberIncidents")
            .WithSummary("Get safety incidents involving this member")
            .Produces<MemberIncidentsResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 5: Get all notes (unified notes system)
        group.MapGet("/notes", GetMemberNotes)
            .WithName("GetMemberNotes")
            .WithSummary("Get all notes for this member (vetting, general, administrative, status changes)")
            .Produces<List<MemberNoteHistoryResponse>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 6: Create a new note
        group.MapPost("/notes", CreateMemberNote)
            .WithName("CreateMemberNote")
            .WithSummary("Create a new note for this member")
            .Produces<UserNoteResponse>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 7: Update member status (active/inactive)
        group.MapPut("/status", UpdateMemberStatus)
            .WithName("UpdateMemberStatus")
            .WithSummary("Update member status (active/inactive) - auto-creates status change note")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 8: Update member role
        // CRITICAL SECURITY NOTE: CSRF protection is AUTOMATICALLY ENABLED via app.UseAntiforgery() middleware
        // All POST/PUT/DELETE/PATCH endpoints have CSRF validation by default - prevents role elevation attacks
        group.MapPut("/role", UpdateMemberRole)
            // CSRF PROTECTION: Enabled automatically by app.UseAntiforgery() middleware (prevents role elevation)
            .WithName("UpdateMemberRole")
            .WithSummary("Update member role")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 9: Get profile change history
        group.MapGet("/profile-change-history", GetProfileChangeHistory)
            .WithName("GetProfileChangeHistory")
            .WithSummary("Get profile change history for this member")
            .Produces<List<ProfileChangeHistoryDto>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 10: Update member contact information (admin)
        group.MapPut("/contact-info", UpdateMemberContactInfo)
            .WithName("UpdateMemberContactInfo")
            .WithSummary("Update member contact information (admin) - auto-creates audit note")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // Endpoint 11: Admin reset member password
        // CRITICAL SECURITY NOTE: CSRF protection is AUTOMATICALLY ENABLED via app.UseAntiforgery() middleware
        // Only administrators can reset passwords - prevents unauthorized password changes
        group.MapPost("/reset-password", AdminResetPassword)
            .WithName("AdminResetPassword")
            .WithSummary("Admin-initiated password reset - sets a new password for a member without requiring their current password")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }

    /// <summary>
    /// Endpoint 1: Get comprehensive member details
    /// GET /api/users/{userId}/details
    /// </summary>
    private static async Task<IResult> GetMemberDetails(
        Guid userId,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await service.GetMemberDetailsAsync(userId, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 2: Get vetting details and questionnaire
    /// GET /api/users/{userId}/vetting-details
    /// </summary>
    private static async Task<IResult> GetVettingDetails(
        Guid userId,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await service.GetVettingDetailsAsync(userId, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 3: Get paginated event history
    /// GET /api/users/{userId}/event-history?page=1&pageSize=20
    /// </summary>
    private static async Task<IResult> GetEventHistory(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IMemberDetailsService service = null!,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Validation Failed",
                detail: "Page must be >= 1",
                statusCode: 400);
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Validation Failed",
                detail: "Page size must be between 1 and 100",
                statusCode: 400);
        }

        var (success, response, error) = await service.GetEventHistoryAsync(userId, page, pageSize, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 4: Get safety incidents
    /// GET /api/users/{userId}/incidents
    /// </summary>
    private static async Task<IResult> GetMemberIncidents(
        Guid userId,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await service.GetMemberIncidentsAsync(userId, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 5: Get all notes (unified notes system)
    /// GET /api/users/{userId}/notes
    /// </summary>
    private static async Task<IResult> GetMemberNotes(
        Guid userId,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await service.GetMemberNotesAsync(userId, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 6: Create a new note
    /// POST /api/users/{userId}/notes
    /// </summary>
    private static async Task<IResult> CreateMemberNote(
        Guid userId,
        [FromBody] CreateUserNoteRequest request,
        HttpContext context,
        IAntiforgery antiforgery,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Validate anti-forgery token FIRST
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        // Get current user ID (admin performing the action)
        var authorIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(authorIdClaim) || !Guid.TryParse(authorIdClaim, out var authorId))
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, response, error) = await service.CreateMemberNoteAsync(userId, request, authorId, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("empty", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Bad Request",
                    detail: error,
                    statusCode: 400);
            }
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Server Error",
                detail: error,
                statusCode: 500);
        }

        return Results.Created($"/api/users/{userId}/notes/{response!.Id}", response);
    }

    /// <summary>
    /// Endpoint 7: Update member status
    /// PUT /api/users/{userId}/status
    /// </summary>
    private static async Task<IResult> UpdateMemberStatus(
        Guid userId,
        [FromBody] UpdateMemberStatusRequest request,
        HttpContext context,
        IAntiforgery antiforgery,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Validate anti-forgery token FIRST
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await service.UpdateMemberStatusAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Endpoint 8: Update member role
    /// PUT /api/users/{userId}/role
    /// </summary>
    private static async Task<IResult> UpdateMemberRole(
        Guid userId,
        [FromBody] UpdateMemberRoleRequest request,
        HttpContext context,
        IAntiforgery antiforgery,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Validate anti-forgery token FIRST
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await service.UpdateMemberRoleAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Bad Request",
                    detail: error,
                    statusCode: 400);
            }
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Server Error",
                detail: error,
                statusCode: 500);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Endpoint 9: Get profile change history
    /// GET /api/users/{userId}/profile-change-history
    /// </summary>
    private static async Task<IResult> GetProfileChangeHistory(
        Guid userId,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        var (success, response, error) = await service.GetProfileChangeHistoryAsync(userId, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Endpoint 10: Update member contact information
    /// PUT /api/users/{userId}/contact-info
    /// Admin-only endpoint that updates contact fields and auto-creates an audit note
    /// </summary>
    private static async Task<IResult> UpdateMemberContactInfo(
        Guid userId,
        [FromBody] AdminUpdateContactInfoDto request,
        HttpContext context,
        IAntiforgery antiforgery,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Validate anti-forgery token FIRST
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await service.UpdateMemberContactInfoAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Bad Request",
                    detail: error,
                    statusCode: 400);
            }
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Server Error",
                detail: error,
                statusCode: 500);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Endpoint 11: Admin reset member password
    /// POST /api/users/{userId}/reset-password
    /// Allows administrators to set a new password for a member.
    /// Requires CSRF token and admin authorization.
    /// </summary>
    private static async Task<IResult> AdminResetPassword(
        Guid userId,
        [FromBody] AdminResetPasswordRequest request,
        HttpContext context,
        IAntiforgery antiforgery,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Validate anti-forgery token FIRST
        var csrfResult = await antiforgery.ValidateAsync(context);
        if (!csrfResult.IsSuccess)
            return csrfResult.ToProblem("CSRF Validation Failed");

        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await service.AdminResetPasswordAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            // Password validation errors from Identity
            return Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
                title: "Bad Request",
                detail: error,
                statusCode: 400);
        }

        return Results.NoContent();
    }
}
