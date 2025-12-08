# Database Design: Per-Session Ticket Tracking

<!-- Last Updated: 2025-12-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer Agent -->
<!-- Status: Final Design - Ready for Implementation -->

## Executive Summary

**Goal**: Enable ONE ticket per SESSION per user (currently ONE ticket per EVENT per user).

**Recommended Approach**: **Option A - Add SessionId to EventAttendance**

**Rationale**: Simple queries, clear data model, minimal complexity, and aligns with existing patterns.

**Key Schema Change**: Add nullable `SessionId` column to `EventAttendance` table.

**Migration Strategy**: Backfill existing records using TicketPurchase → TicketType → Sessions relationship.

**Impact**: Low risk, high clarity, excellent query performance.

---

## Problem Statement

### Current State

**EventAttendance Schema**:
```csharp
public class EventAttendance
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }        // Links to Event only
    public Guid UserId { get; set; }
    public AttendanceType AttendanceType { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid? TicketPurchaseId { get; set; }
    // ... no SessionId field
}
```

**Current Validation** (AttendanceService.cs, lines 562-575):
```csharp
// Checks: "User already has ticket for THIS EVENT"
var existingAttendance = await _context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.EventId == eventId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active);
```

**Problem**: User cannot purchase tickets for different sessions of the same event.

### Target State

**Needed Validation**:
```csharp
// Checks: "User already has ticket for THESE SPECIFIC SESSIONS"
var overlappingSessions = await GetSessionsFromTicketType(ticketTypeId);
var existingAttendance = await _context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.SessionId.HasValue &&
        overlappingSessions.Contains(ea.SessionId.Value) &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active);
```

**Goal**: User can purchase "Friday Only" ticket, then later purchase "Sunday Only" ticket (different sessions).

---

## Options Analysis

### Option A: Single SessionId on EventAttendance (RECOMMENDED)

**Schema Change**:
```csharp
public class EventAttendance
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    // NEW FIELD
    public Guid? SessionId { get; set; }  // Nullable for backward compatibility

    public AttendanceType AttendanceType { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid? TicketPurchaseId { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Session? Session { get; set; }  // NEW
}
```

**Multi-Session Ticket Handling**: Create multiple EventAttendance records.

**Example**:
- User purchases "Full Weekend" ticket (covers Friday, Saturday, Sunday sessions)
- System creates 3 EventAttendance records:
  - Record 1: SessionId = Friday session ID
  - Record 2: SessionId = Saturday session ID
  - Record 3: SessionId = Sunday session ID
  - All 3 records: Same TicketPurchaseId, same UserId, same EventId

**SQL Migration**:
```sql
-- Add SessionId column (nullable for backward compatibility)
ALTER TABLE "EventAttendances"
ADD COLUMN "SessionId" UUID NULL;

-- Add foreign key constraint
ALTER TABLE "EventAttendances"
ADD CONSTRAINT "FK_EventAttendances_Sessions_SessionId"
FOREIGN KEY ("SessionId") REFERENCES "Sessions"("Id")
ON DELETE CASCADE;

-- Create index for performance
CREATE INDEX "IX_EventAttendances_SessionId"
ON "EventAttendances"("SessionId");

-- Create composite index for duplicate detection
CREATE INDEX "IX_EventAttendances_UserId_SessionId_Status"
ON "EventAttendances"("UserId", "SessionId", "Status");
```

**Pros**:
- ✅ Simple queries: Direct JOIN on SessionId
- ✅ Clear data model: One attendance = one session
- ✅ Easy capacity calculations: COUNT WHERE SessionId = X
- ✅ Straightforward validation: Check for duplicate SessionId
- ✅ Backward compatible: NULL SessionId for single-session events
- ✅ Performance: Efficient indexes on SessionId

**Cons**:
- ❌ More records for multi-session tickets (3 records for weekend pass vs. 1 record)
- ❌ Refund logic must cancel all related records (minor complexity)
- ❌ Requires database migration

**Query Examples**:
```csharp
// "Does user have ticket for this session?"
var hasTicket = await _context.EventAttendances
    .AnyAsync(ea =>
        ea.SessionId == sessionId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active);

// "How many tickets sold for this session?"
var sold = await _context.EventAttendances
    .CountAsync(ea =>
        ea.SessionId == sessionId &&
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.Status == AttendanceStatus.Active);

// "What sessions does user's ticket cover?"
var sessions = await _context.EventAttendances
    .Where(ea =>
        ea.TicketPurchaseId == purchaseId &&
        ea.Status == AttendanceStatus.Active)
    .Select(ea => ea.Session)
    .ToListAsync();
```

