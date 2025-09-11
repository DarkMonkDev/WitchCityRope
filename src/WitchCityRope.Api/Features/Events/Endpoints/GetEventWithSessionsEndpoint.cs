using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.DTOs;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Endpoint for retrieving event with complete session and ticket type information
/// </summary>
public static class GetEventWithSessionsEndpoint
{
    /// <summary>
    /// Registers the get event with sessions endpoint
    /// </summary>
    /// <param name="app">Route group builder</param>
    /// <returns>Route group builder for method chaining</returns>
    public static RouteGroupBuilder MapGetEventWithSessionsEndpoint(this RouteGroupBuilder app)
    {
        app.MapGet("/events/{id:guid}/sessions", GetEventWithSessionsAsync)
            .WithName("GetEventWithSessions")
            .WithTags("Events", "Sessions")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get event with sessions and ticket types";
                operation.Description = "Retrieves an event with complete session matrix information including availability.";
                return operation;
            })
            .Produces<EventWithSessionsDto>(200)
            .Produces(404);

        return app;
    }

    /// <summary>
    /// Gets an event with complete session and ticket type information
    /// </summary>
    private static async Task<IResult> GetEventWithSessionsAsync(
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

            // Build session DTOs
            var sessionDtos = eventEntity.Sessions
                .OrderBy(s => s.SessionIdentifier)
                .Select(s => new EventSessionDto
                {
                    Id = s.Id,
                    SessionIdentifier = s.SessionIdentifier,
                    Name = s.Name,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Capacity = s.Capacity,
                    RegisteredCount = s.RegisteredCount,
                    AvailableSpots = s.GetAvailableSpots(),
                    IsRequired = s.IsRequired,
                    HasAvailableCapacity = s.HasAvailableCapacity()
                })
                .ToList();

            // Build ticket type DTOs
            var ticketTypeDtos = eventEntity.TicketTypes
                .Where(tt => tt.IsActive)
                .OrderBy(tt => tt.Name)
                .Select(tt => new EventTicketTypeDto
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    Description = tt.Description,
                    Type = DetermineTicketTypeCategory(tt.Name), // Basic categorization
                    MinPrice = tt.MinPrice,
                    MaxPrice = tt.MaxPrice,
                    IncludedSessions = tt.GetIncludedSessionIdentifiers().OrderBy(x => x).ToList(),
                    QuantityAvailable = tt.QuantityAvailable,
                    TicketsSold = 0, // TODO: Calculate from registrations
                    SalesEndDate = tt.SalesEndDate,
                    IsRsvpMode = tt.IsRsvpMode,
                    IsActive = tt.IsActive,
                    IsCurrentlyOnSale = tt.IsCurrentlyOnSale(),
                    AvailableSpots = eventEntity.CalculateTicketTypeAvailability(tt),
                    HasAvailableCapacity = eventEntity.CalculateTicketTypeAvailability(tt) > 0
                })
                .ToList();

            // Build pricing summary
            var pricingSummary = new PricingSummaryDto();
            if (ticketTypeDtos.Any())
            {
                pricingSummary.MinPrice = ticketTypeDtos.Min(tt => tt.MinPrice);
                pricingSummary.MaxPrice = ticketTypeDtos.Max(tt => tt.MaxPrice);
                pricingSummary.HasFreeOptions = ticketTypeDtos.Any(tt => tt.IsRsvpMode || tt.MinPrice == 0);
            }

            var response = new EventWithSessionsDto
            {
                // Base event properties
                Id = eventEntity.Id,
                Name = eventEntity.Title,
                Description = eventEntity.Description,
                Location = eventEntity.Location,
                StartDateTime = eventEntity.StartDate,
                EndDateTime = eventEntity.EndDate,
                MaxAttendees = eventEntity.Capacity,
                CurrentAttendees = eventEntity.GetConfirmedRegistrationCount(),
                Price = eventEntity.PricingTiers.FirstOrDefault()?.Amount ?? 0,
                Status = eventEntity.IsPublished ? "Published" : "Draft",
                RequiresVetting = false, // TODO: Get from event properties
                Tags = new List<string>(), // TODO: Get from event properties
                RequiredSkillLevels = new List<string>(), // TODO: Get from event properties

                // Session matrix properties
                Sessions = sessionDtos,
                TicketTypes = ticketTypeDtos,
                HasSessions = eventEntity.HasSessions(),
                HasTicketTypes = eventEntity.HasTicketTypes(),
                TotalSessionCapacity = eventEntity.GetTotalSessionCapacity(),
                PricingSummary = pricingSummary
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred while retrieving the event: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Basic categorization of ticket types based on name patterns
    /// </summary>
    private static string DetermineTicketTypeCategory(string ticketTypeName)
    {
        var name = ticketTypeName.ToLowerInvariant();
        
        if (name.Contains("couple") || name.Contains("pair") || name.Contains("two"))
            return "Couples";
        
        if (name.Contains("single") || name.Contains("individual"))
            return "Single";
            
        if (name.Contains("group") || name.Contains("team"))
            return "Group";
            
        return "Single"; // Default category
    }
}