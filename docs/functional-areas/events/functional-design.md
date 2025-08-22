# Events Management - Functional Design
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Events Team -->
<!-- Status: Active -->

## Overview
This document describes the technical implementation of the WitchCityRope events management system, including the domain model, API design, and integration points.

## Architecture Overview

### System Components
```
┌─────────────────┐     ┌─────────────────┐     ┌──────────────┐
│   Blazor Web    │────▶│   Events API    │────▶│  PostgreSQL  │
│  (Presentation) │     │   (Business)    │     │  (Database)  │
└─────────────────┘     └─────────────────┘     └──────────────┘
         │                       │
         │                       ├── Domain Logic
         ├── UI Components       ├── Validation
         ├── Forms              └── Integration
         └── Navigation
```

## Domain Model

### Core Entities

#### Event Entity
```csharp
public class Event : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventType Type { get; private set; } // Class or Social
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int Capacity { get; private set; }
    public Location Location { get; private set; }
    public bool RequiresVetting { get; private set; }
    public EventStatus Status { get; private set; }
    public List<PricingTier> PricingTiers { get; private set; }
    public List<EventOrganizer> Organizers { get; private set; }
    
    // Computed properties
    public int AvailableSpots => Capacity - ConfirmedAttendees;
    public bool IsFull => AvailableSpots <= 0;
    public bool CanRsvp => Type == EventType.Social;
    public bool RequiresPayment => Type == EventType.Class;
}
```

#### RSVP Entity (Social Events)
```csharp
public class Rsvp : Entity
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string ConfirmationCode { get; private set; } // RSVP-XXXXXXXX
    public RsvpStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    
    public static Result<Rsvp> Create(Guid eventId, Guid userId)
    {
        var code = $"RSVP-{GenerateCode(8)}";
        return new Rsvp(eventId, userId, code);
    }
}
```

#### Ticket Entity (All Events)
```csharp
public class Ticket : Entity
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string ConfirmationCode { get; private set; } // TKT-YYYYMMDDHHMM-XXXX
    public TicketStatus Status { get; private set; }
    public Money Price { get; private set; }
    public string? EmergencyContact { get; private set; }
    public string? DietaryRestrictions { get; private set; }
    public string? AccessibilityNeeds { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    public RefundInfo? RefundInfo { get; private set; }
}
```

### Value Objects

#### EventType Enum
```csharp
public enum EventType
{
    Class,    // Educational workshop
    Social    // Community gathering
}
```

#### Status Enums
```csharp
public enum RsvpStatus
{
    Confirmed,
    Waitlisted,
    CheckedIn,
    Cancelled
}

public enum TicketStatus
{
    Pending,
    Confirmed,
    Waitlisted,
    CheckedIn,
    Cancelled
}
```

#### PricingTier
```csharp
public class PricingTier : ValueObject
{
    public string Name { get; }
    public Money Price { get; }
    public string? Description { get; }
    
    // Examples: "Student" ($15), "Standard" ($25), "Supporter" ($35)
}
```

## API Design

### Event Endpoints

#### Public Endpoints
```csharp
// Browse events
GET /api/events
Query params: ?type={class|social}&startDate={date}&endDate={date}

// Get event details
GET /api/events/{id}

// Get user's attendance status
GET /api/events/{id}/attendance
Returns: { hasRsvp: bool, hasTicket: bool, status: string }
```

#### Registration Endpoints
```csharp
// RSVP for social event (free)
POST /api/events/{id}/rsvp
Auth: Required
Body: { emergencyContact?: string }
Returns: { confirmationCode: "RSVP-XXXXXXXX", status: "confirmed|waitlisted" }

// Purchase ticket
POST /api/events/{id}/tickets
Auth: Required
Body: { 
    priceId: guid,
    emergencyContact: string,
    dietaryRestrictions?: string,
    accessibilityNeeds?: string,
    paymentToken: string
}
Returns: { confirmationCode: "TKT-...", status: "confirmed|waitlisted" }

// Cancel registration
POST /api/rsvps/{id}/cancel
POST /api/tickets/{id}/cancel
Body: { reason: string }
```

