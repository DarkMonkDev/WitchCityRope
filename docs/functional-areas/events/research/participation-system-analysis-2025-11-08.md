# Event Participation, Tickets, and RSVP System Analysis

**Date**: 2025-11-08
**Purpose**: Comprehensive analysis of the event participation and ticketing system architecture
**Status**: Research Complete
**Analyst**: Business Requirements Agent

---

## Executive Summary

The WitchCityRope platform has **TWO PARALLEL SYSTEMS** for tracking event participation:

1. **EventParticipations Table** - Modern unified participation tracking system (RSVPs + Tickets)
2. **TicketPurchases Table** - Legacy payment transaction records

These systems work together but serve different purposes. Understanding their relationship is critical for proper capacity calculation and sold count reporting.

**Critical Business Rules:**
- **Social Events**: Capacity based on RSVP count (tickets optional for donations)
- **Class Events**: Capacity based on Ticket count (RSVPs not used)
- Users can have BOTH RSVP and Ticket for the same social event
- Auto-RSVP created when purchasing ticket for social event

---

## Current Data Model

### 1. EventParticipations Table (PRIMARY SYSTEM)

**Purpose**: Unified tracking of ALL event participation (both RSVPs and tickets)

**Location**: `/apps/api/Features/Participation/Entities/EventParticipation.cs`

```csharp
public class EventParticipation
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ParticipationType ParticipationType { get; set; }  // RSVP or Ticket
    public ParticipationStatus Status { get; set; }           // Active, Cancelled, Refunded, Waitlisted
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public string Metadata { get; set; }  // JSON for payment info, etc.

    // Navigation Properties
    public Event Event { get; set; }
    public ApplicationUser User { get; set; }
    public ICollection<ParticipationHistory> History { get; set; }
}
```

**ParticipationType Enum:**
```csharp
public enum ParticipationType
{
    RSVP = 1,    // Free attendance for social events
    Ticket = 2   // Paid ticket for classes (or donation for social events)
}
```

**ParticipationStatus Enum:**
```csharp
public enum ParticipationStatus
{
    Active = 1,      // Currently participating
    Cancelled = 2,   // User cancelled
    Refunded = 3,    // Payment refunded
    Waitlisted = 4   // On waiting list
}
```

**Key Features:**
- Tracks BOTH RSVPs and ticket purchases in ONE table
- Stores participation status (Active/Cancelled/Refunded)
- Maintains audit trail via ParticipationHistory
- Supports cancellation with reason tracking
- Stores payment metadata in Notes/Metadata JSON fields

---

### 2. TicketPurchases Table (PAYMENT TRACKING)

**Purpose**: Track payment transactions and ticket inventory

**Location**: `/apps/api/Models/TicketPurchase.cs`

```csharp
public class TicketPurchase
{
    public Guid Id { get; set; }
    public Guid TicketTypeId { get; set; }
    public Guid UserId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string PaymentStatus { get; set; }  // Pending, Completed, Confirmed
    public string PaymentMethod { get; set; }
    public string PaymentReference { get; set; }
    public string Notes { get; set; }
    public Guid? RecordedByStaffId { get; set; }  // For door purchases

    // Navigation Properties
    public TicketType? TicketType { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? RecordedByStaff { get; set; }
}
```

**Key Features:**
- Records payment transaction details
- Tracks payment method and external reference (PayPal ID, etc.)
- Supports multiple ticket quantities per purchase
- Tracks door purchases via RecordedByStaffId
- Connected to TicketType for price/capacity management

---

### 3. TicketType Table (TICKET INVENTORY)

**Purpose**: Define ticket types and pricing for events/sessions

**Location**: `/apps/api/Models/TicketType.cs`

