using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Events minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class EventEndpoints
{
    /// <summary>
    /// Register events endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all events with optional admin access
        app.MapGet("/api/events", async (
            EventService eventService,
            HttpContext context,
            bool? includeUnpublished,
            CancellationToken cancellationToken) =>
            {
                // Check if user is requesting unpublished events
                var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);

                // If requesting unpublished events, verify admin role
                if (shouldIncludeUnpublished)
                {
                    var user = context.User;
                    if (user.Identity?.IsAuthenticated != true)
                    {
                        return Results.Json(new ApiResponse<List<EventDto>>
                        {
                            Success = false,
                            Data = null,
                            Error = "Authentication required",
                            Message = "Authentication required to access unpublished events"
                        }, statusCode: 401);
                    }

                    var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (userRole != "Administrator")
                    {
                        return Results.Json(new ApiResponse<List<EventDto>>
                        {
                            Success = false,
                            Data = null,
                            Error = "Insufficient permissions",
                            Message = "Administrator role required to access unpublished events"
                        }, statusCode: 403);
                    }
                }

                var (success, response, error) = await eventService.GetEventsAsync(shouldIncludeUnpublished, cancellationToken);

                if (success)
                {
                    return Results.Ok(new ApiResponse<List<EventDto>>
                    {
                        Success = true,
                        Data = response,
                        Message = response.Count == 0 ? "No events found" : "Events retrieved successfully"
                    });
                }

                // Return proper error - NO FALLBACK DATA
                return Results.Json(new ApiResponse<List<EventDto>>
                {
                    Success = false,
                    Data = null,
                    Error = error ?? "Failed to retrieve events from database",
                    Message = "Unable to retrieve events. Please check if the API database connection is working."
                }, statusCode: 500);
            })
            .WithName("GetEvents")
            .WithSummary("Get all events")
            .WithDescription("Returns events from the database. Use ?includeUnpublished=true for admin access to draft events. Requires Administrator role for unpublished events.")
            .WithTags("Events")
            .Produces<ApiResponse<List<EventDto>>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);

        // Get single event by ID
        app.MapGet("/api/events/{id}", async (
            string id,
            EventService eventService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await eventService.GetEventAsync(id, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(new ApiResponse<EventDto>
                    {
                        Success = true,
                        Data = response,
                        Message = "Event retrieved successfully"
                    });
                }

                // Return proper error - NO FALLBACK DATA
                return Results.Json(new ApiResponse<EventDto>
                {
                    Success = false,
                    Data = null,
                    Error = error ?? "Event not found or database error",
                    Message = response == null ? "Event not found" : "Failed to retrieve event from database"
                }, statusCode: response == null ? 404 : 500);
            })
            .WithName("GetEvent")
            .WithSummary("Get single event by ID")
            .WithDescription("Returns a specific event by its unique identifier")
            .WithTags("Events")
            .Produces<ApiResponse<EventDto>>(200)
            .Produces(404)
            .Produces(500);

        // Update existing event by ID
        app.MapPut("/api/events/{id}", async (
            string id,
            UpdateEventRequest request,
            EventService eventService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await eventService.UpdateEventAsync(id, request, cancellationToken);

                if (success && response != null)
                {
                    return Results.Ok(new ApiResponse<EventDto>
                    {
                        Success = true,
                        Data = response,
                        Message = "Event updated successfully"
                    });
                }

                // Determine appropriate HTTP status code based on error message
                var statusCode = error switch
                {
                    string msg when msg.Contains("not found") => 404,
                    string msg when msg.Contains("Invalid event ID") => 400,
                    string msg when msg.Contains("past events") => 400,
                    string msg when msg.Contains("capacity") => 400,
                    string msg when msg.Contains("date") => 400,
                    string msg when msg.Contains("null") => 400,
                    _ => 500
                };

                return Results.Json(new ApiResponse<EventDto>
                {
                    Success = false,
                    Data = null,
                    Error = error,
                    Message = "Failed to update event"
                }, statusCode: statusCode);
            })
            .RequireAuthorization() // Requires JWT authentication
            .WithName("UpdateEvent")
            .WithSummary("Update an existing event")
            .WithDescription("Updates an event with the provided data. Supports partial updates (only non-null fields will be updated). " +
                "Business rules: Cannot update past events, cannot reduce capacity below current attendance.")
            .WithTags("Events")
            .Produces<ApiResponse<EventDto>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500);
    }
}