#### Admin Endpoints
```csharp
// Create event
POST /api/admin/events
Body: CreateEventRequest

// Update event
PUT /api/admin/events/{id}
Body: UpdateEventRequest

// Get attendee list
GET /api/admin/events/{id}/attendees
Returns: List of RSVPs and Tickets with user info

// Check in attendee
POST /api/events/{id}/checkin
Body: { confirmationCode: string }
Returns: { success: bool, attendeeName: string, type: "rsvp|ticket" }
```

## Business Logic Implementation

### Event Creation
```csharp
public class EventService
{
    public async Task<Result<Event>> CreateEventAsync(CreateEventRequest request)
    {
        // Validate business rules
        if (request.StartTime <= DateTime.UtcNow.AddHours(24))
            return Result.Failure("Events must be created 24 hours in advance");
            
        if (request.Type == EventType.Class && !request.PricingTiers.Any())
            return Result.Failure("Classes must have at least one pricing tier");
            
        // Auto-set vetting for social events
        var requiresVetting = request.Type == EventType.Social || request.RequiresVetting;
        
        var event = Event.Create(
            request.Title,
            request.Type,
            request.StartTime,
            requiresVetting);
            
        await _repository.AddAsync(event);
        return Result.Success(event);
    }
}
```

### RSVP Logic
```csharp
public class RsvpService
{
    public async Task<Result<Rsvp>> CreateRsvpAsync(Guid eventId, Guid userId)
    {
        var event = await _eventRepository.GetByIdAsync(eventId);
        
        // Validate event type
        if (event.Type != EventType.Social)
            return Result.Failure("RSVPs only available for social events");
            
        // Check vetting status
        var user = await _userRepository.GetByIdAsync(userId);
        if (event.RequiresVetting && !user.IsVetted)
            return Result.Failure("Event requires vetting");
            
        // Check existing registration
        var existing = await _rsvpRepository.GetByEventAndUserAsync(eventId, userId);
        if (existing != null)
            return Result.Failure("Already registered");
            
        // Check capacity
        var status = event.IsFull ? RsvpStatus.Waitlisted : RsvpStatus.Confirmed;
        
        var rsvp = Rsvp.Create(eventId, userId, status);
        await _rsvpRepository.AddAsync(rsvp);
        
        // Send confirmation email
        await _emailService.SendRsvpConfirmationAsync(rsvp, event, user);
        
        return Result.Success(rsvp);
    }
}
```

### Check-In Process
```csharp
public class CheckInService
{
    public async Task<Result<CheckInResult>> CheckInAsync(string confirmationCode)
    {
        // Try RSVP first
        if (confirmationCode.StartsWith("RSVP-"))
        {
            var rsvp = await _rsvpRepository.GetByCodeAsync(confirmationCode);
            if (rsvp == null) return Result.Failure("Invalid code");
            
            if (rsvp.Status != RsvpStatus.Confirmed)
                return Result.Failure($"Cannot check in {rsvp.Status} RSVP");
                
            rsvp.CheckIn(_currentUser.Id);
            await _rsvpRepository.UpdateAsync(rsvp);
            
            return Result.Success(new CheckInResult(rsvp));
        }
        
        // Try ticket
        if (confirmationCode.StartsWith("TKT-"))
        {
            var ticket = await _ticketRepository.GetByCodeAsync(confirmationCode);
            // Similar logic for tickets
        }
        
        return Result.Failure("Invalid confirmation code format");
    }
}
```

## Database Schema

### Events Table
```sql
CREATE TABLE events (
    id UUID PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    type VARCHAR(20) NOT NULL CHECK (type IN ('Class', 'Social')),
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    capacity INTEGER NOT NULL CHECK (capacity > 0),
    location_name VARCHAR(200),
    location_address TEXT,
    location_type VARCHAR(20), -- Physical, Online, Hybrid
    requires_vetting BOOLEAN NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_events_start_time ON events(start_time);
CREATE INDEX idx_events_type ON events(type);
CREATE INDEX idx_events_status ON events(status);
```

### RSVPs Table
```sql
CREATE TABLE rsvps (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(id),
    user_id UUID NOT NULL REFERENCES users(id),
    confirmation_code VARCHAR(20) UNIQUE NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    checked_in_at TIMESTAMPTZ,
    checked_in_by UUID REFERENCES users(id),
    cancelled_at TIMESTAMPTZ,
    cancellation_reason TEXT,
    UNIQUE(event_id, user_id)
);

CREATE INDEX idx_rsvps_event_status ON rsvps(event_id, status);
CREATE INDEX idx_rsvps_user ON rsvps(user_id);
CREATE INDEX idx_rsvps_confirmation ON rsvps(confirmation_code);
```

