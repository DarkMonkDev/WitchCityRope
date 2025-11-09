# Comprehensive Code Analysis: Sold Count & Capacity System
**Analysis Date**: 2025-11-08  
**Project**: WitchCityRope - Event Management System  
**Thoroughness**: Very Thorough - Complete code review with exact line numbers

---

## Executive Summary

The current implementation uses a **dual data model** for tracking sold counts and capacity:

1. **Event Capacity**: Based on `EventParticipation` records (RSVPs + Tickets)
2. **Session Capacity**: Based on `Session.CurrentAttendees` (calculated from ticket sales)
3. **Ticket Type Sold Count**: **CALCULATED DYNAMICALLY** from `TicketPurchase` records, NOT stored in database
4. **Cancellation Behavior**: When participations are cancelled, sold counts are automatically recalculated (no explicit decrement needed)

**Critical Finding**: The system does NOT store a `Sold` column on `TicketType` entity - it's calculated each time from actual ticket purchase records.

---

## Part 1: Frontend Display Components

### 1.1 Capacity Display Component
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CapacityDisplay.tsx`  
**Lines**: 1-57

**Current Property**: Displays `current` and `max` capacity values  
**Display Format**: `{current}/{max}` with progress bar

```tsx
// Lines 28-34: Color logic
const percentage = maxValue > 0 ? (currentValue / maxValue) * 100 : 0;
const getColor = () => {
  if (percentage >= 80) return 'green';   // High capacity = positive (nearly sold out!)
  if (percentage >= 50) return 'yellow';  // Moderate capacity = okay
  return 'red';                           // Low capacity = concerning (needs more signups)
};
```

**Component Behavior**:
- Shows "Capacity TBD" if max is 0 or both are undefined
- Progress bar turns green at 80%+ (high attendance is positive)
- Displays as `{currentValue}/{maxValue}` (e.g., "42/50")

---

### 1.2 Session Grid Component (Admin Panel)
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventSessionsGrid.tsx`  
**Lines**: 1-162

**Current Properties Used**:
- `session.registrationCount` (Line 131) - Number of registered attendees
- `session.capacity` (Line 125) - Max capacity

```tsx
// Lines 42-52: Sold display logic
const getSoldDisplay = (sold?: number, capacity?: number) => {
  if (!sold && sold !== 0) return { text: '0', color: 'inherit' };
  if (!capacity) return { text: sold.toString(), color: 'inherit' };
  
  const percentage = (sold / capacity) * 100;
  
  if (percentage === 100) {
    return { text: `${sold} - Sold Out`, color: 'var(--mantine-color-red-6)' };
  }
  return { text: sold.toString(), color: 'inherit' };
};
```

**Display**: Shows registration count in "Sold" column (Line 128-135)

---

### 1.3 Ticket Types Grid Component (Admin Panel)
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventTicketTypesGrid.tsx`  
**Lines**: 1-191

**Current Properties Used**:
- `ticketType.quantitySold` (Line 156) - Number sold
- `ticketType.quantityAvailable` (Line 151) - Total available

```tsx
// Line 156: Display sold count
<Text size="sm" fw={700}>
  {ticketType.quantitySold ?? 0}
</Text>
```

---

### 1.4 TypeScript Type Definitions

**File**: `/home/chad/repos/witchcityrope/apps/web/src/lib/api/types/event-session-matrix.types.ts`

**EventSessionDto** (Lines 12-25):
```typescript
export interface EventSessionDto {
  id: string;
  eventId: string;
  sessionIdentifier: string;
  name: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  registrationCount: number;  // ← Frontend expects this field
  isRequired: boolean;
  createdAt: string;
  updatedAt: string;
}
```

**EventTicketTypeDto** (Lines 28-47):
```typescript
export interface EventTicketTypeDto {
  id: string;
  eventId: string;
  name: string;
  description?: string;
  pricingType: components["schemas"]["PricingType"];
  price?: number;
  minPrice?: number;
  maxPrice?: number;
  defaultPrice?: number;
  quantityAvailable?: number;
  quantitySold: number;  // ← Frontend expects this field
  sessionIdentifiers: string[];
  createdAt: string;
  updatedAt: string;
}
```

---

## Part 2: Backend Data Models

### 2.1 TicketType Entity Model
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/TicketType.cs`  
**Lines**: 1-113