```csharp
public class TicketType
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? SessionId { get; set; }  // Null for event-level tickets
    public string Name { get; set; }
    public string Description { get; set; }
    public PricingType PricingType { get; set; }  // Fixed or SlidingScale

    // Pricing fields (vary based on PricingType)
    public decimal? Price { get; set; }           // Fixed price
    public decimal? MinPrice { get; set; }        // Sliding scale minimum
    public decimal? MaxPrice { get; set; }        // Sliding scale maximum
    public decimal? DefaultPrice { get; set; }    // Sliding scale suggestion

    public int Available { get; set; }
    public int Sold { get; set; }  // <-- THIS IS THE KEY FIELD

    public int Remaining => Available - Sold;
    public bool IsSoldOut => Sold >= Available;
}
```

**Key Features:**
- Supports both Fixed and SlidingScale pricing
- Can be event-level OR session-specific
- Tracks Available vs Sold inventory
- Calculated Remaining and IsSoldOut properties

---

### 4. Event Table (CAPACITY TRACKING)

**Purpose**: Event metadata and capacity calculation logic

**Location**: `/apps/api/Models/Event.cs`

```csharp
public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Capacity { get; set; }
    public EventType EventType { get; set; }  // Social or Class

    // Navigation Properties
    public ICollection<Session> Sessions { get; set; }
    public ICollection<TicketType> TicketTypes { get; set; }
    public ICollection<EventParticipation> EventParticipations { get; set; }

    // CRITICAL BUSINESS LOGIC METHODS
    public int GetCurrentAttendeeCount()
    {
        if (EventType == EventType.Social)
        {
            return GetCurrentRSVPCount();  // Social events: capacity = RSVPs
        }
        else // Class
        {
            return GetCurrentTicketCount(); // Class events: capacity = Tickets
        }
    }

    public int GetCurrentRSVPCount()
    {
        if (EventType != EventType.Social) return 0;

        return EventParticipations.Count(ep =>
            ep.ParticipationType == ParticipationType.RSVP &&
            ep.Status == ParticipationStatus.Active);
    }

    public int GetCurrentTicketCount()
    {
        return EventParticipations.Count(ep =>
            ep.ParticipationType == ParticipationType.Ticket &&
            ep.Status == ParticipationStatus.Active);
    }
}
```

**Critical Business Logic:**
- **Social Events**: Current attendees = Active RSVPs (tickets are optional donations)
- **Class Events**: Current attendees = Active Tickets (RSVPs not used)
- Capacity checked against EventParticipations, NOT TicketPurchases

---

## Business Rules Documentation

### Social Events (EventType.Social)

**Participation Model:**
- **RSVP Required**: All attendees MUST have an RSVP (free participation)
- **Tickets Optional**: Can purchase "Suggested Donation" tickets as support
- **Capacity Basis**: Based on RSVP count, NOT ticket count
- **User Can Have**: BOTH RSVP and Ticket simultaneously

**Example Workflow:**
1. User RSVPs for social event (creates EventParticipation with Type=RSVP)
2. User optionally purchases donation ticket (creates EventParticipation with Type=Ticket + TicketPurchase)
3. User now has 2 EventParticipation records (one RSVP, one Ticket)
4. Capacity check: Count only RSVP participations (where ParticipationType=RSVP, Status=Active)

**Seeding Logic** (`ParticipationSeeder.cs` lines 72-152):
```csharp
// Social event example: "Community Rope Jam"
// - 5 RSVPs created (EventParticipation with Type=RSVP)
// - At least half (3) also purchase donation tickets
// - Each donation creates:
//   * EventParticipation (Type=Ticket, Metadata with price)
//   * TicketPurchase (actual payment record)
```

---

### Class Events (EventType.Class)

**Participation Model:**
- **Tickets Required**: Must purchase ticket to attend
- **No RSVPs**: RSVP concept doesn't exist for class events
- **Capacity Basis**: Based on Ticket count
- **User Can Have**: ONE ticket per event (no RSVPs)

**Example Workflow:**
1. User purchases ticket for class (creates EventParticipation with Type=Ticket + TicketPurchase)
2. Capacity check: Count ticket participations (where ParticipationType=Ticket, Status=Active)

