using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Models;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Services;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for managing event sessions and capacity calculations.
    /// Implements the Event Session Matrix concept where sessions are atomic capacity units.
    /// </summary>
    public class EventSessionService : IEventSessionService
    {
        private readonly WitchCityRopeDbContext _dbContext;
        private readonly ILogger<EventSessionService> _logger;

        public EventSessionService(
            WitchCityRopeDbContext dbContext,
            ILogger<EventSessionService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<EventDto>> CreateEventWithSessionsAsync(
            CreateEventRequest request, 
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Creating event with sessions: {Title}", request.Title);

                // Get organizer
                var organizer = await _dbContext.Users.FindAsync(request.OrganizerId);
                if (organizer == null)
                {
                    return Result<EventDto>.Failure("Organizer not found");
                }

                // Create basic event - this is a minimal implementation for TDD
                var startDate = DateTime.UtcNow.AddDays(7); // Default start date
                var endDate = DateTime.UtcNow.AddDays(8); // Default end date
                
                // Use a basic pricing tier
                var basicPricing = new List<Core.ValueObjects.Money> 
                { 
                    Core.ValueObjects.Money.Create(50m, "USD") 
                };

                var eventEntity = new Event(
                    title: request.Title,
                    description: request.Description,
                    startDate: startDate,
                    endDate: endDate,
                    capacity: 20, // Default capacity
                    eventType: Core.Enums.EventType.Class,
                    location: request.Location,
                    primaryOrganizer: organizer,
                    pricingTiers: basicPricing
                );

                _dbContext.Events.Add(eventEntity);
                await _dbContext.SaveChangesAsync(ct);

                var eventDto = MapToEventDto(eventEntity);
                
                _logger.LogInformation("Successfully created event {EventId}", eventEntity.Id);
                return Result<EventDto>.Success(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event with sessions");
                return Result<EventDto>.Failure("Failed to create event");
            }
        }

        public async Task<Result<EventDto>> GetEventWithSessionsAsync(
            Guid eventId, 
            CancellationToken ct = default)
        {
            try
            {
                var eventEntity = await _dbContext.Events
                    .Include(e => e.Sessions)
                    .Include(e => e.TicketTypes)
                    .FirstOrDefaultAsync(e => e.Id == eventId, ct);

                if (eventEntity == null)
                {
                    return Result<EventDto>.Failure("Event not found");
                }

                var eventDto = MapToEventDto(eventEntity);
                return Result<EventDto>.Success(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event {EventId}", eventId);
                return Result<EventDto>.Failure("Failed to retrieve event");
            }
        }

        public async Task<Result<List<SessionCapacityInfo>>> GetSessionCapacityAsync(
            Guid eventId, 
            CancellationToken ct = default)
        {
            try
            {
                var eventEntity = await _dbContext.Events
                    .Include(e => e.Sessions)
                    .FirstOrDefaultAsync(e => e.Id == eventId, ct);

                if (eventEntity == null)
                {
                    return Result<List<SessionCapacityInfo>>.Failure("Event not found");
                }

                var capacityInfo = eventEntity.Sessions
                    .Select(s => new SessionCapacityInfo
                    {
                        SessionIdentifier = s.SessionIdentifier,
                        Name = s.Name,
                        Capacity = s.Capacity,
                        RegisteredCount = s.RegisteredCount,
                        AvailableSpots = s.GetAvailableSpots()
                    })
                    .ToList();

                return Result<List<SessionCapacityInfo>>.Success(capacityInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session capacity for event {EventId}", eventId);
                return Result<List<SessionCapacityInfo>>.Failure("Failed to retrieve session capacity");
            }
        }

        private static EventDto MapToEventDto(Event eventEntity)
        {
            return new EventDto
            {
                Id = eventEntity.Id,
                Name = eventEntity.Title,
                Description = eventEntity.Description,
                Location = eventEntity.Location,
                StartDateTime = eventEntity.StartDate,
                EndDateTime = eventEntity.EndDate,
                MaxAttendees = eventEntity.Capacity,
                CurrentAttendees = eventEntity.GetConfirmedRegistrationCount(),
                Status = eventEntity.IsPublished ? "Published" : "Draft"
            };
        }
    }
}