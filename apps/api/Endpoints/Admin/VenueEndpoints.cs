using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Endpoints.Admin;

/// <summary>
/// Admin-only venue management endpoints.
/// All endpoints require Administrator role for venue CRUD operations.
/// </summary>
public static class VenueEndpoints
{
    /// <summary>
    /// Register admin venue endpoints using minimal API pattern.
    /// All endpoints require Administrator role authorization.
    /// </summary>
    public static void MapVenueEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/admin/venues - List all venues (including inactive)
        app.MapGet("/api/admin/venues", async (
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

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<List<VenueDto>>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to manage venues"
                    }, statusCode: 403);
                }

                try
                {
                    var venues = await dbContext.Venues
                        .OrderBy(v => v.Name)
                        .Select(v => new VenueDto
                        {
                            Id = v.Id,
                            Name = v.Name,
                            Directions = v.Directions,
                            Notes = v.Notes,
                            IsActive = v.IsActive,
                            CreatedAt = v.CreatedAt,
                            UpdatedAt = v.UpdatedAt
                        })
                        .ToListAsync(cancellationToken);

                    return Results.Ok(new ApiResponse<List<VenueDto>>
                    {
                        Success = true,
                        Data = venues,
                        Message = $"Retrieved {venues.Count} venues"
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
            .WithName("GetAllVenues")
            .WithSummary("Get all venues (admin only)")
            .WithDescription("Returns all venues including inactive ones. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces<ApiResponse<List<VenueDto>>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);

        // GET /api/admin/venues/active - List active venues only
        app.MapGet("/api/admin/venues/active", async (
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

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<List<VenueDto>>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to manage venues"
                    }, statusCode: 403);
                }

                try
                {
                    var venues = await dbContext.Venues
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.Name)
                        .Select(v => new VenueDto
                        {
                            Id = v.Id,
                            Name = v.Name,
                            Directions = v.Directions,
                            Notes = v.Notes,
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
            .WithName("GetActiveVenues")
            .WithSummary("Get active venues only (admin only)")
            .WithDescription("Returns only active venues. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces<ApiResponse<List<VenueDto>>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);

        // GET /api/admin/venues/{id} - Get single venue
        app.MapGet("/api/admin/venues/{id:int}", async (
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
                        Message = "You must be logged in to access venues"
                    }, statusCode: 401);
                }

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to manage venues"
                    }, statusCode: 403);
                }

                try
                {
                    var venue = await dbContext.Venues
                        .Where(v => v.Id == id)
                        .Select(v => new VenueDto
                        {
                            Id = v.Id,
                            Name = v.Name,
                            Directions = v.Directions,
                            Notes = v.Notes,
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
                            Message = $"Venue with ID {id} does not exist"
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
            .WithName("GetVenue")
            .WithSummary("Get single venue (admin only)")
            .WithDescription("Returns a single venue by ID. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces<ApiResponse<VenueDto>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // POST /api/admin/venues - Create new venue
        app.MapPost("/api/admin/venues", async (
            CreateVenueRequest request,
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
                        Message = "You must be logged in to create venues"
                    }, statusCode: 401);
                }

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to create venues"
                    }, statusCode: 403);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Venue name is required"
                    }, statusCode: 400);
                }

                if (request.Name.Length > 100)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Venue name must not exceed 100 characters"
                    }, statusCode: 400);
                }

                if (request.Directions?.Length > 500)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Directions must not exceed 500 characters"
                    }, statusCode: 400);
                }

                if (request.Notes?.Length > 1000)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Notes must not exceed 1000 characters"
                    }, statusCode: 400);
                }

                try
                {
                    // Check for duplicate name (case-insensitive)
                    var existingVenue = await dbContext.Venues
                        .AnyAsync(v => v.Name.ToLower() == request.Name.ToLower(), cancellationToken);

                    if (existingVenue)
                    {
                        return Results.Json(new ApiResponse<VenueDto>
                        {
                            Success = false,
                            Data = null,
                            Error = "Duplicate venue name",
                            Message = $"A venue with the name '{request.Name}' already exists"
                        }, statusCode: 400);
                    }

                    // Create new venue
                    var venue = new Venue
                    {
                        Name = request.Name.Trim(),
                        Directions = string.IsNullOrWhiteSpace(request.Directions) ? null : request.Directions.Trim(),
                        Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    dbContext.Venues.Add(venue);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var venueDto = new VenueDto
                    {
                        Id = venue.Id,
                        Name = venue.Name,
                        Directions = venue.Directions,
                        Notes = venue.Notes,
                        IsActive = venue.IsActive,
                        CreatedAt = venue.CreatedAt,
                        UpdatedAt = venue.UpdatedAt
                    };

                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = true,
                        Data = venueDto,
                        Message = "Venue created successfully"
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Database error",
                        Message = $"Failed to create venue: {ex.Message}"
                    }, statusCode: 500);
                }
            })
            .WithName("CreateVenue")
            .WithSummary("Create new venue (admin only)")
            .WithDescription("Creates a new venue. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces<ApiResponse<VenueDto>>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(500);

        // PUT /api/admin/venues/{id} - Update venue
        app.MapPut("/api/admin/venues/{id:int}", async (
            int id,
            UpdateVenueRequest request,
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
                        Message = "You must be logged in to update venues"
                    }, statusCode: 401);
                }

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to update venues"
                    }, statusCode: 403);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Venue name is required"
                    }, statusCode: 400);
                }

                if (request.Name.Length > 100)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Venue name must not exceed 100 characters"
                    }, statusCode: 400);
                }

                if (request.Directions?.Length > 500)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Directions must not exceed 500 characters"
                    }, statusCode: 400);
                }

                if (request.Notes?.Length > 1000)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Validation failed",
                        Message = "Notes must not exceed 1000 characters"
                    }, statusCode: 400);
                }

                try
                {
                    var venue = await dbContext.Venues.FindAsync(new object[] { id }, cancellationToken);

                    if (venue == null)
                    {
                        return Results.Json(new ApiResponse<VenueDto>
                        {
                            Success = false,
                            Data = null,
                            Error = "Venue not found",
                            Message = $"Venue with ID {id} does not exist"
                        }, statusCode: 404);
                    }

                    // Check for duplicate name (case-insensitive, excluding current venue)
                    var duplicateName = await dbContext.Venues
                        .AnyAsync(v => v.Id != id && v.Name.ToLower() == request.Name.ToLower(), cancellationToken);

                    if (duplicateName)
                    {
                        return Results.Json(new ApiResponse<VenueDto>
                        {
                            Success = false,
                            Data = null,
                            Error = "Duplicate venue name",
                            Message = $"Another venue with the name '{request.Name}' already exists"
                        }, statusCode: 400);
                    }

                    // Update venue properties
                    venue.Name = request.Name.Trim();
                    venue.Directions = string.IsNullOrWhiteSpace(request.Directions) ? null : request.Directions.Trim();
                    venue.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                    venue.IsActive = request.IsActive;
                    venue.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync(cancellationToken);

                    var venueDto = new VenueDto
                    {
                        Id = venue.Id,
                        Name = venue.Name,
                        Directions = venue.Directions,
                        Notes = venue.Notes,
                        IsActive = venue.IsActive,
                        CreatedAt = venue.CreatedAt,
                        UpdatedAt = venue.UpdatedAt
                    };

                    return Results.Ok(new ApiResponse<VenueDto>
                    {
                        Success = true,
                        Data = venueDto,
                        Message = "Venue updated successfully"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new ApiResponse<VenueDto>
                    {
                        Success = false,
                        Data = null,
                        Error = "Database error",
                        Message = $"Failed to update venue: {ex.Message}"
                    }, statusCode: 500);
                }
            })
            .WithName("UpdateVenue")
            .WithSummary("Update venue (admin only)")
            .WithDescription("Updates an existing venue. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces<ApiResponse<VenueDto>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);

        // DELETE /api/admin/venues/{id} - Soft delete venue
        app.MapDelete("/api/admin/venues/{id:int}", async (
            int id,
            ApplicationDbContext dbContext,
            HttpContext context,
            CancellationToken cancellationToken) =>
            {
                // Verify authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Error = "Authentication required",
                        Message = "You must be logged in to delete venues"
                    }, statusCode: 401);
                }

                // Verify admin role
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Administrator")
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Error = "Insufficient permissions",
                        Message = "Administrator role required to delete venues"
                    }, statusCode: 403);
                }

                try
                {
                    var venue = await dbContext.Venues.FindAsync(new object[] { id }, cancellationToken);

                    if (venue == null)
                    {
                        return Results.Json(new ApiResponse<object>
                        {
                            Success = false,
                            Data = null,
                            Error = "Venue not found",
                            Message = $"Venue with ID {id} does not exist"
                        }, statusCode: 404);
                    }

                    // Soft delete (set IsActive = false)
                    venue.IsActive = false;
                    venue.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync(cancellationToken);

                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Json(new ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Error = "Database error",
                        Message = $"Failed to delete venue: {ex.Message}"
                    }, statusCode: 500);
                }
            })
            .WithName("DeleteVenue")
            .WithSummary("Soft delete venue (admin only)")
            .WithDescription("Soft deletes a venue by setting IsActive to false. Preserves venue data for historical events. Requires Administrator role.")
            .WithTags("Admin", "Venues")
            .Produces(204)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }
}
