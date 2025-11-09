# Implementation Handoff: Sold Count & Capacity Redesign

**Date**: 2025-11-08
**From**: Main Orchestrator
**To**: backend-developer Agent
**Phase**: Design → Implementation
**Feature**: Sold Count and Capacity Calculation System
**Work Type**: Refactor

---

## Executive Summary

We are implementing a major redesign of the sold count and capacity calculation system to:
1. **Rename** EventParticipation → EventAttendance (clearer terminology)
2. **Remove** stored `TicketType.Sold` and `Session.CurrentAttendees` columns
3. **Replace** with calculated properties that query EventAttendance records
4. **Add** TicketPurchaseId foreign key to link attendance to payment
5. **Add** unique constraint to enforce "one active attendance per user per event"
6. **Fix** seed data to create realistic test scenarios

**Critical**: User approved all design decisions. NO deviation from spec allowed.

## Pre-Implementation Status

### Git Status
- ✅ **Checkpoint commit**: `67cab464` (feat: checkpoint before sold count/capacity redesign)
- ✅ **Test baseline**: 96.4% frontend pass rate (407/422 tests passing)
- ✅ **Design approved**: User signed off on all decisions

### Test Baseline
- Frontend: 407/422 passing (96.4%)
- Backend: Cannot compile (pre-existing issue, not blocking)
- Target: Maintain or improve 96.4% pass rate

## Critical Design Decisions

### Decision 1: Rename EventParticipation → EventAttendance

**Decision**: Rename the entity class and all references
**Rationale**: "Attendance" is clearer than "Participation" - users understand what it means
**Impact**: Touches many files (entities, services, DTOs, tests, frontend)
**User Requirement**: MUST update in-code documentation to explain the purpose

### Decision 2: Self-Healing Sold Count

**Decision**: Remove stored `TicketType.Sold` column, replace with calculated property
**Rationale**:
- Always accurate (no sync issues)
- Self-healing (recalculates every time)
- User requirement: "recalculate on EVERY purchase/cancel/refund action"

**Implementation**:
```csharp
/// <summary>
/// Number of tickets sold for this ticket type.
///
/// BUSINESS LOGIC:
/// - Counts active attendances (Status = Active) only
/// - Automatically excludes cancelled/refunded attendances
/// - Self-healing: Recalculates on every API call
/// - No manual increment/decrement needed
///
/// DESIGN DECISION:
/// Calculated property (not stored) to prevent sync issues.
/// Previous approach: Manually incremented Sold field became stale when tickets cancelled.
/// Current approach: Always queries current EventAttendance records for accuracy.
///
/// FUTURE: Supports multi-ticket purchases (counts all attendances regardless of quantity)
/// </summary>
[NotMapped] // Do NOT store in database
public int Sold
{
    get
    {
        if (Event?.EventAttendances == null) return 0;

        return Event.EventAttendances.Count(ea =>
            ea.Status == AttendanceStatus.Active &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.TicketPurchase != null &&
            ea.TicketPurchase.TicketTypeId == Id);
    }
}
```

### Decision 3: Session CurrentAttendees Calculation

**Decision**: Remove stored `Session.CurrentAttendees`, make it calculated property
**Rationale**: Same as sold count - prevents sync issues
**Implementation**:
```csharp
/// <summary>
/// Current number of attendees registered for this session.
///
/// BUSINESS LOGIC:
/// - Counts active ticket attendances for this session
/// - Handles single-session tickets (SessionId = this session)
/// - Handles multi-session tickets (IsMultiSession flag)
///
/// DESIGN DECISION:
/// Calculated property to ensure accuracy. Previous approach calculated
/// in EventService.cs but never persisted, leading to confusion.
///
/// CAPACITY CALCULATION:
/// For workshops: Capacity based on ticket count (this property)
/// For social events: Capacity based on RSVP count (different calculation)
/// </summary>
[NotMapped]
public int CurrentAttendees
{
    get
    {
        if (Event?.EventAttendances == null) return 0;

        return Event.EventAttendances.Count(ea =>
            ea.Status == AttendanceStatus.Active &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.TicketPurchase != null &&
            // Ticket is for this session
            (ea.TicketPurchase.TicketType.SessionId == Id ||
             // OR it's a multi-session ticket for this event
             (ea.TicketPurchase.TicketType.SessionId == null &&
              ea.TicketPurchase.TicketType.EventId == EventId)));
    }
}
```

