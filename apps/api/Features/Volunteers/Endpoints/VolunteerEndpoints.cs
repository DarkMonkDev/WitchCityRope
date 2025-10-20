using System.Security.Claims;
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
            VolunteerService volunteerService,
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
                return Results.Ok(new ApiResponse<List<VolunteerPositionDto>>
                {
                    Success = true,
                    Data = positions,
                    Message = positions.Count == 0
                        ? "No volunteer positions available for this event"
                        : "Volunteer positions retrieved successfully"
                });
            }

            return Results.Json(new ApiResponse<List<VolunteerPositionDto>>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to retrieve volunteer positions",
                Message = "Unable to retrieve volunteer positions"
            }, statusCode: error == "Event not found" ? 404 : 500);
        })
        .WithName("GetEventVolunteerPositions")
        .WithSummary("Get volunteer positions for an event")
        .WithDescription("Returns volunteer positions for a specific event. Shows public-facing positions only for non-authenticated users. Authenticated users see their signup status.")
        .WithTags("Volunteers")
        .Produces<ApiResponse<List<VolunteerPositionDto>>>(200)
        .Produces(404)
        .Produces(500);

        // Sign up for a volunteer position
        app.MapPost("/api/volunteer-positions/{id}/signup", async (
            string id,
            VolunteerSignupRequest request,
            VolunteerService volunteerService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Require authentication
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ApiResponse<VolunteerSignupDto>
                {
                    Success = false,
                    Data = null,
                    Error = "Authentication required",
                    Message = "You must be logged in to sign up for volunteer positions"
                }, statusCode: 401);
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Json(new ApiResponse<VolunteerSignupDto>
                {
                    Success = false,
                    Data = null,
                    Error = "Invalid user",
                    Message = "User ID not found in authentication token"
                }, statusCode: 401);
            }

            var (success, signup, error) = await volunteerService.SignupForPositionAsync(
                id,
                userId,
                request,
                cancellationToken);

            if (success && signup != null)
            {
                return Results.Ok(new ApiResponse<VolunteerSignupDto>
                {
                    Success = true,
                    Data = signup,
                    Message = "Successfully signed up for volunteer position. You have been automatically RSVPed to the event."
                });
            }

            var statusCode = error switch
            {
                "Volunteer position not found" => 404,
                "You have already signed up for this volunteer position" => 409,
                "This volunteer position is already fully staffed" => 409,
                "This volunteer position is not open for public signups" => 403,
                _ => 500
            };

            return Results.Json(new ApiResponse<VolunteerSignupDto>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to sign up for volunteer position",
                Message = "Unable to complete volunteer signup"
            }, statusCode: statusCode);
        })
        .WithName("SignupForVolunteerPosition")
        .WithSummary("Sign up for a volunteer position")
        .WithDescription("Sign up for a volunteer position. Requires authentication. Automatically RSVPs user to the event if not already registered.")
        .WithTags("Volunteers")
        .Produces<ApiResponse<VolunteerSignupDto>>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);
    }
}
