using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Features.Venues.Services;

namespace WitchCityRope.Api.Endpoints;

/// <summary>
/// Public venue endpoints for authenticated users.
/// These endpoints allow members with RSVP/tickets to view venue details.
/// </summary>
public static class VenueEndpoints
{
    /// <summary>
    /// Register public venue endpoints using minimal API pattern.
    /// Requires authentication but not admin role.
    /// </summary>
    public static void MapPublicVenueEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/venues/{id} - Get single venue (authenticated users only)
        app.MapGet("/api/venues/{id:int}", async (
            int id,
            IVenueService venueService,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Verify authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    return Results.Problem(
                        title: "Authentication Required",
                        detail: "You must be logged in to access venue details",
                        statusCode: 401);
                }

                try
                {
                    var venue = await venueService.GetPublicVenueAsync(id, cancellationToken);

                    if (venue == null)
                    {
                        return Results.Problem(
                            title: "Venue Not Found",
                            detail: $"Venue with ID {id} does not exist or is not available",
                            statusCode: 404);
                    }

                    return Results.Ok(venue);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Database Error",
                        detail: $"Failed to retrieve venue: {ex.Message}",
                        statusCode: 500);
                }
            })
            .WithName("GetPublicVenue")
            .WithSummary("Get single venue (authenticated users)")
            .WithDescription("Returns a single active venue by ID. Requires authentication. Notes field is not exposed to public.")
            .WithTags("Venues")
            .Produces<VenueDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500);

        // GET /api/venues - List all active venues (authenticated users only)
        app.MapGet("/api/venues", async (
            IVenueService venueService,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Verify authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    return Results.Problem(
                        title: "Authentication Required",
                        detail: "You must be logged in to access venues",
                        statusCode: 401);
                }

                try
                {
                    var venues = await venueService.GetPublicVenuesAsync(cancellationToken);

                    return Results.Ok(venues);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Database Error",
                        detail: $"Failed to retrieve venues: {ex.Message}",
                        statusCode: 500);
                }
            })
            .WithName("GetPublicVenues")
            .WithSummary("Get all active venues (authenticated users)")
            .WithDescription("Returns all active venues. Requires authentication. Notes field is not exposed to public.")
            .WithTags("Venues")
            .Produces<List<VenueDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(500);
    }
}