### Decision 4: Add Foreign Key Linkage

**Decision**: Add `TicketPurchaseId` to EventAttendance
**Rationale**:
- Makes relationship explicit
- Easier to query sold count
- Supports future multi-ticket (one purchase → multiple attendances)

**Implementation**:
```csharp
/// <summary>
/// Link to the payment transaction for this attendance.
///
/// BUSINESS LOGIC:
/// - NULL for RSVP attendance (free, no payment)
/// - NOT NULL for Ticket attendance (paid)
///
/// FUTURE MULTI-TICKET SUPPORT:
/// One TicketPurchase (Quantity=2) can create TWO EventAttendance records.
/// This allows tracking each person attending (needed for roster, check-in).
/// </summary>
public Guid? TicketPurchaseId { get; set; }

/// <summary>
/// Navigation property to the payment transaction.
/// Used for refund processing and financial reconciliation.
/// </summary>
public TicketPurchase? TicketPurchase { get; set; }
```

### Decision 5: Unique Constraint

**Decision**: Database constraint to enforce "one active attendance per user per event"
**Rationale**:
- Defense in depth (application + database enforcement)
- Prevents race conditions
- User requirement: Only ONE active ticket/RSVP per user

**Implementation**:
```csharp
// In ApplicationDbContext.OnModelCreating
modelBuilder.Entity<EventAttendance>()
    .HasIndex(ea => new { ea.EventId, ea.UserId, ea.AttendanceType })
    .HasFilter("\"Status\" = 1") // Only Active status
    .IsUnique()
    .HasDatabaseName("IX_EventAttendances_OneActivePerUser");
```

## Implementation Plan

### Phase 1: Rename EventParticipation → EventAttendance

**Files to Update** (COMPLETE LIST - do ALL of these):

#### Backend Entities
1. `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventParticipation.cs`
   - Rename file to `EventAttendance.cs`
   - Rename class to `EventAttendance`
   - Add XML comments explaining purpose

2. `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/ParticipationType.cs`
   - Rename file to `AttendanceType.cs`
   - Rename enum to `AttendanceType`
   - Update comments: RSVP = free attendance, Ticket = paid attendance

3. `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/ParticipationStatus.cs`
   - Rename file to `AttendanceStatus.cs`
   - Rename enum to `AttendanceStatus`

#### Backend Services
4. `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/ParticipationService.cs`
   - Rename file to `AttendanceService.cs`
   - Rename class to `AttendanceService`
   - Update all references to EventParticipation → EventAttendance
   - Update all references to ParticipationType → AttendanceType
   - Update all references to ParticipationStatus → AttendanceStatus

5. `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/IParticipationService.cs`
   - Rename file to `IAttendanceService.cs`
   - Rename interface to `IAttendanceService`

#### Database Context
6. `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs`
   - Rename `DbSet<EventParticipation>` → `DbSet<EventAttendance>`
   - Rename property `EventParticipations` → `EventAttendances`
   - Update all configuration references

#### Event Entities
7. `/home/chad/repos/witchcityrope/apps/api/Features/Events/Entities/Event.cs`
   - Rename navigation property `EventParticipations` → `EventAttendances`
   - Update XML comments

8. `/home/chad/repos/witchcityrope/apps/api/Features/Events/Entities/TicketType.cs`
   - Update `Sold` property to calculated (see Decision 2 above)
   - Add XML comments

9. `/home/chad/repos/witchcityrope/apps/api/Features/Events/Entities/Session.cs`
   - Update `CurrentAttendees` property to calculated (see Decision 3 above)
   - Add XML comments

