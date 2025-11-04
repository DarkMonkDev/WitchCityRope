using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Models;

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
            ApplicationDbContext dbContext,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Verify authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Authentication required",
                        Message = "You must be logged in to access venue details"
                    }, statusCode: 401);
                }

                try
                {
                    // Only return active venues to public
                    var venue = await dbContext.Venues
                        .Where(v => v.Id == id && v.IsActive)
                        .Select(v => new VenueDto
                        {
                            Id = v.Id,
                            Name = v.Name,
                            Directions = v.Directions,
                            Notes = null, // Don't expose internal notes to public
                            IsActive = v.IsActive,
                            CreatedAt = v.CreatedAt,
                            UpdatedAt = v.UpdatedAt
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (venue == null)
                    {
                        return Results.Json(new ApiResponse<VenueDto>
                        {
                            Success = false,
                            Data = null,
                            Error = "Venue not found",
                            Message = $"Venue with ID {id} does not exist or is not available"
                        }, statusCode: 404);
                    }

                    return Results.Ok(new ApiResponse<VenueDto>
                    {
                        Success = true,
                        Data = venue,
                        Message = "Venue retrieved successfully"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Database error",
                        Message = $"Failed to retrieve venue: {ex.Message}"
                    }, statusCode: 500);
                }
            })
            .WithName("GetPublicVenue")
            .WithSummary("Get single venue (authenticated users)")
            .WithDescription("Returns a single active venue by ID. Requires authentication. Notes field is not exposed to public.")
            .WithTags("Venues")
            .Produces<ApiResponse<VenueDto>>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500);

        // GET /api/venues - List all active venues (authenticated users only)
        app.MapGet("/api/venues", async (
            ApplicationDbContext dbContext,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Verify authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    return Results.Json(new ApiResponse<List<VenueDto>>
                    {
                        Success = false,
                        Data = null,
                        Error = "Authentication required",
                        Message = "You must be logged in to access venues"
                    }, statusCode: 401);
                }

                try
                {
                    // Only return active venues to public
                    var venues = await dbContext.Venues
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.Name)
                        .Select(v => new VenueDto
                        {
                            Id = v.Id,
                            Name = v.Name,
                            Directions = v.Directions,
                            Notes = null, // Don't expose internal notes to public
                            IsActive = v.IsActive,
                            CreatedAt = v.CreatedAt,
                            UpdatedAt = v.UpdatedAt
                        })
                        .ToListAsync(cancellationToken);

                    return Results.Ok(new ApiResponse<List<VenueDto>>
                    {
                        Success = true,
                        Data = venues,
                        Message = $"Retrieved {venues.Count} active venues"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new ApiResponse<List<VenueDto>>
                    {
                        Success = false,
                        Data = null,
                        Error = "Database error",
                        Message = $"Failed to retrieve venues: {ex.Message}"
                    }, statusCode: 500);
                }
            })
            .WithName("GetPublicVenues")
            .WithSummary("Get all active venues (authenticated users)")
            .WithDescription("Returns all active venues. Requires authentication. Notes field is not exposed to public.")
            .WithTags("Venues")
            .Produces<ApiResponse<List<VenueDto>>>(200)
            .Produces(401)
            .Produces(500);
    }
}