### Tickets Table
```sql
CREATE TABLE tickets (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(id),
    user_id UUID NOT NULL REFERENCES users(id),
    confirmation_code VARCHAR(30) UNIQUE NOT NULL,
    status VARCHAR(20) NOT NULL,
    price_amount DECIMAL(10,2) NOT NULL,
    price_currency VARCHAR(3) NOT NULL,
    pricing_tier_name VARCHAR(50),
    emergency_contact VARCHAR(200),
    dietary_restrictions TEXT,
    accessibility_needs TEXT,
    purchased_at TIMESTAMPTZ DEFAULT NOW(),
    payment_intent_id VARCHAR(100),
    checked_in_at TIMESTAMPTZ,
    checked_in_by UUID REFERENCES users(id),
    cancelled_at TIMESTAMPTZ,
    refund_amount DECIMAL(10,2),
    refund_processed_at TIMESTAMPTZ
);

CREATE INDEX idx_tickets_event_status ON tickets(event_id, status);
CREATE INDEX idx_tickets_user ON tickets(user_id);
CREATE INDEX idx_tickets_confirmation ON tickets(confirmation_code);
```

## Event Validation System (July 18, 2025)

### Overview
A centralized validation system was implemented to ensure consistency between API and UI layers, solving compilation errors and improving user experience.

### Components

#### EventValidationConstants
```csharp
public static class EventValidationConstants
{
    // Title validation
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    public const string TitlePattern = @"^[a-zA-Z0-9\s\-\.,:;!?()&'""]+$";
    
    // Description validation
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    
    // Capacity validation
    public const int CapacityMin = 1;
    public const int CapacityMax = 500;
    
    // Price validation
    public const decimal PriceMin = 0m;
    public const decimal PriceMax = 10000m;
    
    // Date validation
    public const int EventMinHoursInAdvance = 1;
    public const int EventMaxDaysInAdvance = 365;
    
    // Error messages
    public const string TitleRequired = "Event title is required";
    public const string TitleLength = "Title must be between {0} and {1} characters";
    public const string TitleInvalidChars = "Title contains invalid characters";
    // ... more error messages
}
```

#### EventValidationAttributes
```csharp
// Custom validation attribute for event dates
public class EventDateValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        if (value is DateTime dateTime)
        {
            var minDate = DateTime.UtcNow.AddHours(EventValidationConstants.EventMinHoursInAdvance);
            var maxDate = DateTime.UtcNow.AddDays(EventValidationConstants.EventMaxDaysInAdvance);
            
            if (dateTime < minDate)
                return new ValidationResult($"Event must be at least {EventValidationConstants.EventMinHoursInAdvance} hours in the future");
                
            if (dateTime > maxDate)
                return new ValidationResult($"Event cannot be more than {EventValidationConstants.EventMaxDaysInAdvance} days in the future");
        }
        
        return ValidationResult.Success;
    }
}

// Custom validation for event duration
public class EventDurationValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var instance = context.ObjectInstance;
        var startProperty = instance.GetType().GetProperty("StartDateTime");
        var endProperty = instance.GetType().GetProperty("EndDateTime");
        
        if (startProperty != null && endProperty != null)
        {
            var start = (DateTime?)startProperty.GetValue(instance);
            var end = (DateTime?)endProperty.GetValue(instance);
            
            if (start.HasValue && end.HasValue)
            {
                if (end <= start)
                    return new ValidationResult("End time must be after start time");
                    
                var duration = end.Value - start.Value;
                if (duration.TotalMinutes < 15)
                    return new ValidationResult("Event must be at least 15 minutes long");
                    
                if (duration.TotalHours > 12)
                    return new ValidationResult("Event cannot be longer than 12 hours");
            }
        }
        
        return ValidationResult.Success;
    }
}
```