#### Services Using EventParticipation
10. `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`
    - Update all `EventParticipations` → `EventAttendances`
    - Update all `ParticipationType` → `AttendanceType`
    - Update all `ParticipationStatus` → `AttendanceStatus`
    - Remove manual CurrentAttendees calculation (now automatic via property)

11. `/home/chad/repos/witchcityrope/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs`
    - Update all references

12. `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/ParticipationSeeder.cs`
    - Rename file to `AttendanceSeeder.cs`
    - Rename class to `AttendanceSeeder`
    - Update all entity references
    - FIX SEED DATA (see Phase 4 below)

#### Tests
13. `/home/chad/repos/witchcityrope/tests/unit/api/Features/Participation/` (all files)
    - Rename folder to `Features/Attendance/`
    - Update all class names and references

14. `/home/chad/repos/witchcityrope/tests/integration/api/Features/Vetting/` (any files using EventParticipation)
    - Update all references

### Phase 2: Database Migrations

**Create 4 Migrations in this order:**

#### Migration 1: Rename Table and Columns
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add RenameEventParticipationToEventAttendance
```

**Migration Code**:
```csharp
public partial class RenameEventParticipationToEventAttendance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Rename table
        migrationBuilder.RenameTable(
            name: "EventParticipations",
            schema: "public",
            newName: "EventAttendances",
            newSchema: "public");

        // Update indexes
        migrationBuilder.RenameIndex(
            name: "IX_EventParticipations_EventId",
            table: "EventAttendances",
            newName: "IX_EventAttendances_EventId");

        migrationBuilder.RenameIndex(
            name: "IX_EventParticipations_UserId",
            table: "EventAttendances",
            newName: "IX_EventAttendances_UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse rename
        migrationBuilder.RenameTable(
            name: "EventAttendances",
            schema: "public",
            newName: "EventParticipations",
            newSchema: "public");

        // Reverse indexes
        migrationBuilder.RenameIndex(
            name: "IX_EventAttendances_EventId",
            table: "EventParticipations",
            newName: "IX_EventParticipations_EventId");

        migrationBuilder.RenameIndex(
            name: "IX_EventAttendances_UserId",
            table: "EventParticipations",
            newName: "IX_EventParticipations_UserId");
    }
}
```

#### Migration 2: Remove Stored Columns
```bash
dotnet ef migrations add RemoveSoldAndCurrentAttendeesColumns
```

**Migration Code**:
```csharp
public partial class RemoveSoldAndCurrentAttendeesColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove TicketType.Sold (now calculated property)
        migrationBuilder.DropColumn(
            name: "Sold",
            table: "TicketTypes");

        // Remove Session.CurrentAttendees (now calculated property)
        migrationBuilder.DropColumn(
            name: "CurrentAttendees",
            table: "Sessions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore columns (but data will be lost)
        migrationBuilder.AddColumn<int>(
            name: "Sold",
            table: "TicketTypes",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CurrentAttendees",
            table: "Sessions",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
```

#### Migration 3: Add Foreign Key
```bash
dotnet ef migrations add AddTicketPurchaseIdToEventAttendance
```

**Migration Code**:
```csharp
public partial class AddTicketPurchaseIdToEventAttendance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add TicketPurchaseId column
        migrationBuilder.AddColumn<Guid>(
            name: "TicketPurchaseId",
            table: "EventAttendances",
            type: "uuid",
            nullable: true);

        // Create foreign key
        migrationBuilder.CreateIndex(
            name: "IX_EventAttendances_TicketPurchaseId",
            table: "EventAttendances",
            column: "TicketPurchaseId");

        migrationBuilder.AddForeignKey(
            name: "FK_EventAttendances_TicketPurchases_TicketPurchaseId",
            table: "EventAttendances",
            column: "TicketPurchaseId",
            principalTable: "TicketPurchases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EventAttendances_TicketPurchases_TicketPurchaseId",
            table: "EventAttendances");

        migrationBuilder.DropIndex(
            name: "IX_EventAttendances_TicketPurchaseId",
            table: "EventAttendances");

        migrationBuilder.DropColumn(
            name: "TicketPurchaseId",
            table: "EventAttendances");
    }
}
```

#### Migration 4: Add Unique Constraint
```bash
dotnet ef migrations add AddUniqueConstraintOneActiveAttendancePerUser
```

**Migration Code**:
```csharp
public partial class AddUniqueConstraintOneActiveAttendancePerUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // NOTE: This will fail if duplicate active attendances exist
        // Run seed data cleanup FIRST

        migrationBuilder.CreateIndex(
            name: "IX_EventAttendances_OneActivePerUser",
            table: "EventAttendances",
            columns: new[] { "EventId", "UserId", "AttendanceType" },
            unique: true,
            filter: "\"Status\" = 1"); // Only Active status
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EventAttendances_OneActivePerUser",
            table: "EventAttendances");
    }
}
```

### Phase 3: Update EventAttendance Entity

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`

