using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Features.Events.Endpoints;

/// <summary>
/// Enhanced event creation endpoint supporting sessions and ticket types
/// </summary>
public static class CreateEventEndpoint
{
    /// <summary>
    /// Registers the create event endpoint with session matrix support
    /// </summary>
    /// <param name="app">Route group builder</param>
    /// <returns>Route group builder for method chaining</returns>
    public static RouteGroupBuilder MapCreateEventEndpoint(this RouteGroupBuilder app)
    {
        app.MapPost("/events", CreateEventAsync)
            .WithName("CreateEventWithSessions")
            .WithTags("Events", "Sessions")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new event with sessions and ticket types";
                operation.Description = "Creates an event that can include multiple sessions (S1, S2, S3) and ticket types with flexible pricing.";
                return operation;
            })
            .RequireAuthorization("RequireOrganizer")
            .Produces<CreateEventWithSessionsResponse>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        return app;
    }

    /// <summary>
    /// Creates a new event with sessions and ticket types
    /// </summary>
    private static async Task<IResult> CreateEventAsync(
        [FromBody] CreateEventWithSessionsRequest request,
        WitchCityRopeDbContext dbContext,
        HttpContext httpContext)
    {
        try
        {
            var organizerId = httpContext.User.GetUserId();

            // Verify organizer exists and has permission
            var organizer = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == organizerId);

            if (organizer == null)
            {
                return Results.Problem("Organizer not found", statusCode: 404);
            }

            if (organizer.Role != UserRole.Organizer && organizer.Role != UserRole.Administrator)
            {
                return Results.Problem("User does not have permission to create events", statusCode: 403);
            }

            // Create pricing tiers from the basic price
            var pricingTiers = new List<Money> { Money.Create(request.Price) };

            // Create the event
            var eventEntity = new Event(
                title: request.Title,
                description: request.Description,
                startDate: request.StartDateTime.ToUniversalTime(),
                endDate: request.EndDateTime.ToUniversalTime(),
                capacity: request.MaxAttendees,
                eventType: request.Type,
                location: request.Location,
                primaryOrganizer: organizer,
                pricingTiers: pricingTiers
            );

            // Add sessions if provided
            foreach (var sessionRequest in request.Sessions)
            {
                var session = new EventSession(
                    eventId: eventEntity.Id,
                    sessionIdentifier: sessionRequest.SessionIdentifier,
                    name: sessionRequest.Name,
                    date: sessionRequest.Date.ToUniversalTime(),
                    startTime: sessionRequest.StartTime,
                    endTime: sessionRequest.EndTime,
                    capacity: sessionRequest.Capacity,
                    isRequired: sessionRequest.IsRequired
                );

                eventEntity.AddSession(session);
            }

            // Add ticket types if provided
            foreach (var ticketTypeRequest in request.TicketTypes)
            {
                var ticketType = new EventTicketType(
                    eventId: eventEntity.Id,
                    name: ticketTypeRequest.Name,
                    description: ticketTypeRequest.Description,
                    minPrice: ticketTypeRequest.MinPrice,
                    maxPrice: ticketTypeRequest.MaxPrice,
                    quantityAvailable: ticketTypeRequest.QuantityAvailable,
                    salesEndDate: ticketTypeRequest.SalesEndDate?.ToUniversalTime(),
                    isRsvpMode: ticketTypeRequest.IsRsvpMode
                );

                // Add included sessions
                foreach (var sessionId in ticketTypeRequest.IncludedSessions)
                {
                    ticketType.AddSession(sessionId);
                }

                eventEntity.AddTicketType(ticketType);
            }

            // Save to database
            dbContext.Events.Add(eventEntity);
            await dbContext.SaveChangesAsync();

            var response = new CreateEventWithSessionsResponse
            {
                EventId = eventEntity.Id,
                Title = eventEntity.Title,
                CreatedAt = eventEntity.CreatedAt,
                SessionsCreated = eventEntity.Sessions.Count,
                TicketTypesCreated = eventEntity.TicketTypes.Count,
                Message = "Event created successfully with session matrix support"
            };

            return Results.Created($"/api/v1/events/{eventEntity.Id}", response);
        }
        catch (WitchCityRope.Core.Exceptions.ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors });
        }
        catch (WitchCityRope.Core.DomainException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred while creating the event: {ex.Message}", statusCode: 500);
        }
    }
}

/// <summary>
/// Request model for creating an event with sessions and ticket types
/// </summary>
public class CreateEventWithSessionsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int MaxAttendees { get; set; }
    public decimal Price { get; set; }
    public string[] RequiredSkillLevels { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool RequiresVetting { get; set; }
    public string SafetyNotes { get; set; } = string.Empty;
    public string EquipmentProvided { get; set; } = string.Empty;
    public string EquipmentRequired { get; set; } = string.Empty;

    /// <summary>
    /// Sessions to create for this event (S1, S2, S3, etc.)
    /// </summary>
    public List<CreateEventSessionRequest> Sessions { get; set; } = new();

    /// <summary>
    /// Ticket types to create for this event
    /// </summary>
    public List<CreateEventTicketTypeRequest> TicketTypes { get; set; } = new();
}

/// <summary>
/// Response model for event creation with session information
/// </summary>
public class CreateEventWithSessionsResponse
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SessionsCreated { get; set; }
    public int TicketTypesCreated { get; set; }
    public string Message { get; set; } = string.Empty;
}