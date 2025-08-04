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
    private readonly WitchCityRopeIdentityDbContext _dbContext;
    // TODO: Implement these services
    // private readonly IUserContext _userContext;
    // private readonly Core.Interfaces.IPaymentService _paymentService;
    // private readonly INotificationService _notificationService;
    private readonly ISlugGenerator _slugGenerator;

    public EventService(
        WitchCityRopeIdentityDbContext dbContext,
        ISlugGenerator slugGenerator)
    {
        _dbContext = dbContext;
        _slugGenerator = slugGenerator;
    }

    public async Task<Core.DTOs.CreateEventResponse> CreateEventAsync(Features.Events.Models.CreateEventRequest request, Guid organizerId)
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

    public async Task<Features.Events.Models.RegisterForEventResponse> RegisterForEventAsync(Features.Events.Models.RegisterForEventRequest request)
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
        Features.Events.Models.RegisterForEventRequest request,
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

}