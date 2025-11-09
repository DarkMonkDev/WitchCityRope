# Sold Count and Capacity Calculation System - Database Design

**Date**: 2025-11-08
**Category**: Database Design
**Status**: Design Phase
**Author**: Database Designer Agent

---

## Executive Summary

This design addresses critical issues in the current sold count and capacity calculation system while preparing for future multi-ticket purchase support. The solution eliminates confusion about the data model, implements self-healing calculation algorithms, and enforces business rules at the database level.

**Key Principles**:
1. **Calculated Fields Over Stored Fields**: Sold counts are always calculated from actual participation records
2. **Self-Healing**: Every purchase/cancel/refund action recalculates correctly, even if previous operations missed updates
3. **Single Source of Truth**: EventParticipation table is the authority for "who is attending"
4. **Separation of Concerns**: Attendance tracking vs Payment transactions are separate domains

---

## Table of Contents

1. [Business Context](#business-context)
2. [Current State Analysis](#current-state-analysis)
3. [Data Model Clarification](#data-model-clarification)
4. [Design Solutions](#design-solutions)
5. [Database Schema Changes](#database-schema-changes)
6. [Entity Framework Configuration](#entity-framework-configuration)
7. [Migration Strategy](#migration-strategy)
8. [Code Changes Map](#code-changes-map)
9. [Testing Strategy](#testing-strategy)
10. [Future Support](#future-support)

---

## Business Context

### Current Business Rules

**Social Events:**
- Primary metric: RSVP count
- Tickets are optional donations
- Auto-RSVP when purchasing ticket
- Capacity = Total RSVP count

**Class Events:**
- Primary metric: Ticket count
- One active ticket per user (currently)
- Capacity = Total ticket count

**Multi-Session Events:**
- Ticket types can span multiple sessions
- Each session has individual capacity
- Capacity aggregates correctly across sessions

### Future Requirements

**Multi-Ticket Purchases:**
- One user can buy multiple tickets (for guests)
- Each ticket must be assigned to a user OR name+email
- Each ticket = one person = one seat in capacity
- Payment transaction links to multiple participations

---

## Current State Analysis

### What Works

✅ **TicketTypeDto.QuantitySold** - Already calculated dynamically
✅ **Automatic filtering** - Only counts Status=Active participations
✅ **Recalculates on every API call** - Always accurate for frontend
✅ **Frontend displays correctly** - Uses the calculated DTO values

### What's Broken

❌ **TicketType.Sold field** - Manually incremented, never decremented (stale data)
❌ **Seed data violations** - Multiple active purchases per user violates business rule
❌ **Orphaned TicketPurchases** - No EventParticipation record (data integrity issue)
❌ **No foreign key link** - EventParticipation → TicketPurchase relationship missing
❌ **Session.CurrentAttendees** - Calculated in-memory but not persisted consistently

### Root Causes

1. **Stored `Sold` column exists but is never updated** by service code
2. **No database constraints** enforcing "one active participation per user per event"
3. **Implicit relationship** between EventParticipation and TicketPurchase (not explicit FK)
4. **Two sources of truth** for sold counts (stored vs calculated)

---

## Data Model Clarification

### Purpose of Each Table

#### EventParticipation Table (Attendance Tracking Domain)

**Purpose**: Track WHO is attending WHAT event

**Business Rules**:
- One ACTIVE participation per user per event (can have multiple cancelled/refunded for history)
- `ParticipationType.RSVP` = Free attendance (social events)
- `ParticipationType.Ticket` = Paid attendance (class events, also social with donation)

**Used For**:
- Capacity calculations (COUNT where Status=Active)
- Attendance roster
- Cancellation tracking
- Historical record of participation changes

**Future**: Will have multiple participations per TicketPurchase when Quantity > 1

**Current Schema**:
```sql
CREATE TABLE "EventParticipations" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "EventId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ParticipationType" integer NOT NULL,  -- 1=RSVP, 2=Ticket
    "Status" integer NOT NULL,             -- 1=Active, 2=Cancelled, 3=Refunded, 4=Waitlisted
    "Notes" text,
    "CancelledAt" timestamptz,
    "CancellationReason" text,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,

    CONSTRAINT "FK_EventParticipations_Events"
        FOREIGN KEY ("EventId") REFERENCES "Events"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventParticipations_Users"
        FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
```

#### TicketPurchase Table (Payment Transactions Domain)

**Purpose**: Financial record for refunds and accounting

**Business Rules**:
- Records payment method, amount, reference
- Quantity field supports future multi-ticket purchases
- PaymentStatus tracks transaction state

**Used For**:
- Refund processing
- Financial reconciliation
- Payment audit trail
- Revenue reporting

**Future**: One purchase can create multiple EventParticipations (when Quantity > 1)

**Current Schema**:
```sql
CREATE TABLE "TicketPurchases" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "TicketTypeId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PurchaseDate" timestamptz NOT NULL,
    "Quantity" integer NOT NULL DEFAULT 1,
    "TotalPrice" decimal(10,2) NOT NULL,
    "PaymentStatus" text NOT NULL,         -- "Pending", "Completed", "Confirmed"
    "PaymentMethod" text NOT NULL,
    "PaymentReference" text NOT NULL,
    "Notes" text,
    "RecordedByStaffId" uuid,              -- For door cash purchases
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,

    CONSTRAINT "FK_TicketPurchases_TicketTypes"
        FOREIGN KEY ("TicketTypeId") REFERENCES "TicketTypes"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TicketPurchases_Users"
        FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
```

#### Why Two Tables?

**Separation of Concerns**:
- **Attendance Domain**: WHO is attending, cancellation tracking, capacity management
- **Payment Domain**: Financial transactions, refund processing, accounting

**Different Lifecycles**:
- Participation cancelled ≠ Payment refunded
- User can cancel without refund (late cancellation)
- Payment can fail but participation record remains for audit
- Historical tracking requires both records preserved

**Future Flexibility**:
- One payment can cover multiple people (multi-ticket)
- Group purchases with individual attendance tracking
- Payment splitting scenarios (e.g., scholarship partial payment)

**Query Performance**:
- Capacity queries only need EventParticipation (no payment data)
- Financial reports only need TicketPurchase (no attendance data)
- Separate indexes optimize each domain

---

## Design Solutions

### Solution 1: Remove TicketType.Sold Column

**Current Problem**: `TicketType.Sold` field is stored but never updated, becomes stale immediately

**Recommended Solution**: Remove stored field, use calculated property

**Before (Current - Broken)**:
```csharp
public class TicketType
{
    public int Sold { get; set; } = 0;  // ❌ Never updated, always stale
}
```

**After (Proposed - Self-Healing)**:
```csharp
public class TicketType
{
    // Remove stored field from database completely

    // Add calculated property (NOT persisted to database)
    [NotMapped]
    public int Sold
    {
        get
        {
            if (Event?.EventParticipations == null) return 0;

            // Automatically recalculates from EventParticipations
            // Self-healing: Always accurate regardless of previous operations
            return Event.EventParticipations
                .Count(ep =>
                    ep.Status == ParticipationStatus.Active &&
                    ep.ParticipationType == ParticipationType.Ticket &&
                    ep.TicketPurchase != null &&
                    ep.TicketPurchase.TicketTypeId == Id);
        }
    }
}
```

**Benefits**:
- ✅ Always accurate (no sync issues)
- ✅ Self-healing (recalculates every time)
- ✅ No manual maintenance needed
- ✅ Meets user requirement for calculation on every action
- ✅ Cancellations automatically reflected (Status != Active excluded)

**Trade-offs**:
- Requires EventParticipations to be loaded (but we already do this)
- Slightly slower than reading stored field (negligible with proper indexing)
- Cannot query directly in SQL (but DTO already handles this)

**Alternative (Not Recommended)**: Keep stored field but calculate on save
- Would require calling recalculation after EVERY purchase/cancel/refund
- Risk of missing a call = stale data
- More code to maintain
- More complexity

---

### Solution 2: Add Foreign Key Link (EventParticipation → TicketPurchase)

**Current Problem**: No explicit database relationship between attendance and payment

**Solution**: Add foreign key to link participation to its payment transaction

**Schema Change**:
```sql
ALTER TABLE "EventParticipations"
ADD COLUMN "TicketPurchaseId" uuid NULL;

ALTER TABLE "EventParticipations"
ADD CONSTRAINT "FK_EventParticipations_TicketPurchases"
    FOREIGN KEY ("TicketPurchaseId")
    REFERENCES "TicketPurchases"("Id")
    ON DELETE SET NULL;  -- Preserve participation even if payment record deleted
```

**Entity Framework Configuration**:
```csharp
public class EventParticipation
{
    // Existing fields...

    // NEW: Link to payment transaction (null for RSVP type)
    public Guid? TicketPurchaseId { get; set; }
    public TicketPurchase? TicketPurchase { get; set; }
}

public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        // ... existing configuration ...

        // NEW: Configure foreign key relationship
        builder.HasOne(ep => ep.TicketPurchase)
               .WithMany() // TicketPurchase doesn't need reverse navigation yet
               .HasForeignKey(ep => ep.TicketPurchaseId)
               .OnDelete(DeleteBehavior.SetNull) // Preserve participation if payment deleted
               .IsRequired(false); // Null for RSVP type participations
    }
}
```

**Benefits**:
- ✅ Explicit relationship (clear data model)
- ✅ Easy to find participation for a purchase
- ✅ Supports future multi-ticket: multiple participations → one purchase
- ✅ Can query sold count via navigation property
- ✅ Data integrity enforced by database

**Migration to Populate Existing Data**:
```sql
-- Migration to add foreign key and populate existing data
UPDATE "EventParticipations" ep
SET "TicketPurchaseId" = tp."Id"
FROM "TicketPurchases" tp
JOIN "TicketTypes" tt ON tp."TicketTypeId" = tt."Id"
WHERE ep."EventId" = tt."EventId"
  AND ep."UserId" = tp."UserId"
  AND ep."ParticipationType" = 2  -- Ticket type
  AND ep."TicketPurchaseId" IS NULL  -- Only update if not already set
  AND ep."Status" = 1;  -- Active participations only
```

---

### Solution 3: Enforce "One Active Participation Per User Per Event" with Unique Index

**Current Problem**: No database-level enforcement of business rule

**Solution**: Partial unique index (PostgreSQL feature)

**Schema Change**:
```sql
CREATE UNIQUE INDEX "IX_EventParticipations_OneActivePerUserPerEvent"
ON "EventParticipations" ("EventId", "UserId", "ParticipationType")
WHERE "Status" = 1;  -- Only enforce for Active status
```

**Entity Framework Configuration**:
```csharp
public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        // ... existing configuration ...

        // NEW: Unique constraint for one active participation per user per event
        builder.HasIndex(ep => new { ep.EventId, ep.UserId, ep.ParticipationType })
               .HasFilter("\"Status\" = 1") // PostgreSQL syntax for Active status
               .IsUnique()
               .HasDatabaseName("IX_EventParticipations_OneActivePerUserPerEvent");
    }
}
```

**Benefits**:
- ✅ Database enforces business rule (defense in depth)
- ✅ Prevents race conditions (two simultaneous purchases)
- ✅ Clear error message when violated
- ✅ Filter allows multiple Cancelled/Refunded for same user (history preserved)

**Trade-offs**:
- Requires PostgreSQL 9.0+ (we're on PostgreSQL 15+, so no issue)
- Application code must handle unique constraint violations gracefully

**Error Handling Example**:
```csharp
try
{
    var participation = new EventParticipation(eventId, userId, ParticipationType.Ticket);
    _context.EventParticipations.Add(participation);
    await _context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
    && pgEx.SqlState == "23505" // Unique violation
    && pgEx.ConstraintName == "IX_EventParticipations_OneActivePerUserPerEvent")
{
    throw new BusinessException("User already has an active participation for this event");
}
```

---

### Solution 4: Session.CurrentAttendees as Calculated Property

**Current Problem**: `Session.CurrentAttendees` calculated in-memory but not persisted, creating two sources of truth

**Solution**: Make it a calculated property (not stored in database)

**Before (Current - Inconsistent)**:
```csharp
public class Session
{
    public int CurrentAttendees { get; set; } = 0;  // Stored but overwritten in-memory
}

// In EventService.GetEventAsync() - Lines 158-174
foreach (var session in eventEntity.Sessions)
{
    var ticketsSold = eventEntity.TicketTypes
        .Where(tt => tt.SessionId == session.Id)
        .SelectMany(tt => tt.Purchases)
        .Where(p => p.IsPaymentCompleted && activeUserIds.Contains(p.UserId))
        .Sum(p => p.Quantity);

    session.CurrentAttendees = ticketsSold;  // In-memory calculation
}
// Not persisted back to database!
```

**After (Proposed - Self-Healing)**:
```csharp
public class Session
{
    // Remove stored field from database

    // Add calculated property (NOT persisted to database)
    [NotMapped]
    public int CurrentAttendees
    {
        get
        {
            if (Event?.EventParticipations == null) return 0;

            // Count participations for tickets that include this session
            return Event.EventParticipations
                .Count(ep =>
                    ep.Status == ParticipationStatus.Active &&
                    ep.ParticipationType == ParticipationType.Ticket &&
                    ep.TicketPurchase != null &&
                    ep.TicketPurchase.TicketType != null &&
                    (ep.TicketPurchase.TicketType.SessionId == Id ||
                     ep.TicketPurchase.TicketType.IsMultiSession)); // Future: check session mapping
        }
    }
}
```

**Benefits**:
- ✅ Always accurate
- ✅ Handles multi-session tickets correctly (when implemented)
- ✅ No sync issues
- ✅ Self-healing

**Trade-offs**:
- Requires Event.EventParticipations to be loaded
- Cannot query directly in raw SQL (but service layer always uses EF Core)

---

### Solution 5: Multi-Session Capacity Support (Future-Ready)

**Current Problem**: Multi-session tickets don't count toward individual session capacities

**Immediate Solution** (Simpler for Now):
Keep current `SessionId` field and add `IsMultiSession` flag

```csharp
public class TicketType
{
    // Existing single-session support
    public Guid? SessionId { get; set; }
    public Session? Session { get; set; }

    // NEW: Multi-session flag
    public bool IsMultiSession { get; set; } = false;

    // Business rule:
    // - If SessionId != null: Ticket is for specific session
    // - If SessionId == null && IsMultiSession == true: Ticket is for all sessions
    // - If SessionId == null && IsMultiSession == false: Error (invalid state)
}
```

**Future Solution** (When Needed):
Explicit many-to-many relationship with junction table

```csharp
public class TicketType
{
    // Existing single-session support (keep for backward compatibility)
    public Guid? SessionId { get; set; }
    public Session? Session { get; set; }

    // NEW: Multi-session support via junction table
    public List<TicketTypeSession> TicketTypeSessions { get; set; } = new();

    // Calculated property: Which sessions does this ticket grant access to?
    [NotMapped]
    public IEnumerable<Guid> IncludedSessionIds
    {
        get
        {
            if (SessionId.HasValue)
            {
                // Single-session ticket
                return new[] { SessionId.Value };
            }

            if (TicketTypeSessions.Any())
            {
                // Multi-session ticket via junction table
                return TicketTypeSessions.Select(tts => tts.SessionId);
            }

            // All sessions (default multi-session behavior)
            return Event?.Sessions?.Select(s => s.Id) ?? Enumerable.Empty<Guid>();
        }
    }
}

// Junction table for many-to-many
public class TicketTypeSession
{
    public Guid TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = null!;

    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;
}
```

**Migration Path**:
1. **Phase 1** (This design): Add `IsMultiSession` flag, use in calculations
2. **Phase 2** (Future): Create junction table when explicit session selection needed
3. **Phase 3** (Future): Migrate data from flags to junction table
4. **Phase 4** (Future): Remove flags, use junction table exclusively

---

## Database Schema Changes

### Migration 1: Remove Stale Sold Columns

**File**: `20251108_RemoveStaleSoldColumns.cs`

```csharp
public partial class RemoveStaleSoldColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove TicketType.Sold column (stale data)
        migrationBuilder.DropColumn(
            name: "Sold",
            table: "TicketTypes");

        // Remove Session.CurrentAttendees column (calculated in-memory)
        migrationBuilder.DropColumn(
            name: "CurrentAttendees",
            table: "Sessions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore columns for rollback
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

### Migration 2: Add Foreign Key Link

**File**: `20251108_AddTicketPurchaseLinkToParticipation.cs`

```csharp
public partial class AddTicketPurchaseLinkToParticipation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add foreign key column
        migrationBuilder.AddColumn<Guid>(
            name: "TicketPurchaseId",
            table: "EventParticipations",
            type: "uuid",
            nullable: true);

        // Populate existing data by matching UserId + EventId
        migrationBuilder.Sql(@"
            UPDATE ""EventParticipations"" ep
            SET ""TicketPurchaseId"" = tp.""Id""
            FROM ""TicketPurchases"" tp
            JOIN ""TicketTypes"" tt ON tp.""TicketTypeId"" = tt.""Id""
            WHERE ep.""EventId"" = tt.""EventId""
              AND ep.""UserId"" = tp.""UserId""
              AND ep.""ParticipationType"" = 2  -- Ticket type
              AND ep.""TicketPurchaseId"" IS NULL
              AND ep.""Status"" = 1;  -- Active only
        ");

        // Add foreign key constraint
        migrationBuilder.AddForeignKey(
            name: "FK_EventParticipations_TicketPurchases",
            table: "EventParticipations",
            column: "TicketPurchaseId",
            principalTable: "TicketPurchases",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // Add index for foreign key performance
        migrationBuilder.CreateIndex(
            name: "IX_EventParticipations_TicketPurchaseId",
            table: "EventParticipations",
            column: "TicketPurchaseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EventParticipations_TicketPurchases",
            table: "EventParticipations");

        migrationBuilder.DropIndex(
            name: "IX_EventParticipations_TicketPurchaseId",
            table: "EventParticipations");

        migrationBuilder.DropColumn(
            name: "TicketPurchaseId",
            table: "EventParticipations");
    }
}
```

### Migration 3: Add Unique Constraint

**File**: `20251108_AddUniqueActiveParticipationConstraint.cs`

```csharp
public partial class AddUniqueActiveParticipationConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove any duplicate active participations before creating constraint
        migrationBuilder.Sql(@"
            -- Find duplicates and keep only the most recent one
            WITH Duplicates AS (
                SELECT ""Id"",
                       ROW_NUMBER() OVER (
                           PARTITION BY ""EventId"", ""UserId"", ""ParticipationType""
                           ORDER BY ""CreatedAt"" DESC
                       ) as rn
                FROM ""EventParticipations""
                WHERE ""Status"" = 1  -- Active only
            )
            UPDATE ""EventParticipations""
            SET ""Status"" = 2,  -- Set to Cancelled
                ""CancelledAt"" = NOW(),
                ""CancellationReason"" = 'Duplicate participation removed during migration'
            WHERE ""Id"" IN (
                SELECT ""Id"" FROM Duplicates WHERE rn > 1
            );
        ");

        // Create partial unique index (PostgreSQL feature)
        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX ""IX_EventParticipations_OneActivePerUserPerEvent""
            ON ""EventParticipations"" (""EventId"", ""UserId"", ""ParticipationType"")
            WHERE ""Status"" = 1;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EventParticipations_OneActivePerUserPerEvent",
            table: "EventParticipations");
    }
}
```

### Migration 4: Add Multi-Session Support

**File**: `20251108_AddMultiSessionSupport.cs`

```csharp
public partial class AddMultiSessionSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add IsMultiSession flag to TicketTypes
        migrationBuilder.AddColumn<bool>(
            name: "IsMultiSession",
            table: "TicketTypes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Set flag for existing tickets where SessionId is null
        // (These are assumed to be multi-session tickets)
        migrationBuilder.Sql(@"
            UPDATE ""TicketTypes""
            SET ""IsMultiSession"" = true
            WHERE ""SessionId"" IS NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsMultiSession",
            table: "TicketTypes");
    }
}
```

---

## Entity Framework Configuration

### EventParticipation Entity

**File**: `/apps/api/Features/Participation/Entities/EventParticipation.cs`

```csharp
public class EventParticipation
{
    // Existing properties...
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public ParticipationType ParticipationType { get; set; }
    public ParticipationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // NEW: Link to payment transaction
    public Guid? TicketPurchaseId { get; set; }
    public TicketPurchase? TicketPurchase { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
```

**Configuration File**: `/apps/api/Features/Participation/Entities/Configuration/EventParticipationConfiguration.cs`

```csharp
public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        builder.ToTable("EventParticipations");

        builder.HasKey(ep => ep.Id);

        // Properties
        builder.Property(ep => ep.ParticipationType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(ep => ep.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(ep => ep.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(ep => ep.CancelledAt)
               .HasColumnType("timestamptz");

        // NEW: Foreign key to TicketPurchase
        builder.HasOne(ep => ep.TicketPurchase)
               .WithMany() // No reverse navigation yet
               .HasForeignKey(ep => ep.TicketPurchaseId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        // Foreign keys (existing)
        builder.HasOne(ep => ep.Event)
               .WithMany(e => e.EventParticipations)
               .HasForeignKey(ep => ep.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ep => ep.User)
               .WithMany()
               .HasForeignKey(ep => ep.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // NEW: Unique constraint for one active participation per user per event
        builder.HasIndex(ep => new { ep.EventId, ep.UserId, ep.ParticipationType })
               .HasFilter("\"Status\" = 1")
               .IsUnique()
               .HasDatabaseName("IX_EventParticipations_OneActivePerUserPerEvent");

        // Performance indexes
        builder.HasIndex(ep => ep.EventId)
               .HasDatabaseName("IX_EventParticipations_EventId");

        builder.HasIndex(ep => ep.UserId)
               .HasDatabaseName("IX_EventParticipations_UserId");

        builder.HasIndex(ep => new { ep.EventId, ep.Status })
               .HasDatabaseName("IX_EventParticipations_EventId_Status");
    }
}
```

### TicketType Entity

**File**: `/apps/api/Models/TicketType.cs`

```csharp
public class TicketType
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PricingType PricingType { get; set; }
    public decimal? Price { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? DefaultPrice { get; set; }
    public int Available { get; set; }

    // NEW: Multi-session support
    public bool IsMultiSession { get; set; } = false;

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Session? Session { get; set; }
    public ICollection<TicketPurchase> Purchases { get; set; } = new List<TicketPurchase>();

    // REMOVED: public int Sold { get; set; }  - No longer stored

    // NEW: Calculated property for sold count (NOT persisted to database)
    [NotMapped]
    public int Sold
    {
        get
        {
            if (Event?.EventParticipations == null) return 0;

            // Self-healing calculation: always accurate
            return Event.EventParticipations
                .Count(ep =>
                    ep.Status == ParticipationStatus.Active &&
                    ep.ParticipationType == ParticipationType.Ticket &&
                    ep.TicketPurchase != null &&
                    ep.TicketPurchase.TicketTypeId == Id);
        }
    }

    // Calculated property: Remaining tickets
    [NotMapped]
    public int Remaining => Available - Sold;

    // Calculated property: Is sold out?
    [NotMapped]
    public bool IsSoldOut => Sold >= Available;
}
```

**Configuration File**: `/apps/api/Models/Configuration/TicketTypeConfiguration.cs`

```csharp
public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("TicketTypes");

        builder.HasKey(tt => tt.Id);

        // Properties
        builder.Property(tt => tt.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(tt => tt.PricingType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(tt => tt.Price)
               .HasColumnType("decimal(10,2)");

        builder.Property(tt => tt.MinPrice)
               .HasColumnType("decimal(10,2)");

        builder.Property(tt => tt.MaxPrice)
               .HasColumnType("decimal(10,2)");

        builder.Property(tt => tt.DefaultPrice)
               .HasColumnType("decimal(10,2)");

        builder.Property(tt => tt.Available)
               .IsRequired();

        // NEW: Multi-session support
        builder.Property(tt => tt.IsMultiSession)
               .IsRequired()
               .HasDefaultValue(false);

        // REMOVED: Sold column configuration

        // Foreign keys
        builder.HasOne(tt => tt.Event)
               .WithMany(e => e.TicketTypes)
               .HasForeignKey(tt => tt.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tt => tt.Session)
               .WithMany(s => s.TicketTypes)
               .HasForeignKey(tt => tt.SessionId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        // Indexes
        builder.HasIndex(tt => tt.EventId)
               .HasDatabaseName("IX_TicketTypes_EventId");

        builder.HasIndex(tt => tt.SessionId)
               .HasDatabaseName("IX_TicketTypes_SessionId");
    }
}
```

### Session Entity

**File**: `/apps/api/Models/Session.cs`

```csharp
public class Session
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    // REMOVED: public int CurrentAttendees { get; set; }  - No longer stored

    // NEW: Calculated property for current attendees (NOT persisted to database)
    [NotMapped]
    public int CurrentAttendees
    {
        get
        {
            if (Event?.EventParticipations == null) return 0;

            // Count participations for tickets that include this session
            return Event.EventParticipations
                .Count(ep =>
                    ep.Status == ParticipationStatus.Active &&
                    ep.ParticipationType == ParticipationType.Ticket &&
                    ep.TicketPurchase != null &&
                    ep.TicketPurchase.TicketType != null &&
                    (ep.TicketPurchase.TicketType.SessionId == Id ||
                     ep.TicketPurchase.TicketType.IsMultiSession));
        }
    }

    // Calculated property: Remaining capacity
    [NotMapped]
    public int RemainingCapacity => Capacity - CurrentAttendees;

    // Calculated property: Is sold out?
    [NotMapped]
    public bool IsSoldOut => CurrentAttendees >= Capacity;
}
```

**Configuration File**: `/apps/api/Models/Configuration/SessionConfiguration.cs`

```csharp
public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.SessionCode)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(s => s.StartTime)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(s => s.EndTime)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(s => s.Capacity)
               .IsRequired();

        // REMOVED: CurrentAttendees column configuration

        // Foreign key
        builder.HasOne(s => s.Event)
               .WithMany(e => e.Sessions)
               .HasForeignKey(s => s.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.EventId)
               .HasDatabaseName("IX_Sessions_EventId");

        builder.HasIndex(s => new { s.EventId, s.SessionCode })
               .IsUnique()
               .HasDatabaseName("IX_Sessions_EventId_SessionCode");
    }
}
```

---

## Migration Strategy

### Pre-Migration Checklist

- [ ] **Backup production database** before applying migrations
- [ ] **Test migrations on staging** environment first
- [ ] **Verify seed data** doesn't have duplicate active participations
- [ ] **Review generated SQL** for all migrations
- [ ] **Ensure EF Core tools updated**: `dotnet tool update --global dotnet-ef`

### Migration Execution Order

**Execute migrations in this exact order**:

1. **Migration 1**: Remove stale `Sold` and `CurrentAttendees` columns
2. **Migration 2**: Add `TicketPurchaseId` foreign key and populate existing data
3. **Migration 3**: Add unique constraint (after cleaning duplicates)
4. **Migration 4**: Add `IsMultiSession` flag for future support

### Step-by-Step Execution

#### Step 1: Generate Migrations

```bash
cd /home/chad/repos/witchcityrope/apps/api

# Migration 1: Remove stale columns
dotnet ef migrations add RemoveStaleSoldColumns

# Migration 2: Add foreign key
dotnet ef migrations add AddTicketPurchaseLinkToParticipation

# Migration 3: Add unique constraint
dotnet ef migrations add AddUniqueActiveParticipationConstraint

# Migration 4: Add multi-session support
dotnet ef migrations add AddMultiSessionSupport
```

#### Step 2: Review Generated Migrations

```bash
# View each migration file
cat Migrations/*_RemoveStaleSoldColumns.cs
cat Migrations/*_AddTicketPurchaseLinkToParticipation.cs
cat Migrations/*_AddUniqueActiveParticipationConstraint.cs
cat Migrations/*_AddMultiSessionSupport.cs

# Verify SQL that will be executed
dotnet ef migrations script --output review-migrations.sql
cat review-migrations.sql
```

#### Step 3: Test Locally

```bash
# Navigate to project root
cd /home/chad/repos/witchcityrope

# Stop containers and remove volumes (fresh database)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v

# Start containers (migrations apply automatically via DatabaseInitializationService)
# Use container-restart skill for correct startup with health checks

# Check migration logs
docker logs witchcity-api --tail 100 | grep -i migration

# Expected output:
# "Successfully applied 4 migrations"
```

#### Step 4: Verify Database Schema

```bash
# Connect to PostgreSQL
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev

# Verify TicketTypes table (Sold column removed)
\d "TicketTypes"
-- Should NOT see "Sold" column

# Verify Sessions table (CurrentAttendees column removed)
\d "Sessions"
-- Should NOT see "CurrentAttendees" column

# Verify EventParticipations table (TicketPurchaseId added)
\d "EventParticipations"
-- Should see "TicketPurchaseId | uuid |" column

# Verify foreign key constraint
\d "EventParticipations"
-- Should see FK_EventParticipations_TicketPurchases foreign key

# Verify unique index
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'EventParticipations'
  AND indexname = 'IX_EventParticipations_OneActivePerUserPerEvent';
-- Should see partial unique index WHERE Status = 1

# Exit PostgreSQL
\q
```

#### Step 5: Test Calculated Properties

```bash
# Use API to test calculated properties work correctly
curl http://localhost:5655/api/events

# Response should include:
# - ticketTypes with "quantitySold" (calculated from participations)
# - sessions with "registrationCount" (calculated from participations)
# - No references to stored "Sold" or "CurrentAttendees" fields
```

### Rollback Plan

If migrations fail or cause issues:

```bash
# Rollback to specific migration
cd /home/chad/repos/witchcityrope/apps/api

# Rollback all four migrations
dotnet ef database update <MigrationBeforeRemoveStaleSoldColumns>

# Or rollback to initial migration (nuclear option)
dotnet ef database update InitialMigration

# Restart API
cd /home/chad/repos/witchcityrope
docker-compose restart api
```

### Production Deployment Considerations

**For staging/production**:

1. **Generate idempotent SQL script**:
   ```bash
   dotnet ef migrations script --idempotent --output production-migrations.sql
   ```

2. **Review with DBA** before applying

3. **Apply during maintenance window** (if needed)

4. **Monitor application logs** after deployment:
   ```bash
   docker logs wcr-staging-api --tail 200 -f
   ```

5. **Verify health checks** pass:
   ```bash
   curl https://staging.witchcityrope.com/api/health
   ```

---

## Code Changes Map

### Files to Modify

#### Entity Models

**File**: `/apps/api/Features/Participation/Entities/EventParticipation.cs`
- **Add**: `public Guid? TicketPurchaseId { get; set; }`
- **Add**: `public TicketPurchase? TicketPurchase { get; set; }`

**File**: `/apps/api/Models/TicketType.cs`
- **Remove**: `public int Sold { get; set; }` (stored field)
- **Add**: `[NotMapped] public int Sold { get { ... } }` (calculated property)
- **Add**: `public bool IsMultiSession { get; set; } = false;`

**File**: `/apps/api/Models/Session.cs`
- **Remove**: `public int CurrentAttendees { get; set; }` (stored field)
- **Add**: `[NotMapped] public int CurrentAttendees { get { ... } }` (calculated property)

#### Entity Configurations

**File**: `/apps/api/Features/Participation/Entities/Configuration/EventParticipationConfiguration.cs`
- **Add**: Foreign key configuration for `TicketPurchaseId`
- **Add**: Unique index for one active participation per user per event

**File**: `/apps/api/Models/Configuration/TicketTypeConfiguration.cs`
- **Remove**: `Sold` column configuration
- **Add**: `IsMultiSession` property configuration

**File**: `/apps/api/Models/Configuration/SessionConfiguration.cs`
- **Remove**: `CurrentAttendees` column configuration

#### Service Layer

**File**: `/apps/api/Features/Participation/Services/ParticipationService.cs`

**Method**: `CreateTicketPurchaseAsync()` - Lines 328-544
- **Add**: Set `TicketPurchaseId` on EventParticipation after creating TicketPurchase
- **Change**: Around line 455-460 (after creating participation):
  ```csharp
  var participation = new EventParticipation(request.EventId, userId, ParticipationType.Ticket)
  {
      Notes = request.Notes,
      CreatedBy = userId,
      TicketPurchaseId = ticketPurchase.Id  // NEW: Link to purchase
  };
  ```

**Method**: `CreateRSVPAsync()` - Lines 165-326
- **Ensure**: `TicketPurchaseId` remains null for RSVP type (already correct)

**Method**: `CancelParticipationAsync()` - Lines 549-794
- **No changes needed**: Cancellation sets Status=Cancelled, calculated properties automatically exclude it

**File**: `/apps/api/Features/Events/Services/EventService.cs`

**Method**: `GetEventAsync()` - Lines 116-207
- **Remove**: In-memory calculation of `Session.CurrentAttendees` (lines 158-174)
- **Reason**: Now calculated automatically via property getter

**Method**: `GetEventsAsync()` - Lines 27-111
- **No changes needed**: Already loads EventParticipations, calculated properties work automatically

#### DTO Layer

**File**: `/apps/api/Features/Events/Models/TicketTypeDto.cs`

**Constructor** - Lines 71-153
- **Simplify**: QuantitySold calculation can now use TicketPurchaseId link instead of matching by UserId
- **Change**: Lines 97-130 can be simplified to:
  ```csharp
  if (eventParticipations != null)
  {
      // New approach: Use TicketPurchaseId link directly
      QuantitySold = eventParticipations
          .Count(ep =>
              ep.Status == ParticipationStatus.Active &&
              ep.TicketPurchase != null &&
              ep.TicketPurchase.TicketTypeId == ticketType.Id);
  }
  else
  {
      // Fallback: Count unique users with completed payments
      QuantitySold = ticketType.Purchases
          .Where(p => p.IsPaymentCompleted)
          .Select(p => p.UserId)
          .Distinct()
          .Count();
  }
  ```

**File**: `/apps/api/Features/Events/Models/SessionDto.cs`
- **No changes needed**: Already maps from `Session.CurrentAttendees`, which is now calculated

### Files to Create

#### Migrations

1. `/apps/api/Migrations/20251108_RemoveStaleSoldColumns.cs`
2. `/apps/api/Migrations/20251108_AddTicketPurchaseLinkToParticipation.cs`
3. `/apps/api/Migrations/20251108_AddUniqueActiveParticipationConstraint.cs`
4. `/apps/api/Migrations/20251108_AddMultiSessionSupport.cs`

### Files to Update (Seed Data)

**File**: `/apps/api/Services/Seeding/ParticipationSeeder.cs`

**Changes needed**:
1. **Remove duplicate active participations** - Only one ACTIVE purchase per user per event
2. **Add test cancelled purchases** - Include users with participation history
3. **Link EventParticipation to TicketPurchase** - Set TicketPurchaseId when creating

**Example Pattern**:
```csharp
// Suspension Basics Event (4 active attendees)

// User 1: Simple active purchase
var purchase1 = CreateTicketPurchase(user1, ticketType, 1, 50.00m, "Completed");
var participation1 = CreateEventParticipation(user1, event, ParticipationType.Ticket, ParticipationStatus.Active);
participation1.TicketPurchaseId = purchase1.Id;  // Link to purchase

// User 2: Active purchase + cancelled history (changed mind once)
var purchase2Cancelled = CreateTicketPurchase(user2, ticketType, 1, 50.00m, "Cancelled");
var participation2Cancelled = CreateEventParticipation(user2, event, ParticipationType.Ticket, ParticipationStatus.Cancelled);
participation2Cancelled.TicketPurchaseId = purchase2Cancelled.Id;
participation2Cancelled.CancelledAt = DateTime.UtcNow.AddDays(-7);
participation2Cancelled.CancellationReason = "Changed mind";

var purchase2Active = CreateTicketPurchase(user2, ticketType, 1, 50.00m, "Completed");
var participation2Active = CreateEventParticipation(user2, event, ParticipationType.Ticket, ParticipationStatus.Active);
participation2Active.TicketPurchaseId = purchase2Active.Id;

// User 3: Active purchase + refunded history (attended but refunded within 48hrs)
var purchase3Refunded = CreateTicketPurchase(user3, ticketType, 1, 50.00m, "Refunded");
var participation3Refunded = CreateEventParticipation(user3, event, ParticipationType.Ticket, ParticipationStatus.Refunded);
participation3Refunded.TicketPurchaseId = purchase3Refunded.Id;

var purchase3Active = CreateTicketPurchase(user3, ticketType, 1, 50.00m, "Completed");
var participation3Active = CreateEventParticipation(user3, event, ParticipationType.Ticket, ParticipationStatus.Active);
participation3Active.TicketPurchaseId = purchase3Active.Id;

// User 4: Simple active purchase
var purchase4 = CreateTicketPurchase(user4, ticketType, 1, 50.00m, "Completed");
var participation4 = CreateEventParticipation(user4, event, ParticipationType.Ticket, ParticipationStatus.Active);
participation4.TicketPurchaseId = purchase4.Id;

// Result: 4 active participations, 2 cancelled/refunded (history), realistic test data
```

---

## Testing Strategy

### Unit Tests

**File**: `/tests/unit/api/Features/Events/Models/TicketTypeDtoTests.cs` (Create)

```csharp
public class TicketTypeDtoTests
{
    [Fact]
    public void QuantitySold_CalculatesCorrectly_WithActiveParticipations()
    {
        // Arrange: Create ticket type with purchases and participations
        var ticketType = CreateTicketType();
        var purchases = new List<TicketPurchase>
        {
            CreatePurchase(userId: Guid.NewGuid(), isCompleted: true),
            CreatePurchase(userId: Guid.NewGuid(), isCompleted: true),
            CreatePurchase(userId: Guid.NewGuid(), isCompleted: false) // Pending, shouldn't count
        };
        ticketType.Purchases = purchases;

        var participations = new List<EventParticipation>
        {
            CreateParticipation(userId: purchases[0].UserId, status: Active, purchaseId: purchases[0].Id),
            CreateParticipation(userId: purchases[1].UserId, status: Active, purchaseId: purchases[1].Id),
            CreateParticipation(userId: purchases[2].UserId, status: Cancelled, purchaseId: purchases[2].Id) // Cancelled, shouldn't count
        };

        // Act
        var dto = new TicketTypeDto(ticketType, participations);

        // Assert
        Assert.Equal(2, dto.QuantitySold); // Only active, completed purchases
    }

    [Fact]
    public void QuantitySold_ExcludesCancelledParticipations()
    {
        // Arrange: User bought ticket then cancelled
        var ticketType = CreateTicketType();
        var userId = Guid.NewGuid();
        var purchase = CreatePurchase(userId: userId, isCompleted: true);
        ticketType.Purchases = new[] { purchase };

        var participations = new List<EventParticipation>
        {
            CreateParticipation(userId: userId, status: Cancelled, purchaseId: purchase.Id)
        };

        // Act
        var dto = new TicketTypeDto(ticketType, participations);

        // Assert
        Assert.Equal(0, dto.QuantitySold); // Cancelled participation doesn't count
    }
}
```

**File**: `/tests/unit/api/Models/TicketTypeTests.cs` (Create)

```csharp
public class TicketTypeCalculatedPropertiesTests
{
    [Fact]
    public void Sold_CalculatesCorrectly_FromEventParticipations()
    {
        // Arrange
        var eventEntity = CreateEvent();
        var ticketType = CreateTicketType();
        ticketType.Event = eventEntity;

        var participations = new List<EventParticipation>
        {
            CreateParticipation(status: Active, ticketTypeId: ticketType.Id),
            CreateParticipation(status: Active, ticketTypeId: ticketType.Id),
            CreateParticipation(status: Cancelled, ticketTypeId: ticketType.Id) // Shouldn't count
        };
        eventEntity.EventParticipations = participations;

        // Act
        var sold = ticketType.Sold;

        // Assert
        Assert.Equal(2, sold); // Only active participations
    }

    [Fact]
    public void Sold_ReturnsZero_WhenNoParticipationsLoaded()
    {
        // Arrange
        var ticketType = CreateTicketType();
        ticketType.Event = null; // Navigation not loaded

        // Act
        var sold = ticketType.Sold;

        // Assert
        Assert.Equal(0, sold); // Graceful handling of null navigation
    }
}
```

### Integration Tests

**File**: `/tests/integration/api/Features/Participation/ParticipationServiceTests.cs` (Update)

```csharp
public class ParticipationServiceIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task CreateTicketPurchaseAsync_SetsTicketPurchaseId_OnEventParticipation()
    {
        // Arrange
        var eventEntity = await CreateTestEvent();
        var ticketType = await CreateTestTicketType(eventEntity.Id);
        var user = await CreateTestUser();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            SelectedPrice = 50.00m
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var participation = await _context.EventParticipations
            .Include(ep => ep.TicketPurchase)
            .FirstOrDefaultAsync(ep => ep.EventId == eventEntity.Id && ep.UserId == user.Id);

        Assert.NotNull(participation);
        Assert.NotNull(participation.TicketPurchaseId); // Foreign key set
        Assert.NotNull(participation.TicketPurchase); // Navigation works
        Assert.Equal(ticketType.Id, participation.TicketPurchase.TicketTypeId);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_EnforcesUniqueActiveParticipation()
    {
        // Arrange: User already has active participation
        var eventEntity = await CreateTestEvent();
        var ticketType = await CreateTestTicketType(eventEntity.Id);
        var user = await CreateTestUser();

        // Create first purchase (should succeed)
        var request1 = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            SelectedPrice = 50.00m
        };
        await _participationService.CreateTicketPurchaseAsync(request1, user.Id, CancellationToken.None);

        // Act: Try to create second purchase for same event (should fail)
        var request2 = new CreateTicketPurchaseRequest
        {
            EventId = eventEntity.Id,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            SelectedPrice = 50.00m
        };

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _participationService.CreateTicketPurchaseAsync(request2, user.Id, CancellationToken.None);
        });
        // Unique constraint violation expected
    }

    [Fact]
    public async Task CancelParticipationAsync_UpdatesSoldCount_Automatically()
    {
        // Arrange: Create event with 2 active purchases
        var eventEntity = await CreateTestEvent();
        var ticketType = await CreateTestTicketType(eventEntity.Id);
        var user1 = await CreateTestUser();
        var user2 = await CreateTestUser();

        await CreateTestParticipation(eventEntity.Id, user1.Id, ticketType.Id, Active);
        await CreateTestParticipation(eventEntity.Id, user2.Id, ticketType.Id, Active);

        // Verify initial sold count
        var initialDto = await GetTicketTypeDto(ticketType.Id);
        Assert.Equal(2, initialDto.QuantitySold);

        // Act: Cancel user1's participation
        await _participationService.CancelParticipationAsync(eventEntity.Id, user1.Id, "Changed mind", CancellationToken.None);

        // Assert: Sold count automatically updated
        var updatedDto = await GetTicketTypeDto(ticketType.Id);
        Assert.Equal(1, updatedDto.QuantitySold); // User2 only
    }
}
```

### End-to-End Tests

**File**: `/apps/web/tests/playwright/e2e/events/capacity-calculations.spec.ts` (Create)

```typescript
test.describe('Event Capacity Calculations', () => {
  test('displays correct sold count after purchase', async ({ page }) => {
    // Navigate to event details page
    await page.goto('/admin/events/suspension-basics-workshop');

    // Verify initial sold count
    const initialSold = await page.locator('[data-testid="ticket-sold-count"]').textContent();
    expect(initialSold).toBe('4'); // From seed data

    // Purchase a ticket
    await page.click('[data-testid="purchase-ticket-btn"]');
    await page.fill('[data-testid="payment-amount"]', '50.00');
    await page.click('[data-testid="confirm-purchase"]');

    // Wait for purchase to complete
    await page.waitForSelector('[data-testid="purchase-success"]');

    // Verify sold count updated
    await page.reload();
    const updatedSold = await page.locator('[data-testid="ticket-sold-count"]').textContent();
    expect(updatedSold).toBe('5'); // Incremented
  });

  test('displays correct sold count after cancellation', async ({ page }) => {
    // Navigate to event details page
    await page.goto('/admin/events/suspension-basics-workshop');

    // Verify initial sold count
    const initialSold = await page.locator('[data-testid="ticket-sold-count"]').textContent();

    // Navigate to participations list
    await page.goto('/admin/events/suspension-basics-workshop/participations');

    // Cancel a participation
    await page.click('[data-testid="cancel-participation-btn"]');
    await page.fill('[data-testid="cancellation-reason"]', 'Test cancellation');
    await page.click('[data-testid="confirm-cancel"]');

    // Wait for cancellation to complete
    await page.waitForSelector('[data-testid="cancel-success"]');

    // Navigate back to event details
    await page.goto('/admin/events/suspension-basics-workshop');

    // Verify sold count decreased
    const updatedSold = await page.locator('[data-testid="ticket-sold-count"]').textContent();
    expect(Number(updatedSold)).toBe(Number(initialSold) - 1);
  });
});
```

### Manual Testing Checklist

- [ ] **Purchase ticket** → Verify sold count increments
- [ ] **Cancel participation** → Verify sold count decrements
- [ ] **Refund ticket** → Verify sold count decrements
- [ ] **Multi-session ticket** → Verify capacity counts toward all sessions
- [ ] **Social event RSVP** → Verify capacity based on RSVPs
- [ ] **Social event ticket purchase** → Verify auto-RSVP created
- [ ] **Session capacity display** → Verify shows current attendees correctly
- [ ] **Ticket type sold out** → Verify displays correctly when Available = Sold
- [ ] **Duplicate purchase prevention** → Verify unique constraint blocks duplicate active participations
- [ ] **Database direct query** → Verify calculated properties match raw data

---

## Future Support: Multi-Ticket Purchases

### Phase 1: Current Design (This Proposal)

**Supports**:
- One ticket per purchase (Quantity field exists but always 1)
- One active participation per user per event
- Clear foreign key relationship (EventParticipation → TicketPurchase)

**Database Ready For**:
- TicketPurchase.Quantity > 1 in future
- Multiple EventParticipations pointing to same TicketPurchaseId

### Phase 2: Multi-Ticket Purchase Implementation (Future)

**Business Requirements**:
1. User can buy multiple tickets in single transaction
2. Each ticket must be assigned to:
   - An existing user (via UserId), OR
   - A guest (via Name + Email)
3. Each ticket = one person = one seat in capacity
4. Purchaser can manage all tickets they bought

**Database Changes Needed**:

**Add TicketAssignment Table**:
```sql
CREATE TABLE "TicketAssignments" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "EventParticipationId" uuid NOT NULL,  -- Links to EventParticipation
    "TicketPurchaseId" uuid NOT NULL,      -- Links to payment transaction
    "AssignedToUserId" uuid NULL,          -- If assigned to existing user
    "GuestName" text NULL,                 -- If assigned to guest
    "GuestEmail" text NULL,                -- If assigned to guest
    "QRCodeToken" text NOT NULL,           -- Unique token for check-in
    "AssignedAt" timestamptz NOT NULL,
    "AssignedBy" uuid NOT NULL,

    CONSTRAINT "FK_TicketAssignments_EventParticipations"
        FOREIGN KEY ("EventParticipationId") REFERENCES "EventParticipations"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TicketAssignments_TicketPurchases"
        FOREIGN KEY ("TicketPurchaseId") REFERENCES "TicketPurchases"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TicketAssignments_Users"
        FOREIGN KEY ("AssignedToUserId") REFERENCES "Users"("Id") ON DELETE SET NULL,
    CONSTRAINT "CHK_TicketAssignments_AssignedTo"
        CHECK (("AssignedToUserId" IS NOT NULL) OR ("GuestName" IS NOT NULL AND "GuestEmail" IS NOT NULL))
);
```

**Service Layer Changes**:

```csharp
public async Task<Result> CreateTicketPurchaseAsync(CreateMultiTicketPurchaseRequest request, Guid userId, CancellationToken ct)
{
    // 1. Create TicketPurchase with Quantity = request.TicketCount
    var purchase = new TicketPurchase
    {
        TicketTypeId = request.TicketTypeId,
        UserId = userId,
        Quantity = request.TicketCount,  // Multiple tickets
        TotalPrice = request.TotalPrice,
        PaymentStatus = "Completed"
    };

    // 2. Create one EventParticipation per ticket
    for (int i = 0; i < request.TicketCount; i++)
    {
        var participation = new EventParticipation(request.EventId, userId, ParticipationType.Ticket)
        {
            TicketPurchaseId = purchase.Id  // All link to same purchase
        };

        var assignment = new TicketAssignment
        {
            EventParticipationId = participation.Id,
            TicketPurchaseId = purchase.Id,
            AssignedToUserId = request.Assignments[i].UserId,      // Or null for guest
            GuestName = request.Assignments[i].GuestName,          // Or null for user
            GuestEmail = request.Assignments[i].GuestEmail,        // Or null for user
            QRCodeToken = GenerateUniqueToken(),
            AssignedBy = userId
        };

        _context.EventParticipations.Add(participation);
        _context.TicketAssignments.Add(assignment);
    }

    // 3. Sold count automatically calculates correctly (counts participations)
    // No manual increment needed!
}
```

**Capacity Calculation** (Already Works):
```csharp
// TicketType.Sold property (from this design)
public int Sold
{
    get
    {
        // Counts ALL active participations linked to this ticket type
        // Works for single-ticket AND multi-ticket purchases!
        return Event.EventParticipations
            .Count(ep =>
                ep.Status == ParticipationStatus.Active &&
                ep.TicketPurchase != null &&
                ep.TicketPurchase.TicketTypeId == Id);
    }
}

// Example:
// User1 buys 3 tickets → Creates 3 EventParticipations → Sold count = 3 ✓
// User2 buys 1 ticket → Creates 1 EventParticipation → Sold count = 4 ✓
// User1 cancels 1 ticket → Sets 1 participation.Status=Cancelled → Sold count = 3 ✓
```

**No Code Changes Needed in**:
- TicketType.Sold calculation (already counts participations)
- Session.CurrentAttendees calculation (already counts participations)
- Cancellation logic (already updates participation status)
- DTO calculations (already use participations)

**This Design Is Future-Proof**: Self-healing calculations work for both single-ticket and multi-ticket scenarios!

---

## Performance Considerations

### Index Strategy

**Existing Indexes** (Keep):
```sql
-- EventParticipations performance indexes
CREATE INDEX "IX_EventParticipations_EventId" ON "EventParticipations"("EventId");
CREATE INDEX "IX_EventParticipations_UserId" ON "EventParticipations"("UserId");
CREATE INDEX "IX_EventParticipations_EventId_Status" ON "EventParticipations"("EventId", "Status");
```

**New Indexes** (Add):
```sql
-- Foreign key performance
CREATE INDEX "IX_EventParticipations_TicketPurchaseId" ON "EventParticipations"("TicketPurchaseId");

-- Unique constraint (automatically creates index)
CREATE UNIQUE INDEX "IX_EventParticipations_OneActivePerUserPerEvent"
ON "EventParticipations" ("EventId", "UserId", "ParticipationType")
WHERE "Status" = 1;
```

### Query Performance Analysis

**Current Query** (TicketTypeDto constructor):
```csharp
// With new foreign key link, query is simpler and faster
QuantitySold = eventParticipations
    .Count(ep =>
        ep.Status == ParticipationStatus.Active &&
        ep.TicketPurchase != null &&
        ep.TicketPurchase.TicketTypeId == ticketType.Id);

// Uses indexes:
// 1. IX_EventParticipations_EventId_Status (filters Active)
// 2. IX_EventParticipations_TicketPurchaseId (joins to TicketPurchase)
```

**Old Query** (Without foreign key):
```csharp
// Had to match by UserId, requiring more complex joins
var participationLookup = eventParticipations
    .Where(ep => ep.Status == Active)
    .Select(ep => ep.UserId)
    .ToHashSet();

QuantitySold = ticketType.Purchases
    .Where(p => p.IsPaymentCompleted && participationLookup.Contains(p.UserId))
    .Select(p => p.UserId)
    .Distinct()
    .Count();
```

**Performance Improvement**:
- ✅ Fewer joins (direct FK relationship)
- ✅ Better index utilization
- ✅ Simpler query execution plan
- ✅ Less memory (no HashSet needed)

### Caching Strategy

**Not Recommended**: Caching sold counts
- **Reason**: Self-healing calculations are already fast
- **Risk**: Cache invalidation complexity
- **Better**: Optimize queries with indexes

**Recommended**: Cache event listings
```csharp
// Cache entire event list for 5 minutes
var events = await _cache.GetOrCreateAsync("events-list", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await _eventService.GetEventsAsync();
});
```

### Load Testing Targets

**Acceptable Performance** (with 1000 events, 10,000 participations):
- Event list query: < 500ms
- Single event details: < 200ms
- Purchase operation: < 1000ms
- Cancellation operation: < 500ms

**Monitor with**:
```csharp
// Add logging in EventService
using (_logger.BeginScope(new { Operation = "GetEventAsync", EventId = id }))
{
    var stopwatch = Stopwatch.StartNew();
    var result = await _context.Events...ToListAsync();
    stopwatch.Stop();
    _logger.LogInformation("Query completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    return result;
}
```

---

## Security & Data Integrity

### Constraints Summary

**Primary Keys**:
- EventParticipations.Id
- TicketPurchases.Id
- TicketTypes.Id
- Sessions.Id

**Foreign Keys**:
- EventParticipations.EventId → Events.Id (CASCADE)
- EventParticipations.UserId → Users.Id (CASCADE)
- EventParticipations.TicketPurchaseId → TicketPurchases.Id (SET NULL)
- TicketPurchases.TicketTypeId → TicketTypes.Id (CASCADE)
- TicketTypes.EventId → Events.Id (CASCADE)
- Sessions.EventId → Events.Id (CASCADE)

**Unique Constraints**:
- EventParticipations: (EventId, UserId, ParticipationType) WHERE Status=Active

**Check Constraints**:
- ParticipationType IN (1, 2)
- ParticipationStatus IN (1, 2, 3, 4)

### Data Validation

**Application Layer**:
```csharp
// Prevent selling beyond Available quantity
if (ticketType.Sold >= ticketType.Available)
{
    return Result.Failure("Ticket type is sold out");
}

// Prevent duplicate active participations (belt-and-suspenders with DB constraint)
var existingParticipation = await _context.EventParticipations
    .AnyAsync(ep =>
        ep.EventId == eventId &&
        ep.UserId == userId &&
        ep.ParticipationType == participationType &&
        ep.Status == Active);

if (existingParticipation)
{
    return Result.Failure("User already has an active participation for this event");
}
```

**Database Layer** (Defense in Depth):
- Unique index prevents duplicates even if application check fails
- Foreign keys prevent orphaned records
- Check constraints enforce valid enum values

### Audit Trail

**Existing**:
- EventParticipation.CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
- EventParticipation.CancelledAt, CancellationReason
- ParticipationHistory table tracks all changes

**Recommendation**: No changes needed, audit trail is comprehensive

---

## Monitoring & Health Checks

### Metrics to Track

**Key Metrics**:
- Sold count calculation time (should be < 50ms)
- Participation creation success rate
- Unique constraint violations (should be rare)
- Cancelled participation rate
- Session capacity utilization

**Monitoring Code**:
```csharp
public class ParticipationMetrics
{
    private readonly ILogger<ParticipationMetrics> _logger;

    public void TrackPurchase(Guid eventId, TimeSpan duration, bool success)
    {
        _logger.LogInformation(
            "Purchase {Status} for event {EventId} in {DurationMs}ms",
            success ? "succeeded" : "failed",
            eventId,
            duration.TotalMilliseconds);
    }

    public void TrackUniqueConstraintViolation(Guid eventId, Guid userId)
    {
        _logger.LogWarning(
            "Duplicate participation prevented for user {UserId} on event {EventId}",
            userId,
            eventId);
    }
}
```

### Health Check Endpoint

**Add to existing health checks**:
```csharp
services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        // Existing database connectivity check
    })
    .AddCheck("participation-integrity", async () =>
    {
        // Check for orphaned participations
        var orphanedCount = await _context.EventParticipations
            .Where(ep => ep.ParticipationType == Ticket && ep.TicketPurchaseId == null)
            .CountAsync();

        if (orphanedCount > 0)
        {
            return HealthCheckResult.Degraded($"{orphanedCount} orphaned participations found");
        }

        return HealthCheckResult.Healthy();
    });
```

---

## Documentation Updates Required

### Update These Files

1. **File Registry** (`/docs/architecture/file-registry.md`)
   - Add all migration files created
   - Add test files created
   - Add updated configuration files

2. **Functional Area Master Index** (`/docs/architecture/functional-area-master-index.md`)
   - Update Events section with link to this design document

3. **Database Migrations Guide** (`/docs/standards-processes/backend/database-migrations-guide.md`)
   - Add example of partial unique index (PostgreSQL feature)
   - Add example of calculated properties pattern

4. **Entity Framework Patterns** (`/docs/standards-processes/development-standards/entity-framework-patterns.md`)
   - Add section on calculated properties vs stored fields
   - Add section on partial unique indexes

5. **Backend Developer Lessons Learned** (`/docs/lessons-learned/backend-developer-lessons-learned.md`)
   - Add lesson: "Use calculated properties for derived data instead of maintaining stored values"
   - Add lesson: "Partial unique indexes in PostgreSQL enable business rule enforcement"

### Create New Documentation

1. **Capacity Calculation Reference** (`/docs/functional-areas/events/technical-reference/capacity-calculations.md`)
   - Comprehensive guide to how sold counts and capacity are calculated
   - Include examples, edge cases, and troubleshooting

---

## Glossary

**EventParticipation**: Record of a person's attendance at an event (RSVP or Ticket type)

**TicketPurchase**: Financial transaction record for ticket payment

**ParticipationType**: Enum (RSVP=1, Ticket=2) indicating how user is participating

**ParticipationStatus**: Enum (Active=1, Cancelled=2, Refunded=3, Waitlisted=4) indicating current state

**Sold Count**: Number of active participations for a ticket type (calculated, not stored)

**Current Attendees**: Number of active participations for a session (calculated, not stored)

**Capacity**: Maximum number of attendees allowed (stored)

**Available**: Maximum number of tickets that can be sold (stored)

**Self-Healing**: Calculations that always produce correct results regardless of previous operation failures

**Calculated Property**: Entity property with getter logic, not persisted to database (marked [NotMapped])

**Partial Unique Index**: PostgreSQL index with WHERE clause, enforcing uniqueness only for subset of rows

---

## References

### Internal Documents

- **Current Implementation Analysis**: `/docs/functional-areas/events/research/current-implementation-analysis-2025-11-08.md`
- **Entity Framework Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
- **Database Migrations Guide**: `/docs/standards-processes/backend/database-migrations-guide.md`
- **Database Designer Lessons**: `/docs/lessons-learned/database-designer-lessons-learned.md`

### PostgreSQL Documentation

- **Partial Indexes**: https://www.postgresql.org/docs/current/indexes-partial.html
- **Unique Constraints**: https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-UNIQUE-CONSTRAINTS

### Entity Framework Core Documentation

- **NotMapped Attribute**: https://docs.microsoft.com/ef/core/modeling/entity-properties#excluding-from-model
- **Query Filters**: https://docs.microsoft.com/ef/core/querying/filters
- **Partial Indexes with EF Core**: https://www.npgsql.org/efcore/modeling/indexes.html#partial-indexes

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-08 | Database Designer Agent | Initial comprehensive design document |

---

## Next Steps

1. **Phase 1: Review**
   - [ ] Review design with team
   - [ ] Validate business rules with stakeholders
   - [ ] Confirm migration strategy acceptable

2. **Phase 2: Implementation**
   - [ ] Create migrations as outlined
   - [ ] Update entity models and configurations
   - [ ] Update service layer code
   - [ ] Fix seed data violations

3. **Phase 3: Testing**
   - [ ] Run unit tests
   - [ ] Run integration tests
   - [ ] Run E2E tests
   - [ ] Perform manual testing

4. **Phase 4: Deployment**
   - [ ] Deploy to staging
   - [ ] Verify on staging
   - [ ] Monitor performance
   - [ ] Deploy to production

5. **Phase 5: Documentation**
   - [ ] Update file registry
   - [ ] Update functional area index
   - [ ] Update lessons learned
   - [ ] Create capacity calculation reference

---

**END OF DESIGN DOCUMENT**