**Seeding Logic** (`ParticipationSeeder.cs` lines 154-218):
```csharp
// Class event example: "Suspension Basics"
// - 4 ticket purchases created
// - Each purchase creates:
//   * EventParticipation (Type=Ticket, Metadata with payment amount)
//   * TicketPurchase (payment record)
//   * Updates TicketType.Sold count (lines 226-234)
```

---

### Auto-RSVP for Social Events

**Business Rule**: When user purchases ticket for social event, automatically create RSVP if they don't have one

**Implementation** (`ParticipationService.cs` lines 464-505):
```csharp
// BUSINESS RULE: Auto-RSVP for social events when purchasing a ticket
if (eventEntity.EventType == EventType.Social)
{
    var existingRsvp = await _context.EventParticipations
        .FirstOrDefaultAsync(ep =>
            ep.EventId == request.EventId &&
            ep.UserId == userId &&
            ep.Status == ParticipationStatus.Active &&
            ep.ParticipationType == ParticipationType.RSVP);

    if (existingRsvp == null)
    {
        _logger.LogInformation("Auto-creating RSVP for user {UserId} in social event {EventId}");

        var autoRsvp = new EventParticipation(request.EventId, userId, ParticipationType.RSVP)
        {
            Notes = "Auto-created RSVP from ticket purchase",
            CreatedBy = userId
        };

        _context.EventParticipations.Add(autoRsvp);
    }
}
```

**Why This Exists:**
- Social events require RSVP to attend
- User might go straight to donation purchase
- System ensures they have both participation types

---

## Capacity Calculation Architecture

### Current Implementation

**Location**: `Event.GetCurrentAttendeeCount()` in `/apps/api/Models/Event.cs`

**Social Event Capacity:**
```csharp
SELECT COUNT(*)
FROM EventParticipations
WHERE EventId = @eventId
  AND ParticipationType = 1 (RSVP)
  AND Status = 1 (Active)
```

**Class Event Capacity:**
```csharp
SELECT COUNT(*)
FROM EventParticipations
WHERE EventId = @eventId
  AND ParticipationType = 2 (Ticket)
  AND Status = 1 (Active)
```

**Critical Notes:**
- Capacity checks use `EventParticipations` table, NOT `TicketPurchases`
- Only counts `Status = Active` (excludes Cancelled/Refunded)
- Social events ignore ticket participations for capacity
- Class events don't have RSVP participations

---

### Sold Count vs Attendee Count

**TicketType.Sold** (inventory field):
- Updated when TicketPurchase is created
- Manually incremented in seeder: `ticketType.Sold += ticketPurchase.Quantity`
- Represents "tickets sold" for payment tracking
- **Does NOT automatically sync with EventParticipations**

**Event Capacity Count** (attendance tracking):
- Dynamically calculated from EventParticipations
- Filters by ParticipationType based on event type
- Filters by Status (only Active counted)
- **Source of truth for capacity management**

**POTENTIAL ISSUE**:
- TicketType.Sold is manually updated, not calculated from participations
- Could become out of sync if:
  - Participation cancelled but Sold not decremented
  - Participation created without updating Sold count
  - Bulk operations modify participations

---

## Code Locations: Complete Map

### Entity Definitions

| Entity | Location | Purpose |
|--------|----------|---------|
| EventParticipation | `/apps/api/Features/Participation/Entities/EventParticipation.cs` | Unified participation tracking |
| ParticipationType | `/apps/api/Features/Participation/Entities/ParticipationType.cs` | RSVP vs Ticket enum |
| ParticipationStatus | `/apps/api/Features/Participation/Entities/ParticipationStatus.cs` | Active/Cancelled/Refunded/Waitlisted |
| ParticipationHistory | `/apps/api/Features/Participation/Entities/ParticipationHistory.cs` | Audit trail |
| TicketPurchase | `/apps/api/Models/TicketPurchase.cs` | Payment transaction records |
| TicketType | `/apps/api/Models/TicketType.cs` | Ticket inventory and pricing |
| Event | `/apps/api/Models/Event.cs` | Event metadata + capacity logic |
| Session | `/apps/api/Models/Session.cs` | Multi-session event support |