**Key Properties**:
```csharp
public Guid Id { get; set; }
public Guid EventId { get; set; }
public Guid? SessionId { get; set; }              // Null for multi-session tickets
public string Name { get; set; }
public PricingType PricingType { get; set; }
public decimal? Price { get; set; }
public decimal? MinPrice { get; set; }
public decimal? MaxPrice { get; set; }
public decimal? DefaultPrice { get; set; }
public int Available { get; set; }                // Total available quantity
public int Sold { get; set; } = 0;               // ← Stored in DB but NOT USED for calculations
public ICollection<TicketPurchase> Purchases { get; set; }

// Line 107: Calculated property (NOT persisted)
public int Remaining => Available - Sold;

// Line 112: Calculated property
public bool IsSoldOut => Sold >= Available;
```

**CRITICAL FINDING**: The `Sold` property is stored in the database but is **NOT actively maintained** during ticket operations. Instead, `QuantitySold` is calculated dynamically from actual `TicketPurchase` records.

---

### 2.2 Session Entity Model
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs`  
**Lines**: 1-79

**Key Properties**:
```csharp
public Guid Id { get; set; }
public Guid EventId { get; set; }
public string SessionCode { get; set; }          // "S1", "S2", etc.
public string Name { get; set; }
public DateTime StartTime { get; set; }
public DateTime EndTime { get; set; }
public int Capacity { get; set; }                // Max capacity
public int CurrentAttendees { get; set; } = 0;  // ← Updated dynamically

public ICollection<TicketType> TicketTypes { get; set; }
```

**CurrentAttendees**: Updated in `EventService.GetEventAsync()` (lines 158-174)

---

### 2.3 Event Entity Model - Capacity Calculations
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`  
**Lines**: 1-195

**CRITICAL METHODS**:

#### GetCurrentAttendeeCount() (Lines 139-151)
```csharp
public int GetCurrentAttendeeCount()
{
    if (EventType == Enums.EventType.Social)
    {
        // Social events: Attendees = RSVPs (primary attendance metric)
        return GetCurrentRSVPCount();
    }
    else // Class
    {
        // Class events: Attendees = Tickets (only paid tickets)
        return GetCurrentTicketCount();
    }
}
```

#### GetCurrentRSVPCount() (Lines 159-174)
```csharp
public int GetCurrentRSVPCount()
{
    // Only Social events have RSVPs
    if (EventType != Enums.EventType.Social) return 0;
    
    // Count active RSVP participations if navigation property is loaded
    if (EventParticipations?.Any() == true)
    {
        return EventParticipations.Count(ep =>
            ep.ParticipationType == ParticipationType.RSVP &&
            ep.Status == ParticipationStatus.Active);  // ← Only active
    }
    
    return 0;
}
```

#### GetCurrentTicketCount() (Lines 182-194)
```csharp
public int GetCurrentTicketCount()
{
    // Count active ticket participations if navigation property is loaded
    if (EventParticipations?.Any() == true)
    {
        return EventParticipations.Count(ep =>
            ep.ParticipationType == ParticipationType.Ticket &&
            ep.Status == ParticipationStatus.Active);  // ← Only active
    }
    
    return 0;
}
```

**KEY INSIGHT**: Both count methods ONLY count `Active` participations. Cancelled/Refunded participations are automatically excluded.

---

### 2.4 EventParticipation Entity
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventParticipation.cs`  
**Lines**: 1-134

**Key Properties**:
```csharp
public Guid Id { get; set; }
public Guid EventId { get; set; }
public Guid UserId { get; set; }
public ParticipationType ParticipationType { get; set; }  // RSVP or Ticket
public ParticipationStatus Status { get; set; }          // Active, Cancelled, Refunded, Waitlisted
public DateTime CreatedAt { get; set; }
public DateTime? CancelledAt { get; set; }
public string? CancellationReason { get; set; }
public string? Notes { get; set; }
public string Metadata { get; set; }

