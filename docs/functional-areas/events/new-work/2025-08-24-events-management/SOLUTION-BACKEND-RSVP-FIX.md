# Solution: Backend Domain Model Fix for RSVP Support

## Date: 2025-09-07
## Priority: CRITICAL - Blocking correct frontend implementation

## Part 1: Why Agents Missed the Information

### The Documentation Existed All Along

The librarian found **8 separate documents** clearly stating:
- **Classes**: Require ticket purchase (paid only)
- **Social Events**: Allow free RSVP + optional ticket purchase

These documents were created BEFORE implementation began:
- `/docs/functional-areas/events/requirements/business-requirements.md`
- `/docs/functional-areas/events/functional-design.md`
- `/docs/functional-areas/events/user-flows.md`

### Why Implementation Agents Didn't Use It

1. **Context Window Limitations**
   - Early research exceeds context when implementation begins
   - Agents can't "see" documents created earlier in workflow

2. **No Explicit Handoff**
   - Business requirements agent documented everything
   - Backend developer started fresh without requirements review
   - React developer implemented without seeing either

3. **Assumption Problem**
   - Backend developer saw `EventType` enum and `Registration` entity
   - Assumed this was correct implementation
   - Didn't validate against business requirements

4. **Missing Validation Step**
   - No checkpoint to compare implementation to requirements
   - No automated test for business rules
   - Manual review came too late

## Part 2: Backend Domain Model Fix

### Current Problem

```csharp
// Registration.cs - Line 123-138
public void Confirm(Payment payment)
{
    if (payment == null)
        throw new ArgumentNullException(nameof(payment));
    
    if (payment.Status != PaymentStatus.Completed)
        throw new DomainException("Payment must be completed");
    
    Status = RegistrationStatus.Confirmed;
    // PROBLEM: Cannot confirm without payment!
}
```

### Solution A: Create Separate RSVP Entity (Recommended)

**Why This Is Better:**
- Clean separation of concerns
- No risk of breaking existing Registration logic
- Explicit business rules in domain model
- Easier to test and maintain

#### 1. Create New RSVP Entity

```csharp
// src/WitchCityRope.Core/Entities/RSVP.cs
using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a free RSVP for a social event
    /// Business Rules:
    /// - Only available for Social Events
    /// - No payment required
    /// - Can optionally purchase ticket later
    /// - Can be cancelled anytime before event
    /// </summary>
    public class RSVP
    {
        private RSVP() { }

        public RSVP(User user, Event eventToAttend, string? dietaryRestrictions = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (eventToAttend == null)
                throw new ArgumentNullException(nameof(eventToAttend));

            if (eventToAttend.EventType != EventType.Social)
                throw new DomainException("RSVP is only available for social events");

            if (!eventToAttend.HasAvailableCapacity())
                throw new DomainException("Event is at full capacity");

            Id = Guid.NewGuid();
            UserId = user.Id;
            User = user;
            EventId = eventToAttend.Id;
            Event = eventToAttend;
            Status = RSVPStatus.Confirmed;
            DietaryRestrictions = dietaryRestrictions;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ConfirmationCode = GenerateConfirmationCode();
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; }
        public Guid EventId { get; private set; }
        public Event Event { get; private set; }
        public RSVPStatus Status { get; private set; }
        public string? DietaryRestrictions { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public string? CancellationReason { get; private set; }
        public string ConfirmationCode { get; private set; }
        
        // Link to ticket if user decides to purchase later
        public Guid? TicketId { get; private set; }
        public Registration? Ticket { get; private set; }

        public void Cancel(string? reason = null)
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("RSVP is already cancelled");

            Status = RSVPStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason ?? "User requested cancellation";
            UpdatedAt = DateTime.UtcNow;
        }

        public void LinkTicketPurchase(Registration ticket)
        {
            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));

            if (ticket.EventId != EventId)
                throw new DomainException("Ticket must be for the same event");

            TicketId = ticket.Id;
            Ticket = ticket;
            UpdatedAt = DateTime.UtcNow;
        }

        private string GenerateConfirmationCode()
        {
            var random = new Random().Next(10000000, 99999999);
            return $"RSVP-{random}";
        }
    }

    public enum RSVPStatus
    {
        Confirmed,
        Cancelled,
        CheckedIn
    }
}
```

#### 2. Update Event Entity

```csharp
// src/WitchCityRope.Core/Entities/Event.cs - Add these methods
public class Event : AggregateRoot
{
    // Existing properties...
    
    // Add navigation properties
    public ICollection<RSVP> RSVPs { get; private set; } = new List<RSVP>();
    public ICollection<Registration> Registrations { get; private set; } = new List<Registration>();

    // Business rule properties
    public bool AllowsRSVP => EventType == EventType.Social;
    public bool RequiresPayment => EventType == EventType.Workshop || EventType == EventType.Class;
    
    // Capacity calculation including both RSVPs and Registrations
    public int GetCurrentAttendeeCount()
    {
        var rsvpCount = RSVPs.Count(r => r.Status == RSVPStatus.Confirmed);
        var ticketCount = Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        return rsvpCount + ticketCount;
    }
    
    public bool HasAvailableCapacity()
    {
        return GetCurrentAttendeeCount() < MaxCapacity;
    }
    
    public int GetAvailableSpots()
    {
        return Math.Max(0, MaxCapacity - GetCurrentAttendeeCount());
    }
}
```

