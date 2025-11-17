using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Volunteers.Endpoints;

/// <summary>
/// Volunteer positions and signups minimal API endpoints
/// </summary>
public static class VolunteerEndpoints
{
    /// <summary>
    /// Register volunteer endpoints using minimal API pattern
    /// </summary>
    public static void MapVolunteerEndpoints(this IEndpointRouteBuilder app)
    {
        // Get volunteer positions for an event
        app.MapGet("/api/events/{id}/volunteer-positions", async (
            string id,
            [FromServices] IVolunteerService volunteerService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Get user ID if authenticated
            string? userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var (success, positions, error) = await volunteerService.GetEventVolunteerPositionsAsync(
                id,
                userId,
                cancellationToken);

            if (success && positions != null)
            {
                return Results.Ok(positions);
            }

            return Results.Problem(
                title: "Failed to Retrieve Volunteer Positions",
                detail: error ?? "Failed to retrieve volunteer positions",
                statusCode: error == "Event not found" ? 404 : 500);
        })
        .WithName("GetEventVolunteerPositions")
        .WithSummary("Get volunteer positions for an event")
        .WithDescription("Returns volunteer positions for a specific event. Shows public-facing positions only for non-authenticated users. Authenticated users see their signup status.")
        .WithTags("Volunteers")
        .Produces<List<VolunteerPositionDto>>(200)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Sign up for a volunteer position
        app.MapPost("/api/volunteer-positions/{id}/signup", async (
            string id,
            VolunteerSignupRequest request,
            [FromServices] IVolunteerService volunteerService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require authentication
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be logged in to sign up for volunteer positions",
                    statusCode: 401);
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Problem(
                    title: "Invalid User",
                    detail: "User ID not found in authentication token",
                    statusCode: 401);
            }

            var (success, signup, error) = await volunteerService.SignupForPositionAsync(
                id,
                userId,
                request,
                cancellationToken);

            if (success && signup != null)
            {
                return Results.Ok(signup);
            }

            var statusCode = error switch
            {
                "Volunteer position not found" => 404,
                "You have already signed up for this volunteer position" => 409,
                "This volunteer position is already fully staffed" => 409,
                "This volunteer position is not open for public signups" => 403,
                "You must accept the Event Waiver to volunteer" => 400,
                _ => 500
            };

            return Results.Problem(
                title: "Failed to Sign Up",
                detail: error ?? "Failed to sign up for volunteer position",
                statusCode: statusCode);
        })
        .WithName("SignupForVolunteerPosition")
        .WithSummary("Sign up for a volunteer position")
        .WithDescription("Sign up for a volunteer position. Requires authentication. Event Waiver acceptance is required. Automatically RSVPs user to social events if not already registered.")
        .WithTags("Volunteers")
        .Produces<VolunteerSignupDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(500);

        // Get user's volunteer shifts
        app.MapGet("/api/user/volunteer-shifts", async (
            [FromServices] IVolunteerService volunteerService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require authentication
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "You must be logged in to view your volunteer shifts",
                    statusCode: 401);
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Problem(
                    title: "Invalid User",
                    detail: "User ID not found in authentication token",
                    statusCode: 401);
            }

            var (success, shifts, error) = await volunteerService.GetUserVolunteerShiftsAsync(
                userId,
                cancellationToken);

            if (success && shifts != null)
            {
                return Results.Ok(shifts);
            }

            return Results.Problem(
                title: "Failed to Retrieve Volunteer Shifts",
                detail: error ?? "Failed to retrieve volunteer shifts",
                statusCode: 500);
        })
        .WithName("GetUserVolunteerShifts")
        .WithSummary("Get user's upcoming volunteer shifts")
        .WithDescription("Returns list of upcoming events where the user has signed up to volunteer. Includes event details, position title, and shift times.")
        .WithTags("Volunteers")
        .Produces<List<UserVolunteerShiftDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(500);
    }
}