// Line 124: Cancellation method
public void Cancel(string? reason = null)
{
    if (!CanBeCancelled())
        throw new InvalidOperationException("Participation cannot be cancelled in current status");
    
    Status = ParticipationStatus.Cancelled;
    CancelledAt = DateTime.UtcNow;
    CancellationReason = reason;
    UpdatedAt = DateTime.UtcNow;
}
```

---

### 2.5 ParticipationType & ParticipationStatus Enums
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/ParticipationType.cs`
```csharp
public enum ParticipationType
{
    RSVP = 1,      // Social events: free attendance
    Ticket = 2     // Class events: paid attendance
}
```

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/ParticipationStatus.cs`
```csharp
public enum ParticipationStatus
{
    Active = 1,
    Cancelled = 2,
    Refunded = 3,
    Waitlisted = 4
}
```

---

### 2.6 TicketPurchase Entity
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`  
**Lines**: 1-117

**Key Properties**:
```csharp
public Guid Id { get; set; }
public Guid TicketTypeId { get; set; }
public Guid UserId { get; set; }
public DateTime PurchaseDate { get; set; }
public int Quantity { get; set; } = 1;
public decimal TotalPrice { get; set; }
public string PaymentStatus { get; set; }        // "Pending", "Completed", "Confirmed"
public string PaymentMethod { get; set; }
public string PaymentReference { get; set; }
public string Notes { get; set; }
public Guid? RecordedByStaffId { get; set; }     // For door cash purchases
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }

// Line 106: Helper property
public bool IsPaymentCompleted => PaymentStatus == "Completed" || PaymentStatus == "Confirmed";

// Line 111: Helper property
public bool IsRSVP => TotalPrice == 0;

// Line 116: Helper property
public bool IsDoorPurchase => RecordedByStaffId.HasValue;
```

**NOTE**: This entity tracks COMPLETED purchases. The count of unique users with completed purchases = sold count.

---

## Part 3: DTO Transformation & Calculation Logic

### 3.1 TicketTypeDto Constructor - QuantitySold Calculation
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/TicketTypeDto.cs`  
**Lines**: 71-153

**CRITICAL IMPLEMENTATION** (Lines 97-130):
```csharp
// Calculate QuantitySold dynamically from actual ticket purchases (not stored Sold column)
// Business Rule: QuantitySold = count of unique registered attendees with active participations
// NOT total quantity across all purchases (one user buying multiple tickets counts as 1 sold)
// CRITICAL: Exclude cancelled/refunded tickets by checking EventParticipation.Status
// Only count Active participations (status = 1), exclude Cancelled (2), Refunded (3), Waitlisted (4)

if (eventParticipations != null)
{
    // When event participations are provided, count unique users with active participations
    // who have completed at least one payment for this ticket type
    var participationLookup = eventParticipations
        .Where(ep => ep.Status == WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active)
        .Select(ep => ep.UserId)
        .ToHashSet();
    
    // Count unique users with completed purchases, not total quantity
    // This represents actual registered attendees, not total tickets bought
    QuantitySold = ticketType.Purchases
        .Where(p =>
            p.IsPaymentCompleted &&
            participationLookup.Contains(p.UserId))
        .Select(p => p.UserId)
        .Distinct()
        .Count();
}
else
{
    // Fallback: If no participations provided, count unique users with completed payments
    // Changed from Sum(p.Quantity) to match business logic of counting unique attendees
    QuantitySold = ticketType.Purchases
        .Where(p => p.IsPaymentCompleted)
        .Select(p => p.UserId)
        .Distinct()
        .Count();
}
```

**KEY INSIGHTS**:
- **NOT stored in database** - calculated on every API call
- **Counts UNIQUE USERS**, not total quantity
- **Excludes cancelled/refunded participations** by checking EventParticipation.Status
- **Requires EventParticipation collection to be loaded** for accurate calculation

---

### 3.2 SessionDto Constructor
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/SessionDto.cs`  
**Lines**: 53-63

```csharp
public SessionDto(WitchCityRope.Api.Models.Session session)
{
    Id = session.Id.ToString();
    SessionIdentifier = session.SessionCode;
    Name = session.Name;
    Date = session.StartTime.Date;
    StartTime = session.StartTime;
    EndTime = session.EndTime;
    Capacity = session.Capacity;
    RegistrationCount = session.CurrentAttendees;  // ← Mapped from Session.CurrentAttendees
}
```

---

## Part 4: Backend Service Logic