**Add these properties and comments**:
```csharp
/// <summary>
/// Represents a user's attendance record for an event.
///
/// BUSINESS PURPOSE:
/// Tracks WHO is attending WHAT event and in what capacity (RSVP or Ticket).
/// Used for capacity calculations and attendance roster.
///
/// TWO TYPES OF ATTENDANCE:
/// 1. RSVP (AttendanceType.RSVP): Free attendance for social events
/// 2. Ticket (AttendanceType.Ticket): Paid attendance for class events or donation tickets
///
/// CAPACITY RULES:
/// - Social events: Capacity based on RSVP count (free attendees)
/// - Class events: Capacity based on Ticket count (paid attendees)
/// - Auto-RSVP: When user buys ticket for social event, RSVP is created automatically
///
/// BUSINESS RULE: One Active attendance per user per event
/// - Users can have multiple Cancelled/Refunded for history
/// - Database constraint enforces this rule
///
/// RELATIONSHIP TO PAYMENT:
/// - TicketPurchase = Financial transaction record (for refunds)
/// - EventAttendance = Attendance record (for capacity)
/// - Linked via TicketPurchaseId foreign key
///
/// FUTURE: Multi-ticket purchases will create multiple EventAttendance records
/// from one TicketPurchase (when Quantity > 1).
/// </summary>
public class EventAttendance
{
    public Guid Id { get; set; }

    /// <summary>
    /// Event the user is attending
    /// </summary>
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    /// <summary>
    /// User who is attending
    /// </summary>
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Type of attendance: RSVP (free) or Ticket (paid)
    /// </summary>
    public AttendanceType AttendanceType { get; set; }

    /// <summary>
    /// Current status: Active, Cancelled, or Refunded
    /// </summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// Link to payment transaction (NULL for RSVP, NOT NULL for Ticket).
    /// Used for refund processing and to identify which ticket type was purchased.
    ///
    /// FUTURE: One TicketPurchase can create multiple EventAttendance records
    /// when quantity > 1 (multi-ticket purchases for guests).
    /// </summary>
    public Guid? TicketPurchaseId { get; set; }

    /// <summary>
    /// Navigation property to payment transaction
    /// </summary>
    public TicketPurchase? TicketPurchase { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public string? Metadata { get; set; }
}
```

### Phase 4: Fix Seed Data

**File**: `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/AttendanceSeeder.cs`

**Critical Requirements**:
1. Create realistic test scenarios (active + cancelled + refunded)
2. Link EventAttendance to TicketPurchase via TicketPurchaseId
3. Ensure only ONE active attendance per user per event
4. For "Suspension Basics": 4 active tickets with varied history

