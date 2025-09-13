using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using EventDto = WitchCityRope.Api.Features.Events.Models.EventDto;
using WitchCityRope.Api.Features.Events.Models;

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
        // Get all published events
        app.MapGet("/api/events", async (
            EventService eventService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await eventService.GetPublishedEventsAsync(cancellationToken);

                if (success)
                {
                    // Return fallback data if database is empty to maintain existing behavior
                    if (response.Count == 0)
                    {
                        var fallbackEvents = GetFallbackEvents();
                        return Results.Ok(new ApiResponse<List<EventDto>>
                        {
                            Success = true,
                            Data = fallbackEvents.ToList(),
                            Message = "Events retrieved from fallback data"
                        });
                    }
                    
                    return Results.Ok(new ApiResponse<List<EventDto>>
                    {
                        Success = true,
                        Data = response,
                        Message = "Events retrieved successfully"
                    });
                }

                // If database fails, return fallback events as the original controller did
                try
                {
                    var fallbackEvents = GetFallbackEvents();
                    return Results.Ok(new ApiResponse<List<EventDto>>
                    {
                        Success = true,
                        Data = fallbackEvents.ToList(),
                        Message = "Events retrieved from fallback data due to database error"
                    });
                }
                catch
                {
                    return Results.Json(new ApiResponse<List<EventDto>>
                    {
                        Success = false,
                        Data = null,
                        Error = error,
                        Message = "Failed to retrieve events"
                    }, statusCode: 500);
                }
            })
            .WithName("GetEvents")
            .WithSummary("Get all published events")
            .WithDescription("Returns all published future events from the database with fallback data")
            .WithTags("Events")
            .Produces<ApiResponse<List<EventDto>>>(200)
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

                // If database fails or event not found, check fallback events
                try
                {
                    var fallbackEvents = GetFallbackEvents();
                    var fallbackEvent = fallbackEvents.FirstOrDefault(e => e.Id == id);
                    
                    if (fallbackEvent != null)
                    {
                        return Results.Ok(new ApiResponse<EventDto>
                        {
                            Success = true,
                            Data = fallbackEvent,
                            Message = "Event retrieved from fallback data"
                        });
                    }
                }
                catch
                {
                    // If fallback also fails, continue to error response
                }

                return Results.Json(new ApiResponse<EventDto>
                {
                    Success = false,
                    Data = null,
                    Error = error,
                    Message = response == null ? "Event not found" : "Failed to retrieve event"
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

    /// <summary>
    /// Hardcoded fallback events for reliability
    /// Maintains compatibility with existing EventsController behavior
    /// </summary>
    private static EventDto[] GetFallbackEvents()
    {
        return new[]
        {
            // SOLD OUT Class event (100% capacity) - Green progress bar
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440000",
                Title = "Rope Basics Workshop (Fallback)",
                Description = "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners who want to explore shibari and kinbaku basics.",
                StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 8, 25, 17, 0, 0, DateTimeKind.Utc),
                Location = "Salem Community Center",
                EventType = "Class",
                Capacity = 20,
                CurrentAttendees = 20,    // SOLD OUT
                CurrentRSVPs = 0,         // Class events don't have RSVPs
                CurrentTickets = 20       // All attendees paid tickets
            },
            
            // Nearly sold out Class event (87% capacity) - Green progress bar
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440001",
                Title = "Advanced Suspension Techniques (Fallback)",
                Description = "Advanced workshop covering suspension safety, rigging points, and dynamic movements. Prerequisites: completion of intermediate rope workshops.",
                StartDate = new DateTime(2025, 8, 30, 19, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 8, 30, 22, 0, 0, DateTimeKind.Utc),
                Location = "Studio Space Downtown",
                EventType = "Class",
                Capacity = 15,
                CurrentAttendees = 13,    // 87% capacity - nearly sold out
                CurrentRSVPs = 0,         // Class events don't have RSVPs
                CurrentTickets = 13       // All attendees paid tickets
            },
            
            // Moderately filled Social event (60% capacity) - Yellow progress bar
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440002",
                Title = "Community Social & Practice (Fallback)",
                Description = "Open practice session for all skill levels. Bring your rope and practice with others in a supportive community environment.",
                StartDate = new DateTime(2025, 9, 5, 18, 30, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 9, 5, 21, 30, 0, DateTimeKind.Utc),
                Location = "Salem Arts Collective",
                EventType = "Social",
                Capacity = 30,
                CurrentAttendees = 18,    // 60% capacity - moderately filled
                CurrentRSVPs = 13,        // Most people use free RSVP for social events (72%)
                CurrentTickets = 5        // Some people buy tickets to support (28%)
            },
            
            // Low attendance Social event (33% capacity) - Red progress bar
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440003",
                Title = "New Members Welcome Meetup (Fallback)",
                Description = "Casual introduction to the rope community for new and curious members. Q&A session with experienced practitioners.",
                StartDate = new DateTime(2025, 9, 12, 19, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 9, 12, 21, 0, 0, DateTimeKind.Utc),
                Location = "Salem Coffee Co.",
                EventType = "Social",
                Capacity = 24,
                CurrentAttendees = 8,     // 33% capacity - needs more signups
                CurrentRSVPs = 6,         // Most attendees use free RSVP (75%)
                CurrentTickets = 2        // Few paid tickets (25%)
            }
        };
    }
}