### 4.1 EventService - GetEventsAsync()
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`  
**Lines**: 27-111

**Data Loading** (Lines 45-59):
```csharp
IQueryable<WitchCityRope.Api.Models.Event> query = _context.Events
    .AsNoTracking() // Read-only for better performance
    .Include(e => e.Sessions)
    .Include(e => e.TicketTypes)
        .ThenInclude(tt => tt.Session)
    .Include(e => e.TicketTypes)
        .ThenInclude(tt => tt.Purchases)        // ← Include purchases for QuantitySold
            .ThenInclude(p => p.User)
    .Include(e => e.VolunteerPositions)
    .Include(e => e.Organizers)
    .Include(e => e.EventParticipations);       // ← Include for capacity & sold count calculations
```

**DTO Mapping** (Lines 80-101):
```csharp
var eventDtos = events.Select(e => new EventDto
{
    // ... other fields ...
    RegistrationCount = e.GetCurrentAttendeeCount(),   // Calls Event method
    CurrentRSVPs = e.GetCurrentRSVPCount(),
    CurrentTickets = e.GetCurrentTicketCount(),
    Sessions = e.Sessions.Select(s => new SessionDto(s)).ToList(),
    TicketTypes = e.TicketTypes.Select(tt => new TicketTypeDto(tt, e.EventParticipations)).ToList(),
    // Note: EventParticipations passed to TicketTypeDto constructor for sold count calculation
    // ... other fields ...
}).ToList();
```

---

### 4.2 EventService - GetEventAsync(Single Event)
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`  
**Lines**: 116-207

**Critical Session Capacity Update** (Lines 151-174):
```csharp
// Calculate CurrentAttendees for each session from actual ticket purchases
var activeUserIds = eventEntity.EventParticipations
    .Where(ep => ep.Status == WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active)
    .Select(ep => ep.UserId)
    .ToHashSet();

foreach (var session in eventEntity.Sessions)
{
    // Count completed ticket purchases for this session
    // For single-session tickets: TicketType.SessionId == session.Id
    // For multi-session tickets: Would need additional logic (not implemented yet)
    // CRITICAL: Exclude cancelled/refunded tickets by checking EventParticipation.Status
    var ticketsSold = eventEntity.TicketTypes
        .Where(tt => tt.SessionId == session.Id)
        .SelectMany(tt => tt.Purchases)
        .Where(p =>
            p.IsPaymentCompleted &&
            activeUserIds.Contains(p.UserId))
        .Sum(p => p.Quantity);
    
    session.CurrentAttendees = ticketsSold;
}
```

**KEY INSIGHT**: `Session.CurrentAttendees` is **UPDATED IN MEMORY** during API response generation, not persisted to database.

---

### 4.3 ParticipationService - CreateRSVPAsync()
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/ParticipationService.cs`  
**Lines**: 165-326

**Key Validation** (Lines 222-228):
```csharp
// Check event capacity
var currentParticipationCount = await _context.EventParticipations
    .CountAsync(ep => ep.EventId == request.EventId && ep.Status == ParticipationStatus.Active, cancellationToken);

if (currentParticipationCount >= eventEntity.Capacity)
{
    return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
}
```

**Creation** (Lines 231-237):
```csharp
var participation = new EventParticipation(request.EventId, userId, ParticipationType.RSVP)
{
    Notes = request.Notes,
    CreatedBy = userId
};

_context.EventParticipations.Add(participation);
// ... audit history and EventAttendee creation ...
await _context.SaveChangesAsync(cancellationToken);
```

**NOTE**: No direct modification of `TicketType.Sold` column.

---

### 4.4 ParticipationService - CreateTicketPurchaseAsync()
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/ParticipationService.cs`  
**Lines**: 328-544

**Key Validation** (Lines 388-394):
```csharp
// Check event capacity
var currentParticipationCount = await _context.EventParticipations
    .CountAsync(ep => ep.EventId == request.EventId && ep.Status == ParticipationStatus.Active, cancellationToken);

if (currentParticipationCount >= eventEntity.Capacity)
{
    return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
}
```