**Example Seed Pattern**:
```csharp
// Suspension Basics - 4 active tickets + test cancelled/refunded
var suspensionEvent = events.First(e => e.Title == "Suspension Basics");
var dayOneTicket = suspensionEvent.TicketTypes.First(tt => tt.Name == "Day 1 Only");

// User 1: Simple active ticket
var purchase1 = CreateTicketPurchase(users[0], dayOneTicket, "Completed", 45.00m);
var attendance1 = CreateAttendance(users[0], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Active, purchase1.Id);
ticketPurchasesToAdd.Add(purchase1);
attendancesToAdd.Add(attendance1);

// User 2: Active ticket + previous cancelled (changed mind scenario)
var purchase2Old = CreateTicketPurchase(users[1], dayOneTicket, "Cancelled", 45.00m, daysAgo: 15);
var attendance2Old = CreateAttendance(users[1], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Cancelled, purchase2Old.Id);
var purchase2New = CreateTicketPurchase(users[1], dayOneTicket, "Completed", 45.00m, daysAgo: 10);
var attendance2New = CreateAttendance(users[1], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Active, purchase2New.Id);
ticketPurchasesToAdd.AddRange(new[] { purchase2Old, purchase2New });
attendancesToAdd.AddRange(new[] { attendance2Old, attendance2New });

// User 3: Active ticket + previous refunded (attended but got refund within 48hrs)
var purchase3Old = CreateTicketPurchase(users[2], dayOneTicket, "Refunded", 45.00m, daysAgo: 20);
var attendance3Old = CreateAttendance(users[2], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Refunded, purchase3Old.Id);
var purchase3New = CreateTicketPurchase(users[2], dayOneTicket, "Completed", 45.00m, daysAgo: 5);
var attendance3New = CreateAttendance(users[2], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Active, purchase3New.Id);
ticketPurchasesToAdd.AddRange(new[] { purchase3Old, purchase3New });
attendancesToAdd.AddRange(new[] { attendance3Old, attendance3New });

// User 4: Simple active ticket
var purchase4 = CreateTicketPurchase(users[3], dayOneTicket, "Completed", 45.00m);
var attendance4 = CreateAttendance(users[3], suspensionEvent, AttendanceType.Ticket, AttendanceStatus.Active, purchase4.Id);
ticketPurchasesToAdd.Add(purchase4);
attendancesToAdd.Add(attendance4);

// Helper methods
TicketPurchase CreateTicketPurchase(ApplicationUser user, TicketType ticketType, string paymentStatus, decimal amount, int daysAgo = 5)
{
    return new TicketPurchase
    {
        Id = Guid.NewGuid(),
        TicketTypeId = ticketType.Id,
        UserId = user.Id,
        Quantity = 1,
        TotalPrice = amount,
        PaymentStatus = paymentStatus,
        PaymentMethod = "PayPal",
        PaymentReference = $"PP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
        PurchaseDate = DateTime.UtcNow.AddDays(-daysAgo),
        CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
        UpdatedAt = DateTime.UtcNow.AddDays(-daysAgo)
    };
}

EventAttendance CreateAttendance(ApplicationUser user, Event evt, AttendanceType type, AttendanceStatus status, Guid purchaseId)
{
    return new EventAttendance
    {
        Id = Guid.NewGuid(),
        EventId = evt.Id,
        UserId = user.Id,
        AttendanceType = type,
        Status = status,
        TicketPurchaseId = purchaseId,
        CreatedAt = DateTime.UtcNow.AddDays(-5),
        UpdatedAt = DateTime.UtcNow.AddDays(-5)
    };
}
```

**CRITICAL**: Remove old code that manually incremented TicketType.Sold!

### Phase 5: Update Frontend Types

**Files to Check** (react-developer will handle, but document here):
1. Generated types in `@witchcityrope/shared-types` will auto-update from OpenAPI
2. Frontend code using `EventParticipation` → search and replace with `EventAttendance`
3. Check these files:
   - `apps/web/src/features/dashboard/`
   - `apps/web/src/features/checkin/`
   - `apps/web/src/hooks/useParticipation.ts` (rename to `useAttendance.ts`?)

## Files That MUST Be Updated

This is the COMPLETE list - do NOT skip any:

### Backend Core
- [ ] `EventAttendance.cs` (renamed from EventParticipation.cs)
- [ ] `AttendanceType.cs` (renamed from ParticipationType.cs)
- [ ] `AttendanceStatus.cs` (renamed from ParticipationStatus.cs)
- [ ] `AttendanceService.cs` (renamed from ParticipationService.cs)
- [ ] `IAttendanceService.cs` (renamed from IParticipationService.cs)
- [ ] `ApplicationDbContext.cs`
- [ ] `Event.cs`
- [ ] `TicketType.cs`
- [ ] `Session.cs`
- [ ] `TicketPurchase.cs` (add navigation property)
- [ ] `EventService.cs`
- [ ] `UserDashboardProfileService.cs`
- [ ] `AttendanceSeeder.cs` (renamed from ParticipationSeeder.cs)