---

### Service Layer

**ParticipationService** - `/apps/api/Features/Participation/Services/ParticipationService.cs`

Key Methods:
- `GetParticipationStatusAsync` (lines 41-159): Check user's RSVP/ticket status
- `CreateRSVPAsync` (lines 165-326): Create social event RSVP
- `CreateTicketPurchaseAsync` (lines 331-544): Purchase ticket (with auto-RSVP for social events)
- `CancelParticipationAsync` (lines 549-801): Cancel with optional refund
- `GetUserParticipationsAsync` (lines 806-844): User's participation history
- `GetEventParticipationsAsync` (lines 850-904): Admin view of all participations

**Business Logic Highlights:**
- Lines 62-64: Capacity check uses `EventParticipations.Status = Active`
- Lines 222-228: Capacity validation before RSVP creation
- Lines 387-394: Capacity validation before ticket purchase
- Lines 464-505: Auto-RSVP for social event ticket purchases
- Lines 602-618: Auto-cancel associated RSVP when ticket cancelled

---

### Seeding Data

**ParticipationSeeder** - `/apps/api/Services/Seeding/ParticipationSeeder.cs`

Key Methods:
- `SeedEventParticipationsAsync` (lines 47-238): Create participations for all events
- `SeedHistoricalSocialEventRSVPs` (lines 245-285): Historical social event data
- `CreateHistoricalSocialEventParticipationsAsync` (lines 291-450): Helper for historical data

**Seeding Strategy:**
- Social events: Create RSVPs + optional donation tickets (lines 72-152)
- Class events: Create ticket purchases (lines 154-218)
- Updates `TicketType.Sold` after creating purchases (lines 226-234)
- Creates matching `EventAttendee` records for check-in system

---

### Database Configuration

**ApplicationDbContext** - `/apps/api/Data/ApplicationDbContext.cs`

DbSets:
- `DbSet<EventParticipation> EventParticipations` (line 240)
- `DbSet<ParticipationHistory> ParticipationHistory` (line 245)
- `DbSet<TicketPurchase> TicketPurchases` (line 110)
- `DbSet<TicketType> TicketTypes` (line 105)
- `DbSet<Event> Events` (line 90)

Configuration via Fluent API:
- EventParticipation: `EventParticipationConfiguration` (line 1035)
- TicketPurchase: Lines 579-656 (inline configuration)
- TicketType: Lines 524-577
- Event: Lines 357-429

---

## Problems Identified

### 1. "Suspension Basics" Sold Count Mystery

**User Report**: "Suspension Basics event shows incorrect sold count"

**Hypothesis 1: TicketType.Sold Not Synced**
- TicketType.Sold is manually updated in seeder
- If EventParticipation created without updating TicketType.Sold, counts diverge
- Code location: `ParticipationSeeder.cs` lines 226-234

**Hypothesis 2: Different Data Sources**
- Frontend might be querying TicketType.Sold
- Backend capacity checks use EventParticipations count
- These are different numbers if not kept in sync

**Hypothesis 3: Status Filtering**
- EventParticipations counts only Status=Active
- TicketType.Sold might include Cancelled/Refunded tickets
- Seeder doesn't decrement Sold when creating cancelled participations

**Investigation Needed:**
1. Check what "Suspension Basics" shows in database:
   - Query TicketTypes table for Sold count
   - Query EventParticipations for Active ticket count
   - Compare the numbers
2. Check frontend code: Which field is displayed as "sold"?
3. Trace data flow from database to UI

---

### 2. Duplicate Purchases Concern

**User Report**: "What's causing duplicate purchases?"

**Potential Causes:**