**Auto-RSVP for Social Events** (Lines 464-505):
```csharp
// BUSINESS RULE: Auto-RSVP for social events when purchasing a ticket
// If this is a social event and user doesn't already have an RSVP, create one automatically
if (eventEntity.EventType == EventType.Social)
{
    var existingRsvp = await _context.EventParticipations
        .FirstOrDefaultAsync(ep =>
            ep.EventId == request.EventId &&
            ep.UserId == userId &&
            ep.Status == ParticipationStatus.Active &&
            ep.ParticipationType == ParticipationType.RSVP,
            cancellationToken);
    
    if (existingRsvp == null)
    {
        // Auto-create RSVP
        var autoRsvp = new EventParticipation(request.EventId, userId, ParticipationType.RSVP)
        {
            Notes = "Auto-created RSVP from ticket purchase",
            CreatedBy = userId
        };
        
        _context.EventParticipations.Add(autoRsvp);
    }
}
```

---

### 4.5 ParticipationService - CancelParticipationAsync()
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/ParticipationService.cs`  
**Lines**: 549-794

**Cancellation Logic** (Lines 628-679):
```csharp
// Cancel the participation
participation.Cancel(reason);
participation.UpdatedBy = userId;
_context.EventParticipations.Update(participation);

// Create audit history
var history = new ParticipationHistory(participation.Id, "Cancelled")
{
    OldValues = oldValues,
    NewValues = System.Text.Json.JsonSerializer.Serialize(new
    {
        Status = participation.Status,
        CancelledAt = participation.CancelledAt,
        CancellationReason = participation.CancellationReason
    }),
    ChangedBy = userId,
    ChangeReason = reason ?? "Cancelled by user"
};

_context.ParticipationHistory.Add(history);

// Cancel associated RSVP if exists (lines 651-679)
if (associatedRsvp != null)
{
    // Also cancel RSVP when ticket is cancelled
    associatedRsvp.Cancel("Auto-cancelled when ticket was cancelled");
    // ...
}
```

**CRITICAL**: No explicit `TicketType.Sold` decrement. The sold count is recalculated on next API call based on active participations.

**EventAttendee Status Update** (Lines 681-719):
```csharp
// Check if user has any remaining ACTIVE participations after this cancellation
var remainingActiveParticipations = await _context.EventParticipations
    .Where(ep => ep.EventId == eventId &&
                ep.UserId == userId &&
                ep.Status == ParticipationStatus.Active &&
                ep.Id != participation.Id &&
                (associatedRsvp == null || ep.Id != associatedRsvp.Id))
    .AnyAsync(cancellationToken);

// If no active participations remain, update EventAttendee to "cancelled" status
if (!remainingActiveParticipations)
{
    var eventAttendee = await _context.EventAttendees
        .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);
    
    if (eventAttendee != null)
    {
        eventAttendee.RegistrationStatus = "cancelled";
        eventAttendee.UpdatedAt = DateTime.UtcNow;
        _context.EventAttendees.Update(eventAttendee);
    }
}
```

---

## Part 5: Data Flow Diagrams

### 5.1 Ticket Purchase Flow
```
User purchases ticket
    ↓
ParticipationService.CreateTicketPurchaseAsync()
    ↓
Create EventParticipation (Status: Active, Type: Ticket)
    ↓
(If Social Event) Auto-create RSVP EventParticipation
    ↓
SaveChangesAsync()
    ↓
[Next API Call]
EventService.GetEventAsync() loads:
  - EventParticipations (filtered to Active)
  - TicketTypes with Purchases collections
    ↓
TicketTypeDto constructor calculates QuantitySold:
  - Count unique users with Active participation
  - Who have IsPaymentCompleted purchases
    ↓
Response includes calculated quantitySold
```

### 5.2 Cancellation Flow
```
User cancels participation
    ↓
ParticipationService.CancelParticipationAsync()
    ↓
Find EventParticipation (Status: Active)
    ↓
Call participation.Cancel(reason)
  ↓ Sets Status = Cancelled
  ↓ Sets CancelledAt = Now
    ↓
Save to database
    ↓
[Next API Call]
EventService.GetEventAsync() loads:
  - EventParticipations (filtered to Active ONLY)
  - TicketTypes with Purchases collections
    ↓
TicketTypeDto constructor:
  - Filters participations to Active only
  - Cancelled participation is EXCLUDED automatically
  - QuantitySold recalculated with fewer active users
    ↓
Response shows UPDATED quantitySold (minus cancelled)
```

### 5.3 Session Capacity Calculation Flow
```
EventService.GetEventAsync() called
    ↓
Load Event with Sessions and TicketTypes
    ↓
