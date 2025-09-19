using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;

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
        try
        {
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
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"Failed to get users by role: {ex.Message}",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }
}

/// <summary>
/// Simple DTO for user dropdown options
/// </summary>
public class UserOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}