#### 3. Create RSVP Configuration

```csharp
// src/WitchCityRope.Infrastructure/Data/Configurations/RSVPConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class RSVPConfiguration : IEntityTypeConfiguration<RSVP>
    {
        public void Configure(EntityTypeBuilder<RSVP> builder)
        {
            builder.ToTable("RSVPs");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ConfirmationCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.DietaryRestrictions)
                .HasMaxLength(500);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Event)
                .WithMany(e => e.RSVPs)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Ticket)
                .WithOne()
                .HasForeignKey<RSVP>(r => r.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            // Prevent duplicate RSVPs
            builder.HasIndex(r => new { r.UserId, r.EventId })
                .IsUnique()
                .HasFilter("\"Status\" != 'Cancelled'");

            builder.HasIndex(r => r.ConfirmationCode)
                .IsUnique();
        }
    }
}
```

#### 4. Update DbContext

```csharp
// src/WitchCityRope.Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    // Existing DbSets...
    
    public DbSet<RSVP> RSVPs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Existing configurations...
        
        modelBuilder.ApplyConfiguration(new RSVPConfiguration());
    }
}
```

#### 5. Create Migration

```bash
cd src/WitchCityRope.Infrastructure
dotnet ef migrations add AddRSVPSupport -s ../WitchCityRope.Api
```

### Solution B: Extend Registration (Quick Fix - Not Recommended)

If we must work with existing Registration:

```csharp
// src/WitchCityRope.Core/Entities/Registration.cs
public class Registration
{
    // Add new property
    public bool IsFreeRSVP { get; private set; }
    
    // Add overload for free RSVP
    public void ConfirmAsRSVP()
    {
        if (Status != RegistrationStatus.Pending)
            throw new DomainException("Only pending registrations can be confirmed");

        if (Event.EventType != EventType.Social)
            throw new DomainException("RSVP is only available for social events");

        Status = RegistrationStatus.Confirmed;
        IsFreeRSVP = true;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        // No payment required!
    }
    
    // Modify existing Confirm to check
    public void Confirm(Payment payment)
    {
        if (Status != RegistrationStatus.Pending)
            throw new DomainException("Only pending registrations can be confirmed");

        // Allow null payment for social events
        if (Event.EventType == EventType.Social && payment == null)
        {
            ConfirmAsRSVP();
            return;
        }

        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        if (payment.Status != PaymentStatus.Completed)
            throw new DomainException("Payment must be completed");

        Status = RegistrationStatus.Confirmed;
        Payment = payment;
        IsFreeRSVP = false;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

## Part 3: API Endpoint Updates

### New Endpoints Required

```csharp
// src/WitchCityRope.Api/Features/Events/EventsEndpoints.cs

// RSVP for social event (free)
app.MapPost("/api/events/{id}/rsvp", async (
    Guid id,
    RSVPRequest request,
    IEventService eventService,
    HttpContext context) =>
{
    var userId = context.GetUserId();
    var result = await eventService.CreateRSVPAsync(id, userId, request);
    
    return result.Success 
        ? Results.Ok(result.RSVP)
        : Results.BadRequest(result.Error);
})
.RequireAuthorization()
.WithName("CreateRSVP")
.Produces<RSVPDto>(200)
.Produces<ProblemDetails>(400);

// Purchase ticket (paid)
app.MapPost("/api/events/{id}/tickets", async (
    Guid id,
    TicketPurchaseRequest request,
    IEventService eventService,
    HttpContext context) =>
{
    var userId = context.GetUserId();
    var result = await eventService.PurchaseTicketAsync(id, userId, request);
    
    return result.Success 
        ? Results.Ok(result.Ticket)
        : Results.BadRequest(result.Error);
})
.RequireAuthorization()
.WithName("PurchaseTicket")
.Produces<TicketDto>(200)
.Produces<ProblemDetails>(400);

// Get event with RSVP/ticket status
app.MapGet("/api/events/{id}/attendance", async (
    Guid id,
    IEventService eventService,
    HttpContext context) =>
{
    var userId = context.GetUserId();
    var result = await eventService.GetAttendanceStatusAsync(id, userId);
    
    return result.Success 
        ? Results.Ok(result.Status)
        : Results.NotFound(result.Error);
})
.RequireAuthorization()
.WithName("GetAttendanceStatus")
.Produces<AttendanceStatusDto>(200);
```

### DTOs

```csharp
// src/WitchCityRope.Core/DTOs/EventDTOs.cs

public record RSVPDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTime EventDate,
    string ConfirmationCode,
    RSVPStatus Status,
    DateTime CreatedAt
);

public record TicketDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTime EventDate,
    string ConfirmationCode,
    decimal Price,
    PaymentStatus PaymentStatus,
    DateTime PurchasedAt
);