**Performance**: Excellent - direct index lookups.

---

### Option B: SessionIds Collection via Join Table

**Schema Changes**:
```csharp
// NEW join table
public class EventAttendanceSession
{
    public Guid EventAttendanceId { get; set; }
    public Guid SessionId { get; set; }

    public EventAttendance EventAttendance { get; set; } = null!;
    public Session Session { get; set; } = null!;
}

public class EventAttendance
{
    // Existing fields...

    // NEW navigation property
    public ICollection<EventAttendanceSession> AttendanceSessions { get; set; }
        = new List<EventAttendanceSession>();
}
```

**Multi-Session Ticket Handling**: One EventAttendance with multiple AttendanceSession records.

**Example**:
- User purchases "Full Weekend" ticket
- System creates:
  - 1 EventAttendance record
  - 3 EventAttendanceSession records (Friday, Saturday, Sunday)

**SQL Migration**:
```sql
-- Create join table
CREATE TABLE "EventAttendanceSessions" (
    "EventAttendanceId" UUID NOT NULL,
    "SessionId" UUID NOT NULL,
    CONSTRAINT "PK_EventAttendanceSessions"
        PRIMARY KEY ("EventAttendanceId", "SessionId"),
    CONSTRAINT "FK_EventAttendanceSessions_EventAttendances"
        FOREIGN KEY ("EventAttendanceId")
        REFERENCES "EventAttendances"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventAttendanceSessions_Sessions"
        FOREIGN KEY ("SessionId")
        REFERENCES "Sessions"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EventAttendanceSessions_SessionId"
ON "EventAttendanceSessions"("SessionId");
```

**Pros**:
- ✅ One attendance record per ticket (cleaner conceptually)
- ✅ Easy to see all sessions covered by one ticket
- ✅ Refund logic simpler (cancel one record)

**Cons**:
- ❌ More complex queries (always requires JOIN)
- ❌ Duplicate detection requires subquery or EXISTS
- ❌ Capacity calculations need aggregate queries
- ❌ Additional table to maintain
- ❌ More complex EF Core configuration

**Query Examples**:
```csharp
// "Does user have ticket for this session?"
var hasTicket = await _context.EventAttendances
    .AnyAsync(ea =>
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.AttendanceSessions.Any(eas => eas.SessionId == sessionId));

// "How many tickets sold for this session?"
var sold = await _context.EventAttendanceSessions
    .Where(eas => eas.SessionId == sessionId)
    .Select(eas => eas.EventAttendance)
    .Where(ea =>
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.Status == AttendanceStatus.Active)
    .CountAsync();

// "What sessions does user's ticket cover?"
var sessions = await _context.EventAttendances
    .Where(ea => ea.TicketPurchaseId == purchaseId)
    .SelectMany(ea => ea.AttendanceSessions)
    .Select(eas => eas.Session)
    .ToListAsync();
```

**Performance**: Good, but requires JOINs for all queries.

---

### Option C: Derive from TicketType.Sessions (No Schema Change)

**Approach**: Don't add SessionId to EventAttendance. Always derive sessions from TicketType.

**Query Pattern**:
```csharp
// "Does user have ticket for this session?"
var hasTicket = await _context.EventAttendances
    .Include(ea => ea.TicketPurchase)
        .ThenInclude(tp => tp.TicketType)
            .ThenInclude(tt => tt.Sessions)
    .AnyAsync(ea =>
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.TicketPurchase!.TicketType.Sessions.Any(s => s.Id == sessionId));
```

**Pros**:
- ✅ No schema changes needed
- ✅ No data migration required
- ✅ Single source of truth (TicketType.Sessions)

**Cons**:
- ❌ Complex queries with multiple ThenInclude()
- ❌ Performance issues (4-table JOIN for simple check)
- ❌ RSVP attendances have no TicketPurchase (cannot determine sessions)
- ❌ Capacity calculations extremely complex
- ❌ Difficult to handle session changes after purchase
- ❌ Breaks if TicketType.Sessions modified after purchase

**Fatal Flaw**: RSVP attendances don't have TicketPurchaseId. How do we know which sessions an RSVP covers?

**Performance**: Poor - requires deep JOINs for every query.

