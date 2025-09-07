using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.Models;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Features.Events.Services;

public class EventService : IEventService
{
    private readonly WitchCityRopeDbContext _dbContext;
    // TODO: Implement these services
    // private readonly IUserContext _userContext;
    // private readonly Core.Interfaces.IPaymentService _paymentService;
    // private readonly INotificationService _notificationService;
    private readonly ISlugGenerator _slugGenerator;

    public EventService(
        WitchCityRopeDbContext dbContext,
        ISlugGenerator slugGenerator)
    {
        _dbContext = dbContext;
        _slugGenerator = slugGenerator;
    }

    public async Task<Core.DTOs.CreateEventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
    {
        // Verify organizer exists and has permission to create events
        var organizer = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == organizerId);
            
        if (organizer == null)
        {
            throw new NotFoundException("Organizer not found");
        }

        if (organizer.Role != Core.Enums.UserRole.Organizer && organizer.Role != Core.Enums.UserRole.Administrator)
        {
            throw new ForbiddenException("User does not have permission to create events");
        }

        // Validate event timing
        if (request.StartDateTime <= DateTime.UtcNow)
        {
            throw new Core.Exceptions.ValidationException("Event start time must be in the future");
        }

        if (request.EndDateTime <= request.StartDateTime)
        {
            throw new Core.Exceptions.ValidationException("Event end time must be after start time");
        }

        // Check for scheduling conflicts at the same location
        var hasConflict = await _dbContext.Events
            .AnyAsync(e => e.Location == request.Location &&
                          e.IsPublished &&
                          ((e.StartDate <= request.StartDateTime && e.EndDate > request.StartDateTime) ||
                           (e.StartDate < request.EndDateTime && e.EndDate >= request.EndDateTime) ||
                           (e.StartDate >= request.StartDateTime && e.EndDate <= request.EndDateTime)));

        if (hasConflict)
        {
            throw new Core.Exceptions.ConflictException("Another event is already scheduled at this location during this time");
        }

        // Generate unique slug for the event
        var slug = _slugGenerator.GenerateSlug(request.Title);
        var uniqueSlug = await EnsureUniqueSlugAsync(slug);

        // Create the event using the constructor
        var pricingTiers = new List<Core.ValueObjects.Money>
        {
            Core.ValueObjects.Money.Create(request.Price, "USD")
        };
        
        var @event = new Event(
            title: request.Title,
            description: request.Description,
            startDate: request.StartDateTime,
            endDate: request.EndDateTime,
            capacity: request.MaxAttendees,
            eventType: (Core.Enums.EventType)request.Type,
            location: request.Location,
            primaryOrganizer: organizer,
            pricingTiers: pricingTiers
        );
        
        // Publish the event immediately
        @event.Publish();

        _dbContext.Events.Add(@event);
        await _dbContext.SaveChangesAsync();

        // TODO: Send notifications to interested members based on tags/preferences