#### EventValidationService
```csharp
public class EventValidationService : IEventValidationService
{
    public ValidationResult ValidateEvent(EventDto eventDto)
    {
        var errors = new List<string>();
        
        // Title validation
        if (string.IsNullOrWhiteSpace(eventDto.Title))
            errors.Add(EventValidationConstants.TitleRequired);
        else if (eventDto.Title.Length < EventValidationConstants.TitleMinLength || 
                 eventDto.Title.Length > EventValidationConstants.TitleMaxLength)
            errors.Add(string.Format(EventValidationConstants.TitleLength, 
                EventValidationConstants.TitleMinLength, 
                EventValidationConstants.TitleMaxLength));
        
        // Date validation
        var minDate = DateTime.UtcNow.AddHours(EventValidationConstants.EventMinHoursInAdvance);
        if (eventDto.StartDateTime < minDate)
            errors.Add($"Event must be scheduled at least {EventValidationConstants.EventMinHoursInAdvance} hours in advance");
        
        // Capacity validation for classes
        if (eventDto.Type == EventType.Class)
        {
            if (eventDto.Capacity < EventValidationConstants.CapacityMin || 
                eventDto.Capacity > EventValidationConstants.CapacityMax)
                errors.Add($"Capacity must be between {EventValidationConstants.CapacityMin} and {EventValidationConstants.CapacityMax}");
        }
        
        return errors.Any() 
            ? new ValidationResult(false, errors) 
            : new ValidationResult(true);
    }
}
```

### Usage in DTOs
```csharp
public class CreateEventRequest
{
    [Required(ErrorMessage = EventValidationConstants.TitleRequired)]
    [StringLength(EventValidationConstants.TitleMaxLength, 
        MinimumLength = EventValidationConstants.TitleMinLength,
        ErrorMessage = EventValidationConstants.TitleLength)]
    [RegularExpression(EventValidationConstants.TitlePattern, 
        ErrorMessage = EventValidationConstants.TitleInvalidChars)]
    public string Title { get; set; }
    
    [Required]
    [EventDateValidation]
    public DateTime StartDateTime { get; set; }
    
    [Required]
    [EventDurationValidation]
    public DateTime EndDateTime { get; set; }
    
    [Range(EventValidationConstants.CapacityMin, EventValidationConstants.CapacityMax)]
    public int Capacity { get; set; }
}
```

### UI Integration
```razor
@* WCR validation components automatically use these validation attributes *@
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <WcrInputText @bind-Value="model.Title" 
        Label="Event Title" 
        Required="true"
        MinLength="@EventValidationConstants.TitleMinLength"
        MaxLength="@EventValidationConstants.TitleMaxLength" />
    
    <WcrInputDateTime @bind-Value="model.StartDateTime" 
        Label="Start Date & Time"
        Required="true"
        Min="@DateTime.UtcNow.AddHours(EventValidationConstants.EventMinHoursInAdvance)"
        Max="@DateTime.UtcNow.AddDays(EventValidationConstants.EventMaxDaysInAdvance)" />
</EditForm>
```

### Key Benefits
1. **Single Source of Truth**: All validation rules defined in one place
2. **Consistency**: Same validation logic in API and UI
3. **Maintainability**: Change rules in one location
4. **Type Safety**: Strongly typed constants prevent errors
5. **User Experience**: Clear, consistent error messages
6. **Testability**: Validation logic easily unit tested

## Integration Points

### Payment Processing
- Stripe integration for ticket purchases
- Payment intents created during checkout
- Webhooks handle payment confirmation
- Automatic refund processing

### Email Service
- SendGrid for transactional emails
- Templates for each notification type
- Queued for reliability
- Tracking for delivery status

### Calendar Integration
- ICS file generation for events
- Google Calendar add links
- Timezone handling for displays

## Security Considerations

### Authorization
- Event creation requires admin or organizer role
- Event updates limited to assigned organizers
- Check-in requires staff privileges
- Financial reports require admin role

### Data Protection
- PII (emergency contacts) encrypted at rest
- Confirmation codes are cryptographically random
- No payment details stored locally
- Audit trail for all modifications

## Performance Optimizations

### Caching
- Event details cached for 5 minutes
- Capacity counts cached with invalidation
- User attendance status cached per request

### Query Optimization
- Indexed on common query patterns
- Eager loading for related data
- Pagination for attendee lists
- Async operations throughout

---

*This document describes the technical implementation. For business rules, see [business-requirements.md](business-requirements.md)*