public record AttendanceStatusDto(
    bool HasRSVP,
    bool HasTicket,
    RSVPDto? RSVP,
    TicketDto? Ticket,
    bool CanPurchaseTicket,
    bool CanRSVP
);
```

## Part 4: Service Implementation

```csharp
// src/WitchCityRope.Api/Features/Events/Services/EventService.cs

public async Task<(bool Success, RSVPDto? RSVP, string? Error)> CreateRSVPAsync(
    Guid eventId, 
    Guid userId, 
    RSVPRequest request)
{
    var eventEntity = await _context.Events
        .Include(e => e.RSVPs)
        .FirstOrDefaultAsync(e => e.Id == eventId);
    
    if (eventEntity == null)
        return (false, null, "Event not found");
    
    if (!eventEntity.AllowsRSVP)
        return (false, null, "This event does not allow RSVP. Please purchase a ticket.");
    
    // Check for existing RSVP
    var existingRSVP = await _context.RSVPs
        .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RSVPStatus.Cancelled);
    
    if (existingRSVP != null)
        return (false, null, "You have already RSVP'd for this event");
    
    var user = await _context.Users.FindAsync(userId);
    if (user == null)
        return (false, null, "User not found");
    
    var rsvp = new RSVP(user, eventEntity, request.DietaryRestrictions);
    
    _context.RSVPs.Add(rsvp);
    await _context.SaveChangesAsync();
    
    var dto = new RSVPDto(
        rsvp.Id,
        rsvp.EventId,
        eventEntity.Title,
        eventEntity.StartDate,
        rsvp.ConfirmationCode,
        rsvp.Status,
        rsvp.CreatedAt
    );
    
    return (true, dto, null);
}
```

## Part 5: Frontend Updates

```typescript
// apps/web/src/api/events/eventsApi.ts

export const eventsApi = {
  // RSVP for free (social events only)
  createRSVP: async (eventId: string, data: RSVPRequest): Promise<RSVPResponse> => {
    const response = await fetch(`${API_BASE_URL}/events/${eventId}/rsvp`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data)
    });
    
    if (!response.ok) throw new Error('Failed to RSVP');
    return response.json();
  },
  
  // Purchase ticket (all events)
  purchaseTicket: async (eventId: string, data: TicketPurchaseRequest): Promise<TicketResponse> => {
    const response = await fetch(`${API_BASE_URL}/events/${eventId}/tickets`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data)
    });
    
    if (!response.ok) throw new Error('Failed to purchase ticket');
    return response.json();
  },
  
  // Check attendance status
  getAttendanceStatus: async (eventId: string): Promise<AttendanceStatus> => {
    const response = await fetch(`${API_BASE_URL}/events/${eventId}/attendance`, {
      credentials: 'include'
    });
    
    if (!response.ok) throw new Error('Failed to get attendance status');
    return response.json();
  }
};
```

## Part 6: Testing

```csharp
// tests/WitchCityRope.Core.Tests/Entities/RSVPTests.cs

[Fact]
public void RSVP_Should_Only_Be_Created_For_Social_Events()
{
    // Arrange
    var user = new User("test@example.com");
    var classEvent = new Event("Rope Class", EventType.Class);
    var socialEvent = new Event("Social Gathering", EventType.Social);
    
    // Act & Assert
    Assert.Throws<DomainException>(() => new RSVP(user, classEvent));
    
    var rsvp = new RSVP(user, socialEvent);
    Assert.NotNull(rsvp);
    Assert.StartsWith("RSVP-", rsvp.ConfirmationCode);
}

[Fact]
public void Registration_Should_Allow_Free_Confirmation_For_Social_Events()
{
    // Arrange
    var user = new User("test@example.com");
    var socialEvent = new Event("Social", EventType.Social);
    var registration = new Registration(user, socialEvent, Money.Zero);
    
    // Act
    registration.ConfirmAsRSVP();
    
    // Assert
    Assert.Equal(RegistrationStatus.Confirmed, registration.Status);
    Assert.True(registration.IsFreeRSVP);
    Assert.Null(registration.Payment);
}
```

## Implementation Checklist

- [ ] Create RSVP entity
- [ ] Update Event entity with capacity calculations
- [ ] Create database migration
- [ ] Implement API endpoints
- [ ] Update EventService
- [ ] Update frontend API client
- [ ] Update React components to use correct endpoints
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Update documentation

## Timeline

1. **Day 1**: Backend domain model and migrations (4 hours)
2. **Day 2**: API endpoints and services (4 hours)
3. **Day 3**: Frontend integration (4 hours)
4. **Day 4**: Testing and documentation (4 hours)

Total: 16 hours of work

## Success Criteria

- [ ] Social events show both RSVP and Purchase Ticket buttons
- [ ] Users can RSVP for free to social events
- [ ] Users can purchase tickets for any event
- [ ] Users with RSVP can still purchase tickets
- [ ] Classes only show Purchase Ticket button
- [ ] Capacity counts both RSVPs and tickets
- [ ] Different confirmation codes for RSVP vs tickets