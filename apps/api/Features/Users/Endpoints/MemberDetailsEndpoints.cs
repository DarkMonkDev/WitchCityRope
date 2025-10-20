using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Users.Models.MemberDetails;
using WitchCityRope.Api.Features.Users.Services;

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
            .RequireAuthorization(policy => policy.RequireRole("Administrator"))
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
            .Produces<List<UserNoteResponse>>(200)
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
        group.MapPut("/role", UpdateMemberRole)
            .WithName("UpdateMemberRole")
            .WithSummary("Update member role")
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
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
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
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
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
            return Results.BadRequest(new { error = "Page must be >= 1" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Results.BadRequest(new { error = "Page size must be between 1 and 100" });
        }

        var (success, response, error) = await service.GetEventHistoryAsync(userId, page, pageSize, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
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
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
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
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint 6: Create a new note
    /// POST /api/users/{userId}/notes
    /// </summary>
    private static async Task<IResult> CreateMemberNote(
        Guid userId,
        [FromBody] CreateUserNoteRequest request,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // Get current user ID (admin performing the action)
        var authorIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(authorIdClaim) || !Guid.TryParse(authorIdClaim, out var authorId))
        {
            return Results.Unauthorized();
        }

        var (success, response, error) = await service.CreateMemberNoteAsync(userId, request, authorId, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { error });
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("empty", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error });
            }
            return Results.Json(new { error }, statusCode: 500);
        }

        return Results.Created($"/api/users/{userId}/notes/{response!.Id}", new
        {
            success = true,
            data = response,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint 7: Update member status
    /// PUT /api/users/{userId}/status
    /// </summary>
    private static async Task<IResult> UpdateMemberStatus(
        Guid userId,
        [FromBody] UpdateMemberStatusRequest request,
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Unauthorized();
        }

        var (success, error) = await service.UpdateMemberStatusAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound(new { error })
                : Results.Json(new { error }, statusCode: 500);
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
        ClaimsPrincipal user,
        [FromServices] IMemberDetailsService service,
        CancellationToken cancellationToken)
    {
        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Unauthorized();
        }

        var (success, error) = await service.UpdateMemberRoleAsync(userId, request, performedById, cancellationToken);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { error });
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error });
            }
            return Results.Json(new { error }, statusCode: 500);
        }

        return Results.NoContent();
    }
}