For each session:
    ↓
  Find all TicketTypes where SessionId = this.session.Id
    ↓
  For each TicketType:
    ↓
    Get all Purchases
      ↓ Filter: IsPaymentCompleted = true
      ↓ Filter: UserId in Active participations only
      ↓ Sum quantities
    ↓
  session.CurrentAttendees = total tickets sold
    ↓
Return Session.CurrentAttendees in SessionDto
```

---

## Part 6: All Display Locations Where Sold Counts Appear

### 6.1 Admin Event Details Page
**Component**: `EventSessionsGrid.tsx`  
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventSessionsGrid.tsx`  
**Data Field**: `session.registrationCount`  
**Display Location**: "Sold" column (Lines 128-135)  
**Calculation**: From `Session.CurrentAttendees`

### 6.2 Admin Ticket Types Section
**Component**: `EventTicketTypesGrid.tsx`  
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventTicketTypesGrid.tsx`  
**Data Field**: `ticketType.quantitySold`  
**Display Location**: "Sold" column (Lines 154-158)  
**Calculation**: From `TicketTypeDto` constructor

### 6.3 Capacity Display Component
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CapacityDisplay.tsx`  
**Data Fields**: `current` and `max`  
**Display Location**: Progress bar with count (Lines 36-55)  
**Calculation**: From `Event.RegistrationCount` and `Event.Capacity`

---

## Part 7: Exact Calculations Currently Used

### 7.1 Event RegistrationCount Calculation
**Source**: `Event.cs` methods (Lines 139-194)

```
IF EventType == Social:
  RegistrationCount = COUNT(EventParticipation 
    WHERE EventId = this.Id 
    AND ParticipationType = RSVP 
    AND Status = Active)
ELSE (Class events):
  RegistrationCount = COUNT(EventParticipation 
    WHERE EventId = this.Id 
    AND ParticipationType = Ticket 
    AND Status = Active)
```

### 7.2 Session CurrentAttendees Calculation
**Source**: `EventService.GetEventAsync()` (Lines 158-174)

```
FOR each session:
  ticketsSold = SUM(TicketPurchase.Quantity)
    WHERE TicketType.SessionId = session.Id
    AND TicketPurchase.IsPaymentCompleted = true
    AND TicketPurchase.UserId IN (Active EventParticipation Users)
  
  session.CurrentAttendees = ticketsSold
```

### 7.3 TicketType QuantitySold Calculation
**Source**: `TicketTypeDto.cs` constructor (Lines 97-130)

```
IF eventParticipations provided:
  activeUsers = DISTINCT(EventParticipation.UserId 
    WHERE Status = Active)
  
  QuantitySold = COUNT(DISTINCT TicketPurchase.UserId)
    WHERE TicketPurchase.TicketTypeId = this.Id
    AND TicketPurchase.IsPaymentCompleted = true
    AND TicketPurchase.UserId IN activeUsers
ELSE:
  QuantitySold = COUNT(DISTINCT TicketPurchase.UserId)
    WHERE TicketPurchase.TicketTypeId = this.Id
    AND TicketPurchase.IsPaymentCompleted = true
```

---

## Part 8: Database Persistence Analysis

### 8.1 What IS Stored in Database
- `TicketType.Available` - Total quantity available
- `TicketType.Sold` - **Stored but NOT maintained** (legacy field)
- `Session.CurrentAttendees` - **Stored but overwritten on each API call**
- `EventParticipation.Status` - Tracks Active/Cancelled/Refunded/Waitlisted
- `TicketPurchase` records - All individual purchases with PaymentStatus

### 8.2 What IS NOT Stored (Calculated Instead)
- `TicketTypeDto.QuantitySold` - **ALWAYS calculated dynamically**
- `SessionDto.RegistrationCount` - **Mapped from Session.CurrentAttendees** (which is calculated in memory)
- `EventDto.RegistrationCount` - **ALWAYS calculated from EventParticipation count**

### 8.3 No Update Triggers Found
**Grep Search Result**: No updates to `TicketType.Sold` column found in ParticipationService

The migration file `20251109013502_FixSoldCountExcludeCancelledTickets.cs` exists but has empty Up() and Down() methods, indicating this is a planned but not yet implemented change.

---

## Part 9: Gaps & Issues Found