### Migrations
- [ ] Create Migration 1: RenameEventParticipationToEventAttendance
- [ ] Create Migration 2: RemoveSoldAndCurrentAttendeesColumns
- [ ] Create Migration 3: AddTicketPurchaseIdToEventAttendance
- [ ] Create Migration 4: AddUniqueConstraintOneActiveAttendancePerUser

### Tests (Unit)
- [ ] All files in `tests/unit/api/Features/Participation/` → rename folder and update
- [ ] `ParticipationServiceTests.cs` → `AttendanceServiceTests.cs`
- [ ] Any other test files using EventParticipation

### Tests (Integration)
- [ ] Search for EventParticipation in integration tests
- [ ] Update all references

### Configuration
- [ ] Program.cs (service registration)
- [ ] Any dependency injection configurations

## Testing Requirements

### Unit Tests
- [ ] All EventAttendance entity tests pass
- [ ] AttendanceService tests pass
- [ ] TicketType.Sold calculation tests pass
- [ ] Session.CurrentAttendees calculation tests pass

### Integration Tests
- [ ] Create ticket purchase → EventAttendance created with TicketPurchaseId
- [ ] Cancel ticket → EventAttendance.Status = Cancelled
- [ ] Sold count excludes cancelled attendances
- [ ] Unique constraint prevents duplicate active attendances

### Manual Verification
- [ ] "Suspension Basics" shows 4 tickets sold (not 7)
- [ ] Admin event details page displays correctly
- [ ] Cancelling ticket frees up capacity
- [ ] Cannot buy second active ticket (constraint blocks)

## Success Criteria

✅ **Code Compiles**: No build errors
✅ **Migrations Apply**: All 4 migrations run successfully
✅ **Tests Pass**: Maintain 96.4%+ pass rate
✅ **Sold Count Correct**: "Suspension Basics" = 4 sold
✅ **Self-Healing**: Cancelling ticket automatically updates sold count
✅ **Documentation**: All entities have XML comments explaining business logic
✅ **No Hallucination**: Implementation matches this spec exactly

## Critical Warnings

### DO NOT:
- ❌ Create manual increment/decrement logic for Sold
- ❌ Store CurrentAttendees in database
- ❌ Allow multiple active attendances per user (constraint prevents)
- ❌ Skip XML documentation (required!)
- ❌ Deviate from design spec

### DO:
- ✅ Use calculated properties (NotMapped)
- ✅ Add extensive XML comments explaining business logic
- ✅ Link EventAttendance to TicketPurchase via foreign key
- ✅ Fix seed data with realistic scenarios
- ✅ Follow the spec EXACTLY

## Questions to Ask BEFORE Starting

- [ ] Have you read the complete design document?
- [ ] Do you understand why two tables exist (EventAttendance vs TicketPurchase)?
- [ ] Are you clear on calculated vs stored properties?
- [ ] Do you know where to add XML documentation?
- [ ] Have you reviewed the seed data requirements?

## Next Agent

After backend-developer completes:
- **react-developer**: Update frontend for renamed entity
- **test-developer**: Create additional test coverage
- **test-executor**: Run full test suite

## References

- **Design Document**: `/docs/functional-areas/events/design/sold-count-capacity-redesign-2025-11-08.md`
- **Research Analysis**: `/docs/functional-areas/events/research/current-implementation-analysis-2025-11-08.md`
- **Test Baseline**: `/test-results/test-baseline-2025-11-08-pre-redesign.md`

---

**Backend-Developer**: Read this ENTIRE handoff before writing any code. If anything is unclear, ASK before proceeding. Do NOT hallucinate solutions - implement EXACTLY what is specified here.
