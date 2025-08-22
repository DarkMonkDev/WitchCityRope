using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;

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
                        return Results.Ok(fallbackEvents);
                    }
                    
                    return Results.Ok(response);
                }

                // If database fails, return fallback events as the original controller did
                try
                {
                    var fallbackEvents = GetFallbackEvents();
                    return Results.Ok(fallbackEvents);
                }
                catch
                {
                    return Results.Problem(
                        title: "Get Events Failed",
                        detail: error,
                        statusCode: 500);
                }
            })
            .WithName("GetEvents")
            .WithSummary("Get all published events")
            .WithDescription("Returns all published future events from the database with fallback data")
            .WithTags("Events")
            .Produces<List<EventDto>>(200)
            .Produces(500);

        // Get single event by ID
        app.MapGet("/api/events/{id}", async (
            string id,
            EventService eventService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await eventService.GetEventAsync(id, cancellationToken);

                return success 
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Event Failed",
                        detail: error,
                        statusCode: response == null ? 404 : 500);
            })
            .WithName("GetEvent")
            .WithSummary("Get single event by ID")
            .WithDescription("Returns a specific event by its unique identifier")
            .WithTags("Events")
            .Produces<EventDto>(200)
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
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440000",
                Title = "Rope Basics Workshop (Fallback)",
                Description = "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners who want to explore shibari and kinbaku basics.",
                StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
                Location = "Salem Community Center"
            },
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440001",
                Title = "Advanced Suspension Techniques (Fallback)",
                Description = "Advanced workshop covering suspension safety, rigging points, and dynamic movements. Prerequisites: completion of intermediate rope workshops.",
                StartDate = new DateTime(2025, 8, 30, 19, 0, 0, DateTimeKind.Utc),
                Location = "Studio Space Downtown"
            },
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440002",
                Title = "Community Social & Practice (Fallback)",
                Description = "Open practice session for all skill levels. Bring your rope and practice with others in a supportive community environment.",
                StartDate = new DateTime(2025, 9, 5, 18, 30, 0, DateTimeKind.Utc),
                Location = "Salem Arts Collective"
            }
        };
    }
}