using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WitchCityRope.Api.Features.AuthorizedContacts.Models;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;

namespace WitchCityRope.Api.Features.AuthorizedContacts.Endpoints;

/// <summary>
/// Minimal API endpoints for managing authorized contact relationships.
/// All endpoints require authentication (any authenticated user, no specific role).
/// State-changing endpoints (POST, DELETE) require CSRF validation.
/// </summary>
public static class AuthorizedContactEndpoints
{
    /// <summary>
    /// Register all authorized contact endpoints using minimal API pattern.
    /// Follows the same patterns as ParticipationEndpoints and UserEndpoints.
    /// </summary>
    public static void MapAuthorizedContactEndpoints(this IEndpointRouteBuilder app)
    {
        // ============================================================================
        // EP-01: GET /api/authorized-contacts
        // Lists all active authorized contact relationships for the current user
        // ============================================================================
        app.MapGet("/api/authorized-contacts",
            [Authorize] async (
                IAuthorizedContactService contactService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await contactService.GetContactsAsync(userId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: "Failed to retrieve authorized contacts",
                        detail: result.Error,
                        statusCode: 500);
            })
            .RequireAuthorization()
            .WithName("GetAuthorizedContacts")
            .WithSummary("List all authorized contact relationships for the current user")
            .WithDescription("Returns contacts in two lists: Delegates (people I authorized) and Principals (people who authorized me)")
            .WithTags("AuthorizedContacts")
            .Produces<AuthorizedContactsListDto>(200)
            .Produces(401)
            .Produces(500);

        // ============================================================================
        // EP-02: POST /api/authorized-contacts
        // Creates a new authorized contact relationship (caller = Principal)
        // ============================================================================
        app.MapPost("/api/authorized-contacts",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                AddAuthorizedContactRequest request,
                IAuthorizedContactService contactService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first for state-changing operations
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

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await contactService.AddContactAsync(userId, request.DelegateUserId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created($"/api/authorized-contacts/{result.Value!.Id}", result.Value);
                }

                // Map service error messages to appropriate HTTP status codes
                var statusCode = result.Error switch
                {
                    "Cannot authorize yourself as a contact" => 400,  // BR-003
                    "User not found" => 404,                          // BR-006
                    "Authorization already exists" => 409,             // Duplicate
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("AddAuthorizedContact")
            .WithSummary("Add a new authorized contact (caller becomes Principal)")
            .WithDescription("Creates a delegation authorization. The caller (Principal) authorizes the specified user (Delegate) to act on their behalf for ticket purchases and RSVPs.")
            .WithTags("AuthorizedContacts")
            .Produces<AuthorizedContactDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(409)
            .Produces(500);

        // ============================================================================
        // EP-03: DELETE /api/authorized-contacts/{contactId:guid}
        // Revokes an authorized contact relationship (soft delete)
        // ============================================================================
        app.MapDelete("/api/authorized-contacts/{contactId:guid}",
            [Authorize] async (
                HttpContext context,
                IAntiforgery antiforgery,
                Guid contactId,
                IAuthorizedContactService contactService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                // CSRF validation - MUST be first for state-changing operations
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

                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await contactService.RevokeContactAsync(userId, contactId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }

                // Map service error messages to appropriate HTTP status codes
                var statusCode = result.Error switch
                {
                    "Authorization not found" => 404,
                    "Not authorized to revoke" => 403,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("RevokeAuthorizedContact")
            .WithSummary("Revoke an authorized contact relationship")
            .WithDescription("Soft-deletes the authorization. Only the Principal who created it can revoke. Does not affect existing tickets/RSVPs (BR-005).")
            .WithTags("AuthorizedContacts")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // ============================================================================
        // EP-04: GET /api/authorized-contacts/search?q={sceneName}
        // Searches users by scene name for the add-contact typeahead
        // ============================================================================
        app.MapGet("/api/authorized-contacts/search",
            [Authorize] async (
                string? q,
                IAuthorizedContactService contactService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                {
                    return Results.Problem(
                        title: "Query too short",
                        detail: "Search query must be at least 2 characters",
                        statusCode: 400);
                }

                var result = await contactService.SearchUsersAsync(userId, q, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: result.Error,
                        detail: result.Details,
                        statusCode: result.Error == "Query too short" ? 400 : 500);
            })
            .RequireAuthorization()
            .WithName("SearchUsersForAuthorizedContacts")
            .WithSummary("Search users by scene name for adding authorized contacts")
            .WithDescription("Returns up to 10 users matching the query. Excludes self and already-authorized users. Returns only scene name per AD-009 privacy.")
            .WithTags("AuthorizedContacts")
            .Produces<List<ContactSearchResultDto>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(500);

        // ============================================================================
        // EP-05: GET /api/authorized-contacts/principals?eventId={guid}
        // Gets principals who authorized the current user (for checkout/RSVP dropdowns)
        // ============================================================================
        app.MapGet("/api/authorized-contacts/principals",
            [Authorize] async (
                Guid? eventId,
                IAuthorizedContactService contactService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Results.Problem(
                        title: "Unauthorized",
                        detail: "User authentication failed - missing or invalid user identifier",
                        statusCode: 401);
                }

                var result = await contactService.GetPrincipalsAsync(userId, eventId, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var statusCode = result.Error switch
                {
                    "Event not found" => 404,
                    _ => 500
                };

                return Results.Problem(
                    title: result.Error,
                    detail: result.Details,
                    statusCode: statusCode);
            })
            .RequireAuthorization()
            .WithName("GetPrincipalContacts")
            .WithSummary("Get principals who authorized the current user as their delegate")
            .WithDescription("Used in checkout/RSVP dropdowns. When eventId is provided, filters by vetting requirements (BR-035) and excludes principals who already have attendance (BR-012).")
            .WithTags("AuthorizedContacts")
            .Produces<List<PrincipalContactDto>>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);
    }
}