**A. Double-Submission Protection Missing**
- No unique constraint preventing same user from creating multiple active tickets
- Check `ParticipationService.CreateTicketPurchaseAsync` lines 374-385
- Validation exists: "User already has a ticket" check
- BUT: Race condition possible if two requests arrive simultaneously

**B. Payment Webhook Duplicates**
- PayPal webhooks could fire multiple times
- If webhook handler creates EventParticipation, could create duplicates
- Need to check PayPal webhook handler code

**C. Frontend Re-Submission**
- User clicks "Buy Ticket" multiple times
- If no UI-level prevention (disabled button, loading state)
- Multiple API requests could create duplicates

**Investigation Needed:**
1. Check for unique index on (EventId, UserId, ParticipationType, Status)
2. Review PayPal webhook integration code
3. Check frontend ticket purchase component

---

### 3. EventParticipations vs TicketPurchases Confusion

**Problem**: Two tables for similar data creates confusion

**Why Both Tables Exist:**

**EventParticipations** (Participation Tracking):
- **Purpose**: Track WHO is attending WHAT
- Supports both free RSVPs and paid tickets
- Includes status (Active/Cancelled/Refunded)
- Used for capacity management
- Audit trail via ParticipationHistory

**TicketPurchases** (Payment Tracking):
- **Purpose**: Track financial transactions
- Records payment method, amount, reference
- Supports door purchases (RecordedByStaffId)
- Used for payment reconciliation
- Connected to TicketType for inventory

**Design Intent**: Separation of concerns
- Participation: Attendance/capacity domain
- Purchases: Payment/financial domain

**Confusion Points:**
- Both have UserId and EventId
- Both track ticket-related data
- Not always clear which to query
- Data must be kept in sync manually

**Better Design?**
- Possible to merge into single table with payment fields nullable
- Or create stronger linkage between Participation and Purchase
- Current design allows: RSVP without payment, future use cases

---

## Questions for Clarification

### Data Model Questions

1. **TicketType.Sold Management:**
   - Should Sold count be calculated from EventParticipations?
   - Or should it remain manually updated field?
   - How to handle decrements when tickets cancelled/refunded?

2. **EventParticipation to TicketPurchase Relationship:**
   - Should there be a foreign key linking them?
   - Or keep them separate (current design)?
   - What happens if TicketPurchase exists without EventParticipation?

3. **Duplicate Prevention:**
   - Should database enforce unique constraint on (EventId, UserId, ParticipationType, Status=Active)?
   - Or rely on application-level validation only?

---

### Business Logic Questions