**Verdict**: ❌ Not recommended due to complexity and RSVP handling issues.

---

## Recommended Approach: Option A

**Decision**: Add `SessionId` to EventAttendance table.

**Reasons**:
1. **Simplicity**: Direct queries, clear data model
2. **Performance**: Efficient indexed lookups
3. **Backward Compatibility**: NULL SessionId for single-session events
4. **RSVP Support**: Works for both Ticket and RSVP attendance types
5. **Future-Proof**: Easy to query, modify, and report on
6. **Consistency**: Follows existing EventId pattern

**Trade-off Accepted**: More records for multi-session tickets (3 records for weekend pass). This is acceptable because:
- Database storage is cheap
- Query simplicity is more valuable
- Cancellation logic already handles related records (see existing code)

---

## Detailed Schema Design

### EventAttendance Table Changes

**New Column**:
```sql
ALTER TABLE "EventAttendances"
ADD COLUMN "SessionId" UUID NULL;
```

**Foreign Key**:
```sql
ALTER TABLE "EventAttendances"
ADD CONSTRAINT "FK_EventAttendances_Sessions_SessionId"
FOREIGN KEY ("SessionId") REFERENCES "Sessions"("Id")
ON DELETE CASCADE;
```

**Indexes**:
```sql
-- Index for session-specific queries
CREATE INDEX "IX_EventAttendances_SessionId"
ON "EventAttendances"("SessionId");

-- Composite index for duplicate detection
CREATE INDEX "IX_EventAttendances_UserId_SessionId_Status"
ON "EventAttendances"("UserId", "SessionId", "Status");

-- Composite index for capacity calculations
CREATE INDEX "IX_EventAttendances_SessionId_Status_AttendanceType"
ON "EventAttendances"("SessionId", "Status", "AttendanceType");
```

**Check Constraints**:
```sql
-- Ensure SessionId is set for new records (allow NULL for backward compatibility)
-- No constraint needed - NULL is valid for single-session events
```

---

### Entity Framework Configuration

**EventAttendance.cs** (add property):
```csharp
public class EventAttendance
{
    // Existing properties...

    /// <summary>
    /// Session the user is attending (NULL for single-session events or legacy data)
    /// For multi-session tickets, multiple EventAttendance records are created.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Navigation property to session
    /// </summary>
    public Session? Session { get; set; }
}
```

**EventAttendanceConfiguration.cs** (add configuration):
```csharp
public class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
{
    public void Configure(EntityTypeBuilder<EventAttendance> builder)
    {
        // Existing configuration...

        // Session relationship (nullable for backward compatibility)
        builder.HasOne(ea => ea.Session)
            .WithMany()
            .HasForeignKey(ea => ea.SessionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(ea => ea.SessionId)
            .HasDatabaseName("IX_EventAttendances_SessionId");

        builder.HasIndex(ea => new { ea.UserId, ea.SessionId, ea.Status })
            .HasDatabaseName("IX_EventAttendances_UserId_SessionId_Status");

        builder.HasIndex(ea => new { ea.SessionId, ea.Status, ea.AttendanceType })
            .HasDatabaseName("IX_EventAttendances_SessionId_Status_AttendanceType");
    }
}
```

---

## Migration Strategy

### Phase 1: Schema Migration

