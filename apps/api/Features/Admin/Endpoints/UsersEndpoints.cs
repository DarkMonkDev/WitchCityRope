using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Users.Models;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.Admin.Endpoints;

/// <summary>
/// API endpoints for user management
/// </summary>
public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        // GET /api/users/by-role/{role} - Get users by role (for dropdowns)
        group.MapGet("/by-role/{role}", GetUsersByRole)
            .WithName("GetUsersByRole")
            .WithSummary("Get users by role")
            .WithDescription("Get list of users filtered by role (e.g., 'Teacher', 'Admin')")
            .RequireAuthorization(); // Only authenticated users can access
    }

    private static async Task<IResult> GetUsersByRole(
        string role,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        // No try/catch needed: any unhandled exception here is caught by GlobalExceptionHandler
        // which logs it and returns a uniform RFC 7807 ProblemDetails 500. This avoids leaking
        // ex.Message on the wire (see docs/standards-processes/backend/error-handling-standard.md).

        // Get all users with the specified role
        var usersInRole = await userManager.GetUsersInRoleAsync(role);

        // Convert to simple DTOs for dropdowns
        var userOptions = usersInRole.Select(user => new UserOptionDto
        {
            Id = user.Id.ToString(),
            Name = user.SceneName ?? user.Email ?? "Unknown",
            Email = user.Email ?? ""
        }).ToList();

        return Results.Ok(userOptions);
    }
}

// UserOptionDto is defined in WitchCityRope.Api.Features.Users.Models.UserOptionDto
// Do NOT re-declare here — single source of truth avoids OpenAPI schema collisions.