### 9.1 CRITICAL ISSUE: Disconnected Database Fields
**Status**: Confirmed Issue

**Problem**: 
- `TicketType.Sold` column exists in database and is initialized in migrations
- BUT it is **NEVER updated** by any service code
- QuantitySold is instead calculated dynamically from `TicketPurchase` records
- The stored `Sold` value becomes stale and inaccurate immediately

**Evidence**:
- No calls to update `TicketType.Sold` in ParticipationService (confirmed via grep)
- TicketTypeDto constructor explicitly recalculates from purchases (Line 113-119)
- Comments indicate this is intentional (Line 97-98: "not stored Sold column")

**Impact**:
- If anyone queries `TicketType.Sold` directly (via database or raw queries), they get wrong numbers
- Frontend is safe because it uses the DTO which recalculates
- But raw database queries would return stale data

**Recommendation**: Either 
1. Remove the `Sold` column from database (preferred)
2. Or actively maintain it during ticket operations

---

### 9.2 ISSUE: Session.CurrentAttendees Not Persisted
**Status**: Confirmed Issue

**Problem**:
- `Session.CurrentAttendees` is calculated in-memory in `EventService.GetEventAsync()`
- The calculation modifies the entity but doesn't persist it back to database
- Next API call recalculates from scratch

**Evidence**:
- Lines 158-174 in EventService calculate and assign to `session.CurrentAttendees`
- `query.AsNoTracking()` is used, so changes aren't tracked
- No `SaveChangesAsync()` call follows the calculation

**Impact**:
- Any direct database queries for Session.CurrentAttendees get the OLD stale value
- API responses are always current (good)
- But creates two sources of truth

**Recommendation**: Either
1. Persist the calculation back to database after each event query
2. Or remove the column and always calculate on-demand (preferred for accuracy)

---

### 9.3 ISSUE: Multi-Session Ticket Capacity Not Fully Implemented
**Status**: Confirmed Gap

**Problem**:
- Code comment in EventService line 162: "For multi-session tickets: Would need additional logic (not implemented yet)"
- A ticket type can include multiple sessions (multi-session packages)
- But the capacity calculation only handles single-session tickets

**Evidence**:
```csharp
// Line 165-166: Only handles SessionId != null
var ticketsSold = eventEntity.TicketTypes
    .Where(tt => tt.SessionId == session.Id)  // ← Only matches single-session
```

**Impact**:
- Multi-session tickets don't count toward individual session capacities
- This could allow overselling of multi-session packages

**Recommendation**: Implement logic to allocate multi-session ticket quantities to all included sessions

---

### 9.4 ISSUE: No Explicit Capacity Enforcement at TicketType Level
**Status**: Confirmed Gap

**Problem**:
- `TicketType.Available` and `TicketType.Sold` exist but aren't enforced
- Capacity checks only happen at Event level (line 388-394 in ParticipationService)
- Could theoretically allow selling more tickets than `TicketType.Available`

**Evidence**:
- CreateTicketPurchaseAsync checks Event.Capacity only (line 388)
- No check against TicketType.Available
- No check against TicketType.Sold vs Available

**Impact**:
- Can oversell individual ticket types
- Appears to rely on `TicketType.Available` being pre-configured correctly

**Recommendation**: Add validation to prevent selling beyond `TicketType.Available`

---

### 9.5 ISSUE: Inconsistent Sold Count Meaning
**Status**: Confirmed Design Issue

**Problem**:
- `Session.CurrentAttendees` = Sum of ticket QUANTITIES
- `TicketTypeDto.QuantitySold` = Count of UNIQUE USERS
- These are different metrics but both called "sold"

**Evidence**:
- Session calculation (Line 171): `.Sum(p => p.Quantity)` - sums quantities
- TicketTypeDto calculation (Line 119): `.Distinct().Count()` - counts unique users

**Impact**:
- Confusing for administrators
- If user buys 2 tickets: Session shows "+2" but TicketType shows "+1"
- Makes capacity projections inaccurate

**Example**:
```
Event with 2 sessions, 20 capacity each
Ticket Type A: All 2 Days (multi-session)

User 1 buys 1 ticket for both days
Session 1 CurrentAttendees: 1
Session 2 CurrentAttendees: 1
TicketType QuantitySold: 1

User 2 buys 2 tickets for both days
Session 1 CurrentAttendees: 3 (1+2)
Session 2 CurrentAttendees: 3 (1+2)
TicketType QuantitySold: 2
```

