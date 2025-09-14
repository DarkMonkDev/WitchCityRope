using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.DTOs;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Endpoint for retrieving real-time event availability information
/// </summary>
public static class GetEventAvailabilityEndpoint
{
    /// <summary>
    /// Registers the get event availability endpoint
    /// </summary>
    /// <param name="app">Route group builder</param>
    /// <returns>Route group builder for method chaining</returns>
    public static RouteGroupBuilder MapGetEventAvailabilityEndpoint(this RouteGroupBuilder app)
    {
        app.MapGet("/events/{id:guid}/availability", GetEventAvailabilityAsync)
            .WithName("GetEventAvailability")
            .WithTags("Events", "Sessions", "Availability")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get real-time event availability";
                operation.Description = "Retrieves real-time availability information for all sessions and ticket types.";
                return operation;
            })
            .Produces<EventAvailabilityDto>(200)
            .Produces(404);

        return app;
    }

    /// <summary>
    /// Gets real-time availability information for an event
    /// </summary>
    private static async Task<IResult> GetEventAvailabilityAsync(
        [FromRoute] Guid id,
        WitchCityRopeDbContext dbContext)
    {
        try
        {
            var eventEntity = await dbContext.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.TicketTypeSessions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return Results.NotFound(new { error = "Event not found" });
            }

            // Calculate session availability
            var sessionAvailability = eventEntity.Sessions
                .OrderBy(s => s.SessionIdentifier)
                .Select(s => new SessionAvailabilityDto
                {
                    SessionIdentifier = s.SessionIdentifier,
                    SessionName = s.Name,
                    Capacity = s.Capacity,
                    RegisteredCount = s.RegisteredCount,
                    AvailableSpots = s.GetAvailableSpots(),
                    HasAvailableCapacity = s.HasAvailableCapacity(),
                    AvailabilityStatus = GetSessionAvailabilityStatus(s.GetAvailableSpots(), s.Capacity)
                })
                .ToList();

            // Calculate ticket type availability
            var ticketTypeAvailability = eventEntity.TicketTypes
                .Where(tt => tt.IsActive)
                .OrderBy(tt => tt.Name)
                .Select(tt => 
                {
                    var availableSpots = eventEntity.CalculateTicketTypeAvailability(tt);
                    var includedSessions = tt.GetIncludedSessionIdentifiers().OrderBy(x => x).ToList();
                    var limitingSession = GetLimitingSession(eventEntity, tt);
                    
                    return new TicketTypeAvailabilityDto
                    {
                        TicketTypeId = tt.Id,
                        TicketTypeName = tt.Name,
                        IncludedSessions = includedSessions,
                        AvailableSpots = availableSpots,
                        HasAvailableCapacity = availableSpots > 0,
                        IsCurrentlyOnSale = tt.IsCurrentlyOnSale(),
                        LimitingSession = limitingSession,
                        AvailabilityStatus = GetTicketTypeAvailabilityStatus(tt, availableSpots)
                    };
                })
                .ToList();

            // Calculate overall availability
            var totalAvailableSpots = ticketTypeAvailability.Any() 
                ? ticketTypeAvailability.Where(tt => tt.IsCurrentlyOnSale).Sum(tt => tt.AvailableSpots)
                : eventEntity.GetAvailableSpots();

            var response = new EventAvailabilityDto
            {
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                HasSessions = eventEntity.HasSessions(),
                IsAvailable = totalAvailableSpots > 0 && eventEntity.IsPublished,
                TotalAvailableSpots = Math.Max(0, totalAvailableSpots),
                SessionAvailability = sessionAvailability,
                TicketTypeAvailability = ticketTypeAvailability,
                CalculatedAt = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred while calculating availability: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Determines the session availability status for display
    /// </summary>
    private static string GetSessionAvailabilityStatus(int availableSpots, int capacity)
    {
        if (availableSpots == 0)
            return "Full";
        
        if (availableSpots <= capacity * 0.1) // Less than 10% remaining
            return "Limited";
            
        return "Available";
    }

    /// <summary>
    /// Determines the ticket type availability status for display
    /// </summary>
    private static string GetTicketTypeAvailabilityStatus(Core.Entities.EventTicketType ticketType, int availableSpots)
    {
        if (!ticketType.IsCurrentlyOnSale())
            return "Off Sale";
            
        if (availableSpots == 0)
            return "Full";
        
        if (availableSpots <= 5) // Limited availability threshold
            return "Limited";
            
        return "Available";
    }

    /// <summary>
    /// Finds the session with the lowest availability that limits this ticket type
    /// </summary>
    private static string? GetLimitingSession(Core.Entities.Event eventEntity, Core.Entities.EventTicketType ticketType)
    {
        var includedSessions = ticketType.GetIncludedSessionIdentifiers();
        string? limitingSession = null;
        int minAvailable = int.MaxValue;

        foreach (var sessionId in includedSessions)
        {
            var session = eventEntity.GetSession(sessionId);
            if (session != null)
            {
                var available = session.GetAvailableSpots();
                if (available < minAvailable)
                {
                    minAvailable = available;
                    limitingSession = sessionId;
                }
            }
        }

        return limitingSession;
    }
}