1. **Social Event Capacity:**
   - Confirmed: Capacity based on RSVP count (tickets don't count)?
   - What if 100 people RSVP and 100 people buy tickets (same people)?
   - Is this working as designed?

2. **Class Event Tickets:**
   - Confirmed: One ticket per user maximum?
   - Can users buy tickets for others (quantity > 1)?
   - How does multi-session event capacity work?

3. **Cancellation Behavior:**
   - When ticket cancelled, should TicketType.Sold decrement?
   - When RSVP cancelled, should capacity become available immediately?
   - What's the refund policy (impacts Status choices)?

---

### Technical Questions

1. **Sold Count Display:**
   - Where in frontend is "sold count" displayed?
   - Which field is being used (TicketType.Sold or EventParticipations count)?
   - Is it event-level or ticket-type-level?

2. **Capacity Calculations:**
   - Are capacity checks using Event.GetCurrentAttendeeCount()?
   - Or direct database queries?
   - Is the navigation property (.EventParticipations) always loaded?

3. **Data Synchronization:**
   - When is TicketType.Sold updated (only on purchase)?
   - Is there background job to sync counts?
   - What triggers updates?

---

## Recommendations for Next Steps

### Immediate Investigation

1. **Database Query** - Check "Suspension Basics" data:
```sql
-- Check event type and capacity
SELECT Id, Title, EventType, Capacity
FROM Events
WHERE Title = 'Suspension Basics';

-- Check TicketType sold count
SELECT tt.Id, tt.Name, tt.Available, tt.Sold, tt.Remaining
FROM TicketTypes tt
JOIN Events e ON tt.EventId = e.Id
WHERE e.Title = 'Suspension Basics';

-- Check EventParticipations actual count
SELECT
    ParticipationType,
    Status,
    COUNT(*) as Count
FROM EventParticipations ep
JOIN Events e ON ep.EventId = e.Id
WHERE e.Title = 'Suspension Basics'
GROUP BY ParticipationType, Status;

-- Check TicketPurchases count
SELECT COUNT(*) as PurchaseCount
FROM TicketPurchases tp
JOIN TicketTypes tt ON tp.TicketTypeId = tt.Id
JOIN Events e ON tt.EventId = e.Id
WHERE e.Title = 'Suspension Basics'
  AND tp.PaymentStatus IN ('Completed', 'Confirmed');
```

2. **Code Trace** - Find frontend sold count display:
- Search for `TicketType.Sold` or `.sold` in React components
- Search for capacity/sold/remaining display logic
- Identify which API endpoint provides data

3. **Service Review** - Validate business logic:
- Confirm ParticipationService correctly updates all related records
- Check for race conditions in ticket purchase flow
- Verify cancellation properly updates all tables

---

### Design Improvements to Consider

1. **Calculated Sold Count**:
```csharp
// Option 1: Make TicketType.Sold a computed property
public int Sold
{
    get
    {
        return Purchases.Count(p =>
            p.PaymentStatus == "Completed" ||
            p.PaymentStatus == "Confirmed");
    }
}

// Option 2: Sync sold count in ParticipationService
private async Task UpdateTicketTypeSoldCount(Guid ticketTypeId)
{
    var soldCount = await _context.EventParticipations
        .CountAsync(ep =>
            ep.ParticipationType == ParticipationType.Ticket &&
            ep.Status == ParticipationStatus.Active &&
            ep.Event.TicketTypes.Any(tt => tt.Id == ticketTypeId));

    var ticketType = await _context.TicketTypes.FindAsync(ticketTypeId);
    ticketType.Sold = soldCount;
    await _context.SaveChangesAsync();
}
```

2. **Unique Constraint**:
```csharp
// In EventParticipationConfiguration
modelBuilder.Entity<EventParticipation>()
    .HasIndex(ep => new { ep.EventId, ep.UserId, ep.ParticipationType, ep.Status })
    .HasFilter("Status = 1") // Only for Active status
    .IsUnique()
    .HasDatabaseName("IX_EventParticipations_UniqueActiveParticipation");
```

3. **Foreign Key Linkage**:
```csharp
// Add to EventParticipation entity
public Guid? TicketPurchaseId { get; set; }
public TicketPurchase? TicketPurchase { get; set; }

// Creates explicit link between participation and payment
// Easier to trace ticket purchases
// Enables cascade delete/update rules
```

---

## Summary: Key Findings

### System Architecture
1. **Dual Table Design**: EventParticipations (attendance) + TicketPurchases (payments)
2. **Participation Types**: RSVP (free) vs Ticket (paid)
3. **Capacity Logic**: Social events use RSVP count, Class events use Ticket count

### Business Rules
1. **Social Events**: RSVP required, tickets optional (donations)
2. **Class Events**: Ticket purchase required
3. **Auto-RSVP**: Purchasing ticket for social event auto-creates RSVP
4. **Status Filtering**: Only Active participations count toward capacity

### Potential Issues
1. **TicketType.Sold**: Manually updated, could diverge from actual participation count
2. **Data Synchronization**: No automatic sync between Sold count and EventParticipations
3. **Duplicate Prevention**: Application-level validation, no database unique constraint

### Investigation Priorities
1. Query "Suspension Basics" event to compare Sold vs actual participation count
2. Trace frontend sold count display to identify data source
3. Review duplicate purchase scenarios and race conditions

---

## Document Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-11-08 | Initial research document created | Business Requirements Agent |