**Recommendation**: Decide and implement consistently:
- Option A: Both count unique users
- Option B: Both sum quantities
- Option C: Keep separate but rename (e.g., "QuantityOfUsers" vs "TicketQuantityPurchased")

---

### 9.6 ISSUE: Capacity Checks Don't Consider Cancelled-Then-Repurchased
**Status**: Minor Issue

**Problem**:
- If user cancels ticket and immediately buys again, they may exceed displayed capacity during the cancellation window

**Impact**: 
- Unlikely in practice due to millisecond timing
- But theoretically possible if capacity is at 100%

**Recommendation**: Add transaction-level locking during purchase/cancel

---

## Part 10: Summary Table - Current Calculation Methods

| Component | Field | Source | Calculation Method | Persisted? | Stale Risk |
|-----------|-------|--------|-------------------|-----------|-----------|
| Event | RegistrationCount | EventParticipation | COUNT(Active participations) | No | Low (calculated on demand) |
| Session | CurrentAttendees | EventParticipation + TicketPurchase | SUM(quantities) for SessionId matches | Yes, but overwritten | High (in-memory calc not saved) |
| TicketType | QuantitySold | TicketPurchase | COUNT(DISTINCT users) with Active participation | No | Low (calculated on demand) |
| TicketType | Sold (DB field) | Database | Not maintained | Yes | Very High (stale immediately) |

---

## Part 11: File Registry Update

**All files analyzed and documented with exact line numbers:**

| File Path | Purpose | Lines Read |
|-----------|---------|-----------|
| `/apps/web/src/components/events/CapacityDisplay.tsx` | Display component for capacity bars | 1-57 |
| `/apps/web/src/components/events/EventSessionsGrid.tsx` | Admin grid showing sessions and sold counts | 1-162 |
| `/apps/web/src/components/events/EventTicketTypesGrid.tsx` | Admin grid showing ticket types and sold counts | 1-191 |
| `/apps/web/src/lib/api/types/event-session-matrix.types.ts` | TypeScript type definitions | 1-200 |
| `/apps/api/Models/TicketType.cs` | TicketType entity model | 1-113 |
| `/apps/api/Models/Session.cs` | Session entity model | 1-79 |
| `/apps/api/Models/Event.cs` | Event entity with capacity calculation methods | 1-195 |
| `/apps/api/Features/Participation/Entities/EventParticipation.cs` | EventParticipation entity | 1-134 |
| `/apps/api/Features/Participation/Entities/ParticipationType.cs` | Enum for participation types | Complete |
| `/apps/api/Features/Participation/Entities/ParticipationStatus.cs` | Enum for participation status | Complete |
| `/apps/api/Models/TicketPurchase.cs` | TicketPurchase entity model | 1-117 |
| `/apps/api/Features/Events/Models/TicketTypeDto.cs` | DTO with QuantitySold calculation | 1-159 |
| `/apps/api/Features/Events/Models/SessionDto.cs` | SessionDTO with RegistrationCount mapping | 1-69 |
| `/apps/api/Features/Events/Services/EventService.cs` | Main service for event queries | 1-207+ |
| `/apps/api/Features/Participation/Services/ParticipationService.cs` | Service for RSVPs and ticket purchases | 1-904 |
| `/apps/api/Data/ApplicationDbContext.cs` | Entity Framework DbContext | 1-100+ |

---

## Conclusion

The current system works correctly for **frontend display** because it calculates sold counts dynamically from actual participation records. However, there are several **database-level inconsistencies** where stored values become stale:

1. `TicketType.Sold` - Never updated, always stale
2. `Session.CurrentAttendees` - Recalculated in memory but not persisted
3. Multi-session ticket capacity not fully allocated

For the **frontend**, the system is working as designed with accurate calculations. For **raw database queries** or **direct lookups**, the data will be stale.

The clean solution would be to either:
- **Option A**: Remove stored Sold/CurrentAttendees columns and always calculate on-demand
- **Option B**: Implement update triggers to keep database values fresh
- **Option C**: Switch to calculated columns/views in PostgreSQL

Currently using **Option A implicitly** (ignoring stored values and recalculating) which works but creates confusion and potential bugs.