        return new Core.DTOs.CreateEventResponse
        {
            EventId = @event.Id,
            Message = $"Event '{@event.Title}' created successfully"
        };
    }

    public async Task<ListEventsResponse> ListEventsAsync(ListEventsRequest request)
    {
        // Validate pagination parameters
        if (request.Page < 1) throw new Core.Exceptions.ValidationException("Page must be greater than 0");
        if (request.PageSize < 1 || request.PageSize > 100)
            throw new Core.Exceptions.ValidationException("PageSize must be between 1 and 100");

        // Build base query
        var query = _dbContext.Events
            .Include(e => e.Organizers)
            .Where(e => e.IsPublished)
            .AsQueryable();

        // Apply filters
        var startDateFrom = request.StartDateFrom ?? DateTime.UtcNow;
        query = query.Where(e => e.StartDate >= startDateFrom);

        if (request.StartDateTo.HasValue)
        {
            query = query.Where(e => e.StartDate <= request.StartDateTo.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(e => e.EventType == (Core.Enums.EventType)request.Type.Value);
        }

        if (request.Tags?.Any() == true)
        {
            // TODO: Add tags to event entity
            // query = query.Where(e => e.Tags.Any(t => request.Tags.Contains(t)));
        }

        if (request.SkillLevels?.Any() == true)
        {
            // TODO: Add skill levels to event entity
            // query = query.Where(e => e.RequiredSkillLevels.Any(s => request.SkillLevels.Contains(s)));
        }

        if (request.HasAvailability.HasValue && request.HasAvailability.Value)
        {
            query = query.Where(e => e.GetAvailableSpots() > 0);
        }

        if (request.RequiresVetting.HasValue)
        {
            // TODO: Add RequiresVetting to event entity
            // query = query.Where(e => e.RequiresVetting == request.RequiresVetting.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(e => 
                e.Title.ToLower().Contains(searchTerm) ||
                e.Description.ToLower().Contains(searchTerm) ||
                e.Location.ToLower().Contains(searchTerm));
        }

        // Apply user-specific filters
        // TODO: Implement user context
        // var currentUser = await _userContext.GetCurrentUserAsync();
        // if (currentUser == null)
        {
            // Non-authenticated or non-vetted users can't see vetting-required events
            // TODO: Add RequiresVetting filter
            // query = query.Where(e => !e.RequiresVetting);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var skip = (request.Page - 1) * request.PageSize;

        // For sorting by price or available spots, we need to materialize the query first
        // since these involve computed values that EF Core can't translate
        List<Event> sortedEvents;
        
        if (request.SortBy == EventSortBy.Price || request.SortBy == EventSortBy.AvailableSpots)
        {
            // Get all filtered events first (without pagination)
            var allEvents = await query.ToListAsync();
            
            // Apply sorting in memory
            sortedEvents = request.SortBy switch
            {
                EventSortBy.Price => request.SortDirection == SortDirection.Ascending
                    ? allEvents.OrderBy(e => e.PricingTiers.Any() ? e.PricingTiers.Min(p => p.Amount) : 0).ToList()
                    : allEvents.OrderByDescending(e => e.PricingTiers.Any() ? e.PricingTiers.Min(p => p.Amount) : 0).ToList(),
                EventSortBy.AvailableSpots => request.SortDirection == SortDirection.Ascending
                    ? allEvents.OrderBy(e => e.GetAvailableSpots()).ToList()
                    : allEvents.OrderByDescending(e => e.GetAvailableSpots()).ToList(),
                _ => allEvents // This case won't be reached, but needed for completeness
            };
            
            // Apply pagination
            sortedEvents = sortedEvents
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();
        }
        else
        {
            // For other sorting options, we can use database sorting
            query = request.SortBy switch
            {
                EventSortBy.Title => request.SortDirection == SortDirection.Ascending
                    ? query.OrderBy(e => e.Title)
                    : query.OrderByDescending(e => e.Title),
                _ => request.SortDirection == SortDirection.Ascending
                    ? query.OrderBy(e => e.StartDate)
                    : query.OrderByDescending(e => e.StartDate)
            };
            
            // Get paginated results
            sortedEvents = await query
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync();
        }

        // Project to DTOs
        var events = sortedEvents.Select(e => new EventSummaryDto(
            e.Id,
            e.Title,
            _slugGenerator.GenerateSlug(e.Title), // Generate slug from title
            e.Description.Length > 200 ? e.Description.Substring(0, 200) + "..." : e.Description,
            (EventType)e.EventType,
            e.StartDate, // StartDateTime
            e.EndDate, // EndDateTime
            e.Location,
            e.Capacity, // MaxAttendees
            e.GetConfirmedRegistrationCount(), // CurrentAttendees
            e.GetAvailableSpots(),
            e.PricingTiers.Any() ? e.PricingTiers.Min(p => p.Amount) : 0, // Price with null check
            new List<string>(), // TODO: Add tags to event entity
            new List<string>(), // TODO: Add skill levels to event entity
            false, // TODO: Add RequiresVetting to event entity
            e.Organizers.FirstOrDefault() != null ? e.Organizers.First().SceneName.Value : "Unknown",
            "" // TODO: Add thumbnail URL
        )).ToList();

        return new ListEventsResponse(
            Events: events,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );
    }

    public async Task<GetFeaturedEventsResponse> GetFeaturedEventsAsync(GetFeaturedEventsRequest request)
    {
        // Get upcoming events with available spots, prioritizing those starting soon
        // First get the events from the database
        var eventsQuery = await _dbContext.Events
            .Include(e => e.Organizers)
            .Where(e => e.IsPublished)
            .Where(e => e.StartDate >= DateTime.UtcNow)
            .Where(e => e.StartDate <= DateTime.UtcNow.AddDays(30))
            // TODO: Add RequiresVetting filter
            // .Where(e => !e.RequiresVetting) // Featured events should be public
            .OrderBy(e => e.StartDate)
            .ToListAsync();
        
        // Filter by available spots in memory (since GetAvailableSpots() can't be translated)
        var filteredEvents = eventsQuery
            .Where(e => e.GetAvailableSpots() > 0)
            .Take(request.Count)
            .ToList();
        
        // Project to DTOs
        var events = filteredEvents.Select(e => new EventSummaryDto(
            e.Id,
            e.Title,
            _slugGenerator.GenerateSlug(e.Title), // Generate slug from title
            e.Description.Length > 150 ? e.Description.Substring(0, 150) + "..." : e.Description,
            (EventType)e.EventType,
            e.StartDate, // StartDateTime
            e.EndDate, // EndDateTime
            e.Location,
            e.Capacity, // MaxAttendees
            e.GetConfirmedRegistrationCount(), // CurrentAttendees
            e.GetAvailableSpots(),
            e.PricingTiers.Any() ? e.PricingTiers.Min(p => p.Amount) : 0, // Price with null check
            new List<string>(), // TODO: Add tags to event entity
            new List<string>(), // TODO: Add skill levels to event entity
            false, // TODO: Add RequiresVetting to event entity
            e.Organizers.FirstOrDefault() != null ? e.Organizers.First().SceneName.Value : "Unknown",
            "" // TODO: Add thumbnail URL
        )).ToList();

        return new GetFeaturedEventsResponse(Events: events);
    }

    public async Task<RegisterForEventResponse> RegisterForEventAsync(RegisterForEventRequest request)
    {
        // Get event details
        var @event = await _dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId);
            
        if (@event == null)
        {
            throw new NotFoundException("Event not found");
        }

        // Check if event is still open for registration
        if (!@event.IsPublished)
        {
            throw new Core.Exceptions.ValidationException("Event is not open for registration");
        }

        if (@event.StartDate <= DateTime.UtcNow)
        {
            throw new Core.Exceptions.ValidationException("Cannot register for past events");
        }

        // Get user details
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId);
            
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Check if user is already registered
        var existingRegistration = await _dbContext.Registrations
            .AnyAsync(r => r.EventId == request.EventId && r.UserId == request.UserId);

        if (existingRegistration)
        {
            throw new Core.Exceptions.ConflictException("User is already registered for this event");
        }

        // Check if event requires vetting
        // TODO: Add vetting checks
        // if (@event.RequiresVetting && !user.IsVetted)
        {
            // Create pending registration
            var vettingRegistration = await CreateRegistrationAsync(
                request,
                @event,
                RegistrationStatus.RequiresVetting,
                null
            );

            return new RegisterForEventResponse(
                RegistrationId: vettingRegistration.Id,
                Status: RegistrationStatus.RequiresVetting,
                WaitlistPosition: null,
                AmountCharged: 0,
                ConfirmationCode: GenerateConfirmationCode()
            );
        }

        // Check event capacity
        var currentRegistrations = await _dbContext.Registrations
            .CountAsync(r => r.EventId == request.EventId && 
                            r.Status == Core.Enums.RegistrationStatus.Confirmed);
        var isWaitlisted = currentRegistrations >= @event.Capacity;

        // TODO: Process payment if not waitlisted and event has a price
        decimal amountCharged = 0;
        // if (!isWaitlisted && @event.PricingTiers.Any() && @event.PricingTiers.Min(p => p.Amount) > 0)
        {
            try
            {
                // TODO: Implement payment processing when payment service is available
                // var paymentResult = await _paymentService.ProcessPaymentAsync(new PaymentRequest
                // {
                //     Amount = @event.PricingTiers.Min(p => p.Amount),
                //     Currency = "USD",
                //     Description = $"Registration for {@event.Title}",
                //     PaymentMethod = request.PaymentMethod,
                //     PaymentToken = request.PaymentToken,
                //     CustomerId = user.Id.ToString()
                // });

                // if (!paymentResult.Success)
                // {
                //     throw new PaymentException(paymentResult.ErrorMessage ?? "Payment failed");
                // }
                // amountCharged = paymentResult.AmountCharged;
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"Payment processing not yet implemented: {ex.Message}");
            }
        }

        // Create registration
        var status = isWaitlisted ? RegistrationStatus.Waitlisted : RegistrationStatus.Confirmed;
        var waitlistPosition = isWaitlisted
            ? 1 // TODO: Implement GetNextWaitlistPositionAsync
            : (int?)null;

        // TODO: Implement CreateRegistrationAsync properly
        var selectedPrice = @event.PricingTiers.FirstOrDefault() ?? Core.ValueObjects.Money.Zero("USD");
        var registration = new Registration(
            user: user,
            eventToRegister: @event,
            selectedPrice: selectedPrice,
            dietaryRestrictions: request.DietaryRestrictions,
            accessibilityNeeds: request.AccessibilityNeeds
        );
        
        _dbContext.Registrations.Add(registration);
        await _dbContext.SaveChangesAsync();

        // TODO: Send confirmation email
        // await _notificationService.SendEventRegistrationConfirmationAsync(
        //     user.Email,
        //     user.DisplayName,
        //     @event.Title,
        //     @event.StartDateTime,
        //     registration.ConfirmationCode,
        //     (Core.Enums.RegistrationStatus)status,
        //     waitlistPosition
        // );

        // TODO: Update event attendee count if confirmed
        // Event entity doesn't have CurrentAttendees property
        // if (status == RegistrationStatus.Confirmed)
        // {
        //     @event.CurrentAttendees++;
        //     await _dbContext.SaveChangesAsync();
        // }

        return new RegisterForEventResponse(
            RegistrationId: registration.Id,
            Status: status,
            WaitlistPosition: waitlistPosition,
            AmountCharged: amountCharged,
            ConfirmationCode: GenerateConfirmationCode()
        );
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 1;

        // TODO: Add slug to Event entity
        // while (await _dbContext.Events.AnyAsync(e => e.Slug == slug))
        // {
        //     slug = $"{baseSlug}-{counter}";
        //     counter++;
        // }

        return slug;
    }

    private static string GenerateConfirmationCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }

    private async Task<Registration> CreateRegistrationAsync(
        RegisterForEventRequest request,
        Event @event,
        RegistrationStatus status,
        int? waitlistPosition)
    {
        // Get user entity for registration
        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null)
            throw new NotFoundException("User not found");
            
        // Get the pricing tier (using minimum price for now)
        var selectedPrice = @event.PricingTiers.OrderBy(p => p.Amount).First();
        
        var registration = new Registration(
            user: user,
            eventToRegister: @event,
            selectedPrice: selectedPrice,
            dietaryRestrictions: request.DietaryRestrictions,
            accessibilityNeeds: request.AccessibilityNeeds
        );
        
        // TODO: Store emergency contact information separately
        // TODO: Handle waitlist status properly
        
        _dbContext.Registrations.Add(registration);
        await _dbContext.SaveChangesAsync();
        
        return registration;
    }

    private async Task<int> GetNextWaitlistPositionAsync(Guid eventId)
    {
        // Count the number of waitlisted registrations for this event
        var waitlistCount = await _dbContext.Registrations
            .Where(r => r.EventId == eventId && r.Status == Core.Enums.RegistrationStatus.Waitlisted)
            .CountAsync();

        return waitlistCount + 1;
    }


    // Additional methods for API endpoints
    public async Task<Core.Models.PagedResult<Core.DTOs.EventDto>> GetEventsAsync(int page, int pageSize, string? search)
    {
        // TODO: Implement GetEventsAsync
        throw new NotImplementedException();
    }



    public async Task UpdateEventAsync(Guid id, Core.DTOs.UpdateEventRequest request, Guid userId)
    {
        // Check if user has permission to update
        var @event = await _dbContext.Events
            .Include(e => e.Organizers)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (@event == null)
            throw new NotFoundException($"Event with ID {id} not found");
            
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");
            
        var isOrganizer = @event.Organizers.Any(o => o.Id == userId);
        var isAdminOrOrganizerRole = user.Role == Core.Enums.UserRole.Administrator || user.Role == Core.Enums.UserRole.Organizer;
        
        if (!isOrganizer && !isAdminOrOrganizerRole)
            throw new ForbiddenException("User does not have permission to update this event");

        // Update event details based on request
        if (request.Name != null || request.Description != null || request.Location != null)
        {
            @event.UpdateDetails(
                request.Name ?? @event.Title,
                request.Description ?? @event.Description,
                request.Location ?? @event.Location
            );
        }
        
        if (request.StartDateTime.HasValue && request.EndDateTime.HasValue)
            @event.UpdateDates(request.StartDateTime.Value, request.EndDateTime.Value);
            
        if (request.MaxAttendees.HasValue)
            @event.UpdateCapacity(request.MaxAttendees.Value);
            
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Core.Models.PagedResult<Core.DTOs.EventDto>> GetEventsAsync(int? pageNumber, int? pageSize, string? eventType, string? search)
    {
        var query = _dbContext.Events.AsQueryable();

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(e => e.EventType.ToString() == eventType);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e => e.Title.Contains(search) || e.Description.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var page = pageNumber ?? 1;
        var size = pageSize ?? 10;

        var events = await query
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new Core.DTOs.EventDto
            {
                Id = e.Id,
                Name = e.Title,
                Description = e.Description,
                StartDateTime = e.StartDate,
                EndDateTime = e.EndDate,
                Location = e.Location,
                MaxAttendees = e.Capacity,
                CurrentAttendees = e.Registrations.Count(r => r.Status == Core.Enums.RegistrationStatus.Confirmed),
                Price = e.PricingTiers.Any() ? e.PricingTiers.First().Amount : 0,
                Status = e.IsPublished ? "Published" : "Draft",
                Tags = new List<string>(), // TODO: Add tags to event entity
                RequiredSkillLevels = new List<string>(), // TODO: Add skill levels to event entity
                RequiresVetting = false // TODO: Add RequiresVetting to event entity
            })
            .ToListAsync();

        return new Core.Models.PagedResult<Core.DTOs.EventDto>
        {
            Items = events,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = size
        };
    }

    public async Task<Core.DTOs.EventDto?> GetEventByIdAsync(Guid id)
    {
        var @event = await _dbContext.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (@event == null)
            return null;

        return new Core.DTOs.EventDto
        {
            Id = @event.Id,
            Name = @event.Title,
            Description = @event.Description,
            StartDateTime = @event.StartDate,
            EndDateTime = @event.EndDate,
            Location = @event.Location,
            MaxAttendees = @event.Capacity,
            CurrentAttendees = @event.Registrations.Count(r => r.Status == Core.Enums.RegistrationStatus.Confirmed),
            Price = @event.PricingTiers.FirstOrDefault()?.Amount ?? 0,
            Status = @event.IsPublished ? "Published" : "Draft",
            Tags = new List<string>(), // TODO: Add tags to event entity
            RequiredSkillLevels = new List<string>(), // TODO: Add skill levels to event entity
            RequiresVetting = false // TODO: Add RequiresVetting to event entity
        };
    }

    #region Event Session Management

    public async Task<(bool Success, string Message, Features.Events.Models.EventSessionDto? Session)> CreateEventSessionAsync(Guid eventId, Features.Events.Models.CreateEventSessionRequest request)
    {
        try
        {
            // Validate the event exists
            var @event = await _dbContext.Events
                .Include(e => e.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null)
                return (false, "Event not found", null);

            // Validate session identifier is unique within the event
            if (@event.Sessions.Any(s => s.SessionIdentifier.Equals(request.SessionIdentifier, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Session identifier '{request.SessionIdentifier}' already exists for this event", null);

            // Create new session
            var session = new Core.Entities.EventSession(
                @event,
                request.SessionIdentifier,
                request.Name,
                request.StartDateTime,
                request.EndDateTime,
                request.Capacity
            );

            // Add session to event
            @event.AddSession(session);
            _dbContext.EventSessions.Add(session);

            await _dbContext.SaveChangesAsync();

            // Create DTO for response
            var sessionDto = new Features.Events.Models.EventSessionDto
            {
                Id = session.Id,
                EventId = session.EventId,
                SessionIdentifier = session.SessionIdentifier,
                Name = session.Name,
                StartDateTime = session.StartDateTime,
                EndDateTime = session.EndDateTime,
                Capacity = session.Capacity,
                IsActive = session.IsActive,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };

            return (true, "Session created successfully", sessionDto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message, Features.Events.Models.EventSessionDto? Session)> UpdateEventSessionAsync(Guid sessionId, Features.Events.Models.UpdateEventSessionRequest request)
    {
        try
        {
            var session = await _dbContext.EventSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return (false, "Session not found", null);

            // Update session details
            session.UpdateDetails(
                request.Name,
                request.StartDateTime,
                request.EndDateTime,
                request.Capacity
            );

            await _dbContext.SaveChangesAsync();

            // Create DTO for response
            var sessionDto = new Features.Events.Models.EventSessionDto
            {
                Id = session.Id,
                EventId = session.EventId,
                SessionIdentifier = session.SessionIdentifier,
                Name = session.Name,
                StartDateTime = session.StartDateTime,
                EndDateTime = session.EndDateTime,
                Capacity = session.Capacity,
                IsActive = session.IsActive,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };

            return (true, "Session updated successfully", sessionDto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteEventSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _dbContext.EventSessions
                .Include(s => s.TicketTypeInclusions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return (false, "Session not found");

            // Check if session is included in any ticket types
            if (session.TicketTypeInclusions.Any())
                return (false, "Cannot delete session that is included in ticket types");

            // Soft delete by deactivating
            session.Deactivate();
            await _dbContext.SaveChangesAsync();

            return (true, "Session deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<ICollection<Features.Events.Models.EventSessionDto>> GetEventSessionsAsync(Guid eventId)
    {
        var sessions = await _dbContext.EventSessions
            .Where(s => s.EventId == eventId && s.IsActive)
            .OrderBy(s => s.SessionIdentifier)
            .Select(s => new Features.Events.Models.EventSessionDto
            {
                Id = s.Id,
                EventId = s.EventId,
                SessionIdentifier = s.SessionIdentifier,
                Name = s.Name,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                Capacity = s.Capacity,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return sessions;
    }

    #endregion

    #region Event Ticket Type Management

    public async Task<(bool Success, string Message, Features.Events.Models.EventTicketTypeDto? TicketType)> CreateEventTicketTypeAsync(Guid eventId, Features.Events.Models.CreateEventTicketTypeRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Validate the event exists and load sessions
            var @event = await _dbContext.Events
                .Include(e => e.Sessions.Where(s => s.IsActive))
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null)
                return (false, "Event not found", null);

            // Validate ticket type name is unique within the event
            if (@event.TicketTypes.Any(tt => tt.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Ticket type with name '{request.Name}' already exists for this event", null);

            // Validate all included session identifiers exist
            var includedSessions = @event.Sessions
                .Where(s => request.IncludedSessionIdentifiers.Contains(s.SessionIdentifier))
                .ToList();

            if (includedSessions.Count != request.IncludedSessionIdentifiers.Count)
            {
                var missing = request.IncludedSessionIdentifiers.Except(includedSessions.Select(s => s.SessionIdentifier));
                return (false, $"Session identifiers not found: {string.Join(", ", missing)}", null);
            }

            // Create new ticket type
            var ticketType = new Core.Entities.EventTicketType(
                @event,
                request.Name,
                request.TicketType,
                request.MinPrice,
                request.MaxPrice,
                request.QuantityAvailable,
                request.SalesEndDateTime
            );

            // Add session inclusions
            foreach (var session in includedSessions)
            {
                ticketType.AddSessionInclusion(session);
            }

            // Add ticket type to event
            @event.AddTicketType(ticketType);
            _dbContext.EventTicketTypes.Add(ticketType);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Create DTO for response
            var ticketTypeDto = await CreateTicketTypeDto(ticketType);

            return (true, "Ticket type created successfully", ticketTypeDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message, Features.Events.Models.EventTicketTypeDto? TicketType)> UpdateEventTicketTypeAsync(Guid ticketTypeId, Features.Events.Models.UpdateEventTicketTypeRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            var ticketType = await _dbContext.EventTicketTypes
                .Include(tt => tt.Event.Sessions.Where(s => s.IsActive))
                .Include(tt => tt.SessionInclusions)
                    .ThenInclude(si => si.EventSession)
                .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId);

            if (ticketType == null)
                return (false, "Ticket type not found", null);

            // Validate all included session identifiers exist
            var availableSessions = ticketType.Event.Sessions.ToList();
            var includedSessions = availableSessions
                .Where(s => request.IncludedSessionIdentifiers.Contains(s.SessionIdentifier))
                .ToList();

            if (includedSessions.Count != request.IncludedSessionIdentifiers.Count)
            {
                var missing = request.IncludedSessionIdentifiers.Except(includedSessions.Select(s => s.SessionIdentifier));
                return (false, $"Session identifiers not found: {string.Join(", ", missing)}", null);
            }

            // Update ticket type details
            ticketType.UpdateDetails(
                request.Name,
                request.MinPrice,
                request.MaxPrice,
                request.QuantityAvailable,
                request.SalesEndDateTime
            );

            // Remove existing session inclusions and add new ones
            var existingInclusions = ticketType.SessionInclusions.ToList();
            foreach (var inclusion in existingInclusions)
            {
                _dbContext.EventTicketTypeSessions.Remove(inclusion);
            }

            // Add new session inclusions
            foreach (var session in includedSessions)
            {
                var inclusion = new Core.Entities.EventTicketTypeSession(ticketType, session);
                _dbContext.EventTicketTypeSessions.Add(inclusion);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Create DTO for response
            var ticketTypeDto = await CreateTicketTypeDto(ticketType, includedSessions);

            return (true, "Ticket type updated successfully", ticketTypeDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteEventTicketTypeAsync(Guid ticketTypeId)
    {
        try
        {
            var ticketType = await _dbContext.EventTicketTypes
                .Include(tt => tt.Registrations)
                .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId);

            if (ticketType == null)
                return (false, "Ticket type not found");

            // Check if ticket type has confirmed registrations
            if (ticketType.GetConfirmedRegistrationCount() > 0)
                return (false, "Cannot delete ticket type with confirmed registrations");

            // Soft delete by deactivating
            ticketType.Deactivate();
            await _dbContext.SaveChangesAsync();

            return (true, "Ticket type deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<ICollection<Features.Events.Models.EventTicketTypeDto>> GetEventTicketTypesAsync(Guid eventId)
    {
        var ticketTypes = await _dbContext.EventTicketTypes
            .Include(tt => tt.SessionInclusions)
                .ThenInclude(si => si.EventSession)
            .Where(tt => tt.EventId == eventId && tt.IsActive)
            .OrderBy(tt => tt.Name)
            .ToListAsync();

        var ticketTypeDtos = new List<Features.Events.Models.EventTicketTypeDto>();
        
        foreach (var ticketType in ticketTypes)
        {
            var dto = await CreateTicketTypeDto(ticketType);
            ticketTypeDtos.Add(dto);
        }

        return ticketTypeDtos;
    }

    private async Task<Features.Events.Models.EventTicketTypeDto> CreateTicketTypeDto(Core.Entities.EventTicketType ticketType, ICollection<Core.Entities.EventSession>? sessions = null)
    {
        // If sessions not provided, load them
        if (sessions == null)
        {
            var sessionIds = ticketType.SessionInclusions.Select(si => si.EventSessionId).ToList();
            sessions = await _dbContext.EventSessions
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();
        }

        return new Features.Events.Models.EventTicketTypeDto
        {
            Id = ticketType.Id,
            EventId = ticketType.EventId,
            Name = ticketType.Name,
            TicketType = ticketType.TicketType,
            MinPrice = ticketType.MinPrice,
            MaxPrice = ticketType.MaxPrice,
            QuantityAvailable = ticketType.QuantityAvailable,
            SalesEndDateTime = ticketType.SalesEndDateTime,
            IsActive = ticketType.IsActive,
            CreatedAt = ticketType.CreatedAt,
            UpdatedAt = ticketType.UpdatedAt,
            IncludedSessions = sessions.Select(s => s.SessionIdentifier).ToList(),
            AvailableQuantity = ticketType.GetAvailableQuantity(),
            SalesOpen = ticketType.AreSalesOpen()
        };
    }

    #endregion

}