**EF Core Migration**:
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddSessionIdToEventAttendance --output-dir Data/Migrations
```

**Expected Migration File**:
```csharp
public partial class AddSessionIdToEventAttendance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SessionId",
            table: "EventAttendances",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventAttendances_SessionId",
            table: "EventAttendances",
            column: "SessionId");

        migrationBuilder.CreateIndex(
            name: "IX_EventAttendances_UserId_SessionId_Status",
            table: "EventAttendances",
            columns: new[] { "UserId", "SessionId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_EventAttendances_SessionId_Status_AttendanceType",
            table: "EventAttendances",
            columns: new[] { "SessionId", "Status", "AttendanceType" });

        migrationBuilder.AddForeignKey(
            name: "FK_EventAttendances_Sessions_SessionId",
            table: "EventAttendances",
            column: "SessionId",
            principalTable: "Sessions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EventAttendances_Sessions_SessionId",
            table: "EventAttendances");

        migrationBuilder.DropIndex(
            name: "IX_EventAttendances_SessionId",
            table: "EventAttendances");

        migrationBuilder.DropIndex(
            name: "IX_EventAttendances_UserId_SessionId_Status",
            table: "EventAttendances");

        migrationBuilder.DropIndex(
            name: "IX_EventAttendances_SessionId_Status_AttendanceType",
            table: "EventAttendances");

        migrationBuilder.DropColumn(
            name: "SessionId",
            table: "EventAttendances");
    }
}
```

---

### Phase 2: Data Backfill

**Goal**: Populate SessionId for existing EventAttendance records.

**Strategy**:
1. For each EventAttendance record with NULL SessionId
2. Determine which session(s) it should cover
3. For single-session tickets: Update SessionId
4. For multi-session tickets: Create additional EventAttendance records

**Backfill Script**:
```csharp
public class BackfillEventAttendanceSessionIds
{
    public async Task BackfillAsync(WitchCityRopeIdentityDbContext context)
    {
        // Get all EventAttendance records with NULL SessionId
        var attendancesWithoutSession = await context.EventAttendances
            .Include(ea => ea.TicketPurchase)
                .ThenInclude(tp => tp.TicketType)
                    .ThenInclude(tt => tt.Sessions)
            .Include(ea => ea.Event)
                .ThenInclude(e => e.Sessions)
            .Where(ea => ea.SessionId == null)
            .ToListAsync();

        foreach (var attendance in attendancesWithoutSession)
        {
            // Determine sessions this attendance covers
            List<Session> coveredSessions;

            if (attendance.TicketPurchaseId.HasValue &&
                attendance.TicketPurchase?.TicketType?.Sessions != null)
            {
                // Has ticket purchase - use ticket type's sessions
                coveredSessions = attendance.TicketPurchase.TicketType.Sessions.ToList();
            }
            else if (attendance.Event?.Sessions != null)
            {
                // RSVP or ticket purchase data missing - use all event sessions
                coveredSessions = attendance.Event.Sessions.ToList();
            }
            else
            {
                // No session data - skip (edge case for corrupted data)
                continue;
            }

            if (coveredSessions.Count == 0)
            {
                // No sessions found - skip
                continue;
            }
            else if (coveredSessions.Count == 1)
            {
                // Single session - just update SessionId
                attendance.SessionId = coveredSessions[0].Id;
            }
            else
            {
                // Multi-session - update first, create additional records
                attendance.SessionId = coveredSessions[0].Id;

                for (int i = 1; i < coveredSessions.Count; i++)
                {
                    var additionalAttendance = new EventAttendance
                    {
                        Id = Guid.NewGuid(),
                        EventId = attendance.EventId,
                        UserId = attendance.UserId,
                        SessionId = coveredSessions[i].Id,
                        AttendanceType = attendance.AttendanceType,
                        Status = attendance.Status,
                        TicketPurchaseId = attendance.TicketPurchaseId,
                        CreatedAt = attendance.CreatedAt,
                        CancelledAt = attendance.CancelledAt,
                        CancellationReason = attendance.CancellationReason,
                        Notes = $"Auto-created from multi-session backfill. Original attendance: {attendance.Id}",
                        Metadata = attendance.Metadata,
                        CreatedBy = attendance.CreatedBy,
                        UpdatedBy = attendance.UpdatedBy,
                        UpdatedAt = DateTime.UtcNow,
                        EventWaiverAccepted = attendance.EventWaiverAccepted,
                        EventWaiverAcceptedAt = attendance.EventWaiverAcceptedAt
                    };

                    context.EventAttendances.Add(additionalAttendance);
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
```

**Execution Plan**:
1. Run backfill script in staging environment
2. Verify data integrity (all records have SessionId)
3. Verify capacity calculations match pre-migration values
4. Run backfill script in production during maintenance window

**Validation Queries**:
```sql
-- Check for NULL SessionIds (should return 0 after backfill)
SELECT COUNT(*) FROM "EventAttendances" WHERE "SessionId" IS NULL;

-- Verify multi-session tickets created multiple records
SELECT "TicketPurchaseId", COUNT(*) as RecordCount
FROM "EventAttendances"
WHERE "TicketPurchaseId" IS NOT NULL
GROUP BY "TicketPurchaseId"
HAVING COUNT(*) > 1
ORDER BY RecordCount DESC;

-- Verify session capacity calculations
SELECT
    s."Id",
    s."Name",
    COUNT(ea."Id") as AttendeeCount
FROM "Sessions" s
LEFT JOIN "EventAttendances" ea
    ON ea."SessionId" = s."Id"
    AND ea."Status" = 'Active'
    AND ea."AttendanceType" = 'Ticket'
GROUP BY s."Id", s."Name"
ORDER BY s."Name";
```

---

### Phase 3: Backward Compatibility

**Single-Session Events**: SessionId remains NULL (optional).

**Logic**:
```csharp
// ALLOWED: SessionId can be NULL for single-session events
if (event.Sessions.Count == 1)
{
    // Option 1: Set SessionId to the single session
    attendance.SessionId = event.Sessions.First().Id;

    // Option 2: Leave SessionId as NULL (backward compatible)
    attendance.SessionId = null;
}
```

**Validation Logic**: Handle both NULL and non-NULL SessionId.

```csharp
// Check for duplicate attendance
var hasExistingAttendance = await _context.EventAttendances
    .AnyAsync(ea =>
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        (
            // New multi-session logic
            (sessionId.HasValue && ea.SessionId == sessionId) ||
            // Old single-session logic (SessionId NULL, EventId match)
            (!sessionId.HasValue && !ea.SessionId.HasValue && ea.EventId == eventId)
        ));
```

---

## Business Logic Changes

### AttendanceService.cs Updates

**Current Validation** (lines 562-575):
```csharp
// OLD: Event-level duplicate check
var existingAttendance = await _context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.EventId == eventId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active);

if (existingAttendance != null)
{
    throw new InvalidOperationException("User already has a ticket for this event");
}
```

**New Validation** (session-level):
```csharp
// NEW: Session-level duplicate check
// Get sessions covered by the ticket type user is purchasing
var ticketType = await _context.TicketTypes
    .Include(tt => tt.Sessions)
    .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId);

if (ticketType == null)
{
    throw new NotFoundException("Ticket type not found");
}

var requestedSessionIds = ticketType.Sessions.Select(s => s.Id).ToList();

// Check if user already has a ticket for ANY of these sessions
var overlappingAttendance = await _context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.SessionId.HasValue &&
        requestedSessionIds.Contains(ea.SessionId.Value));

if (overlappingAttendance != null)
{
    var overlappingSession = ticketType.Sessions
        .First(s => s.Id == overlappingAttendance.SessionId);

    throw new InvalidOperationException(
        $"User already has a ticket that includes the {overlappingSession.Name} session");
}
```

**Create Attendance for Multi-Session Tickets**:
```csharp
// For each session in the ticket type, create an EventAttendance record
foreach (var session in ticketType.Sessions)
{
    var attendance = new EventAttendance
    {
        Id = Guid.NewGuid(),
        EventId = eventId,
        UserId = userId,
        SessionId = session.Id,  // NEW
        AttendanceType = AttendanceType.Ticket,
        Status = AttendanceStatus.Active,
        TicketPurchaseId = ticketPurchaseId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = userId,
        UpdatedBy = userId,
        EventWaiverAccepted = waiverAccepted,
        EventWaiverAcceptedAt = waiverAccepted ? DateTime.UtcNow : null
    };

    _context.EventAttendances.Add(attendance);
}

await _context.SaveChangesAsync();
```

**Cancellation Logic**: Cancel all related EventAttendance records.

```csharp
// Get all EventAttendance records for this ticket purchase
var attendances = await _context.EventAttendances
    .Where(ea => ea.TicketPurchaseId == ticketPurchaseId)
    .ToListAsync();

// Cancel all of them
foreach (var attendance in attendances)
{
    attendance.Cancel(cancellationReason);
}

await _context.SaveChangesAsync();
```

---

### Capacity Calculation Updates

**Session.CurrentAttendees** (already correctly calculates per-session):
```csharp
[NotMapped]
public int CurrentAttendees
{
    get
    {
        if (Event?.EventAttendances == null) return 0;

        // CURRENT: Uses TicketType.Sessions many-to-many (works but complex)
        // FUTURE: Use SessionId directly (simpler after migration)
        return Event.EventAttendances.Count(ea =>
            ea.Status == AttendanceStatus.Active &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.SessionId == Id);  // NEW: Direct check
    }
}
```

**TicketType.Sold** (already correct, no changes needed):
```csharp
[NotMapped]
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

---

## Sample Queries

### 1. Does user have ticket for this session?

**Query**:
```csharp
var hasTicket = await _context.EventAttendances
    .AnyAsync(ea =>
        ea.SessionId == sessionId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.AttendanceType == AttendanceType.Ticket);
```

**SQL**:
```sql
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM "EventAttendances"
    WHERE "SessionId" = @sessionId
      AND "UserId" = @userId
      AND "Status" = 'Active'
      AND "AttendanceType" = 'Ticket'
) THEN TRUE ELSE FALSE END;
```

**Performance**: Excellent (uses `IX_EventAttendances_UserId_SessionId_Status` index).

---

### 2. How many tickets sold for this session?

**Query**:
```csharp
var sold = await _context.EventAttendances
    .CountAsync(ea =>
        ea.SessionId == sessionId &&
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.Status == AttendanceStatus.Active);
```

**SQL**:
```sql
SELECT COUNT(*) FROM "EventAttendances"
WHERE "SessionId" = @sessionId
  AND "AttendanceType" = 'Ticket'
  AND "Status" = 'Active';
```

**Performance**: Excellent (uses `IX_EventAttendances_SessionId_Status_AttendanceType` index).

---

### 3. What sessions does user's ticket cover?

**Query**:
```csharp
var sessions = await _context.EventAttendances
    .Where(ea =>
        ea.TicketPurchaseId == ticketPurchaseId &&
        ea.Status == AttendanceStatus.Active)
    .Include(ea => ea.Session)
    .Select(ea => ea.Session)
    .ToListAsync();
```

**SQL**:
```sql
SELECT s.* FROM "EventAttendances" ea
INNER JOIN "Sessions" s ON s."Id" = ea."SessionId"
WHERE ea."TicketPurchaseId" = @ticketPurchaseId
  AND ea."Status" = 'Active';
```

**Performance**: Good (index on TicketPurchaseId + SessionId FK).

---

### 4. Get all attendees for a session

**Query**:
```csharp
var attendees = await _context.EventAttendances
    .Where(ea =>
        ea.SessionId == sessionId &&
        ea.Status == AttendanceStatus.Active)
    .Include(ea => ea.User)
    .Select(ea => new
    {
        UserId = ea.UserId,
        SceneName = ea.User.SceneName,
        AttendanceType = ea.AttendanceType
    })
    .ToListAsync();
```

**SQL**:
```sql
SELECT
    ea."UserId",
    u."SceneName",
    ea."AttendanceType"
FROM "EventAttendances" ea
INNER JOIN "AspNetUsers" u ON u."Id" = ea."UserId"
WHERE ea."SessionId" = @sessionId
  AND ea."Status" = 'Active'
ORDER BY u."SceneName";
```

**Performance**: Good (uses session index + user FK).

---

### 5. Session capacity report (all sessions for event)

**Query**:
```csharp
var report = await _context.Sessions
    .Where(s => s.EventId == eventId)
    .Select(s => new
    {
        SessionId = s.Id,
        SessionName = s.Name,
        Capacity = s.Capacity,
        Sold = s.Event.EventAttendances.Count(ea =>
            ea.SessionId == s.Id &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.Status == AttendanceStatus.Active),
        Remaining = s.Capacity - s.Event.EventAttendances.Count(ea =>
            ea.SessionId == s.Id &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.Status == AttendanceStatus.Active)
    })
    .ToListAsync();
```

**SQL**:
```sql
SELECT
    s."Id" as "SessionId",
    s."Name" as "SessionName",
    s."Capacity",
    COUNT(ea."Id") FILTER (WHERE ea."AttendanceType" = 'Ticket' AND ea."Status" = 'Active') as "Sold",
    s."Capacity" - COUNT(ea."Id") FILTER (WHERE ea."AttendanceType" = 'Ticket' AND ea."Status" = 'Active') as "Remaining"
FROM "Sessions" s
LEFT JOIN "EventAttendances" ea ON ea."SessionId" = s."Id"
WHERE s."EventId" = @eventId
GROUP BY s."Id", s."Name", s."Capacity"
ORDER BY s."StartTime";
```

**Performance**: Excellent (single query with aggregation).

---

## Performance Considerations

### Index Strategy

**Indexes Created**:
1. `IX_EventAttendances_SessionId` - Session-specific queries
2. `IX_EventAttendances_UserId_SessionId_Status` - Duplicate detection
3. `IX_EventAttendances_SessionId_Status_AttendanceType` - Capacity calculations

**Query Performance Targets**:
- Duplicate detection: < 10ms (index seek)
- Capacity calculations: < 50ms (aggregate with index)
- Attendee list queries: < 100ms (JOIN with index)

**Database Size Impact**:
- Single-session tickets: No change (1 record per ticket)
- Multi-session tickets: 3x records for 3-day workshop
- Example: 100 "Full Weekend" tickets = 300 EventAttendance records
- Storage impact: Minimal (~100 bytes per record = 30KB total)

**Query Optimization**:
- Use `.AsNoTracking()` for read-only capacity queries
- Batch cancellations for multi-session tickets
- Consider caching capacity counts (invalidate on purchase/cancel)

---

## Security Considerations

### Data Privacy

**Session Attendance Information**:
- Public users: See aggregate capacity only ("15 of 30 spots")
- Authenticated users: Same as public (no additional session details)
- Admins: Full attendee list per session with scene names

**Access Control**:
- Session capacity queries: Public (no authentication required)
- Attendee lists: Admin role required
- User's own session tickets: Authenticated user only

### Data Integrity

**Foreign Key Constraints**:
- SessionId → Sessions.Id (CASCADE DELETE)
  - If session deleted, all attendances deleted
  - Prevents orphaned attendance records

**Status Validation**:
- Only Active attendances count toward capacity
- Cancelled/Refunded attendances don't block new purchases

**Concurrency Handling**:
- Use optimistic locking on capacity checks
- Handle race conditions for last available spot
- Transaction isolation for ticket purchases

---

## Monitoring & Observability

### Key Metrics to Track

**Performance Metrics**:
- Duplicate detection query time (target: <10ms p95)
- Capacity calculation query time (target: <50ms p95)
- Multi-session purchase creation time (target: <200ms p95)

**Data Quality Metrics**:
- Percentage of EventAttendance with NULL SessionId (should decrease over time)
- Multi-session ticket records (should match TicketType.Sessions count)
- Orphaned records (SessionId points to deleted session - should be 0)

**Business Metrics**:
- Multi-session ticket purchases (measure adoption)
- Session utilization percentage (average capacity filled)
- Cancellation rate for multi-session vs single-session tickets

### Logging

**Log Events**:
- Multi-session attendance creation (INFO)
- Session overlap detection (WARNING)
- Backfill script execution (INFO)
- SessionId migration errors (ERROR)

**Sample Log Entry**:
```json
{
  "timestamp": "2025-12-08T10:30:00Z",
  "level": "INFO",
  "message": "Created multi-session attendance",
  "userId": "abc123",
  "eventId": "def456",
  "ticketTypeId": "ghi789",
  "sessionIds": ["session1", "session2", "session3"],
  "attendanceIds": ["att1", "att2", "att3"]
}
```

---

## Testing Strategy

### Unit Tests

**Test Cases**:
1. Create single-session attendance (SessionId set)
2. Create multi-session attendance (3 records created)
3. Detect duplicate session purchase (throws exception)
4. Allow non-overlapping session purchases (succeeds)
5. Cancel multi-session ticket (all records cancelled)
6. Calculate session capacity (accurate count)

**Sample Test**:
```csharp
[Fact]
public async Task CreateAttendance_MultiSessionTicket_CreatesMultipleRecords()
{
    // Arrange
    var ticketType = CreateTicketTypeWithThreeSessions();
    var userId = Guid.NewGuid();

    // Act
    await _attendanceService.CreateAttendanceAsync(
        userId, ticketType.Id, ticketPurchaseId: Guid.NewGuid());

    // Assert
    var attendances = await _context.EventAttendances
        .Where(ea => ea.UserId == userId)
        .ToListAsync();

    Assert.Equal(3, attendances.Count);
    Assert.All(attendances, a => Assert.NotNull(a.SessionId));
    Assert.Equal(3, attendances.Select(a => a.SessionId).Distinct().Count());
}
```

### Integration Tests

**Test Scenarios**:
1. End-to-end ticket purchase (multi-session)
2. Duplicate session detection (API level)
3. Capacity calculation accuracy (database level)
4. Cancellation workflow (all records cancelled)
5. Backfill script validation (data integrity)

### E2E Tests

**User Workflows**:
1. Purchase "Friday Only" ticket → Purchase "Sunday Only" ticket (succeeds)
2. Purchase "Full Weekend" ticket → Attempt "Saturday Only" (fails with clear error)
3. Admin views session capacity report (accurate counts)
4. User cancels multi-session ticket (all sessions released)

---

## Migration Rollback Plan

### If Issues Detected

**Rollback Steps**:
1. Stop all ticket purchases (feature flag)
2. Revert database migration (restore SessionId NULL)
3. Delete duplicate EventAttendance records (keep original)
4. Restart API with old validation logic
5. Verify capacity calculations match pre-migration

**Rollback SQL**:
```sql
-- Delete additional records created for multi-session tickets
DELETE FROM "EventAttendances"
WHERE "Notes" LIKE '%Auto-created from multi-session backfill%';

-- Set all SessionIds to NULL
UPDATE "EventAttendances" SET "SessionId" = NULL;

-- Drop foreign key
ALTER TABLE "EventAttendances"
DROP CONSTRAINT "FK_EventAttendances_Sessions_SessionId";

-- Drop indexes
DROP INDEX "IX_EventAttendances_SessionId";
DROP INDEX "IX_EventAttendances_UserId_SessionId_Status";
DROP INDEX "IX_EventAttendances_SessionId_Status_AttendanceType";

-- Drop column
ALTER TABLE "EventAttendances" DROP COLUMN "SessionId";
```

**Data Verification**:
```sql
-- Verify capacity counts match pre-migration snapshot
SELECT
    e."Id",
    e."Title",
    COUNT(ea."Id") as CurrentAttendees
FROM "Events" e
LEFT JOIN "EventAttendances" ea ON ea."EventId" = e."Id"
WHERE ea."Status" = 'Active' AND ea."AttendanceType" = 'Ticket'
GROUP BY e."Id", e."Title";
```

---

## Success Criteria

### Technical Success

- ✅ Migration applied without errors
- ✅ All existing EventAttendance records have SessionId populated
- ✅ Capacity calculations match pre-migration values
- ✅ All unit tests passing (>90% coverage)
- ✅ All integration tests passing
- ✅ Query performance within targets (<100ms p95)

### Business Success

- ✅ Users can purchase non-consecutive session tickets
- ✅ Duplicate session purchases blocked with clear error messages
- ✅ Session capacity displays accurately
- ✅ Admins can manage per-session capacity
- ✅ Zero overselling incidents

### Operational Success

- ✅ Zero data loss during migration
- ✅ Backward compatibility maintained (single-session events work)
- ✅ Monitoring dashboards show healthy metrics
- ✅ No production incidents related to session tracking

---

## Next Steps

### Implementation Phases

1. **Database Migration** (Database Designer + Backend Developer)
   - Create EF Core migration
   - Test on staging environment
   - Apply to production during maintenance window

2. **Data Backfill** (Backend Developer)
   - Implement backfill script
   - Validate data integrity
   - Execute in staging and production

3. **Business Logic** (Backend Developer)
   - Update AttendanceService validation
   - Modify capacity calculations
   - Update cancellation logic

4. **Testing** (Test Developer)
   - Write comprehensive unit tests
   - Create integration tests
   - Execute E2E test scenarios

5. **Deployment** (Orchestrator + DevOps)
   - Staging deployment
   - Production deployment with rollback plan
   - Post-deployment monitoring

---

## Related Documentation

**Business Requirements**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/requirements/business-requirements.md`

**Impact Analysis**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/impact-analysis.md`

**Entity Framework Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`

**Database Migrations Guide**: `/docs/standards-processes/backend/database-migrations-guide.md`

---

## Appendix: Alternative Approaches Considered

### Approach: JSONB Array of SessionIds

**Concept**: Store SessionIds as JSONB array on EventAttendance.

**Schema**:
```csharp
public class EventAttendance
{
    public string SessionIdsJson { get; set; } = "[]";

    [NotMapped]
    public List<Guid> SessionIds
    {
        get => JsonSerializer.Deserialize<List<Guid>>(SessionIdsJson);
        set => SessionIdsJson = JsonSerializer.Serialize(value);
    }
}
```

**Why Rejected**:
- ❌ JSONB queries more complex in EF Core
- ❌ Less intuitive for SQL queries
- ❌ Harder to enforce foreign key constraints
- ❌ Index strategy less efficient

---

## Document Status

**Version**: 1.0
**Created**: 2025-12-08
**Author**: Database Designer Agent
**Status**: Final Design - Ready for Implementation
**Reviewed By**: Pending
**Approved By**: Pending

**Change History**:
- 2025-12-08: Initial design document created
- 2025-12-08: Added comprehensive query examples and migration strategy
- 2025-12-08: Finalized Option A recommendation with detailed rationale
