# Functional Specification: Session-Based Ticket Validation
<!-- Last Updated: 2025-12-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Executive Summary

This specification defines the technical implementation for transitioning from ONE ticket per EVENT per user to ONE ticket per SESSION per user. This is a **minimal changes implementation** - the existing UI is 95%+ complete. The core fix is backend validation logic; frontend changes are limited to displaying session-specific data and preventing overlap selection.

**Key Changes:**
- **Backend (Priority 1)**: Add `SessionId` to `EventAttendance` entity, change validation from event-level to session-level
- **Frontend (Minimal)**: Disable overlapping tickets during selection, display session coverage information
- **Database**: Add nullable `SessionId` column with foreign key to `Sessions` table
- **DTOs**: Add session-specific fields to participation status and user event DTOs

---

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5655
- **Database** (PostgreSQL): localhost:5434
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

---

## Backend Changes (PRIORITY 1)

### 1. Database Schema Changes

#### EventAttendance Entity - Add SessionId

**File**: `/apps/api/Features/Participation/Entities/EventAttendance.cs`

**Add Property:**
```csharp
/// <summary>
/// Session the user is attending (NULL for single-session events or legacy data)
/// For multi-session tickets, multiple EventAttendance records are created.
/// </summary>
public Guid? SessionId { get; set; }

/// <summary>
/// Navigation property to session
/// </summary>
public Session? Session { get; set; }
```

#### Entity Framework Configuration

**File**: `/apps/api/Features/Participation/Entities/EventAttendanceConfiguration.cs`

**Add Configuration:**
```csharp
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
```

#### Database Migration

**Create Migration:**
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddSessionIdToEventAttendance --output-dir Data/Migrations
```

**Expected Migration SQL:**
```sql
-- Add SessionId column (nullable for backward compatibility)
ALTER TABLE "EventAttendances"
ADD COLUMN "SessionId" UUID NULL;

-- Add foreign key constraint
ALTER TABLE "EventAttendances"
ADD CONSTRAINT "FK_EventAttendances_Sessions_SessionId"
FOREIGN KEY ("SessionId") REFERENCES "Sessions"("Id")
ON DELETE CASCADE;

-- Create indexes
CREATE INDEX "IX_EventAttendances_SessionId"
ON "EventAttendances"("SessionId");

CREATE INDEX "IX_EventAttendances_UserId_SessionId_Status"
ON "EventAttendances"("UserId", "SessionId", "Status");

CREATE INDEX "IX_EventAttendances_SessionId_Status_AttendanceType"
ON "EventAttendances"("SessionId", "Status", "AttendanceType");
```

---

### 2. AttendanceService.cs - Session-Level Validation

**File**: `/apps/api/Features/Participation/Services/AttendanceService.cs`

#### Current Validation (Lines 562-575) - REPLACE:

```csharp
// OLD: Event-level duplicate check
var existingTicket = await _context.EventAttendances
    .FirstOrDefaultAsync(ea =>
        ea.EventId == request.EventId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.AttendanceType == AttendanceType.Ticket,
        cancellationToken);

if (existingTicket != null)
{
    return Result<ParticipationStatusDto>.Failure("User already has a ticket for this event");
}
```

#### New Validation Logic - SESSION-LEVEL:

```csharp
// NEW: Session-level duplicate check
// Get sessions covered by the ticket type user is purchasing
var ticketType = await _context.TicketTypes
    .Include(tt => tt.Sessions)
    .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId, cancellationToken);

if (ticketType == null)
{
    return Result<ParticipationStatusDto>.Failure("Ticket type not found");
}

// Get all session IDs for this ticket type
var requestedSessionIds = ticketType.Sessions
    .Select(s => s.Id)
    .ToList();

// Check if user already has a ticket for ANY of these sessions
var overlappingAttendance = await _context.EventAttendances
    .Where(ea =>
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.SessionId.HasValue &&
        requestedSessionIds.Contains(ea.SessionId.Value))
    .Include(ea => ea.Session)
    .Include(ea => ea.TicketPurchase)
        .ThenInclude(tp => tp.TicketType)
    .FirstOrDefaultAsync(cancellationToken);

if (overlappingAttendance != null)
{
    // Get the session name for error message
    var overlappingSessionName = overlappingAttendance.Session?.Name ?? "a session";
    var existingTicketName = overlappingAttendance.TicketPurchase?.TicketType?.Name ?? "an existing ticket";

    return Result<ParticipationStatusDto>.Failure(
        $"You already have a ticket that includes the {overlappingSessionName} session ({existingTicketName})"
    );
}

// Validation passed - proceed with ticket creation
```

#### Create Attendance Records for Multi-Session Tickets:

**Location**: After validation passes in ticket purchase flow

```csharp
// For each session in the ticket type, create an EventAttendance record
foreach (var session in ticketType.Sessions)
{
    var attendance = new EventAttendance
    {
        Id = Guid.NewGuid(),
        EventId = eventId,
        UserId = userId,
        SessionId = session.Id,  // NEW - Link to specific session
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

await _context.SaveChangesAsync(cancellationToken);
```

#### Cancellation Logic - Cancel All Session Records:

```csharp
// Get all EventAttendance records for this ticket purchase
var attendances = await _context.EventAttendances
    .Where(ea => ea.TicketPurchaseId == ticketPurchaseId)
    .ToListAsync(cancellationToken);

// Cancel all of them (for multi-session tickets, this will be multiple records)
foreach (var attendance in attendances)
{
    attendance.Cancel(cancellationReason);
}

await _context.SaveChangesAsync(cancellationToken);
```

---

### 3. DTO Changes - Add Session Information

#### EnhancedParticipationStatusDto

**File**: `/apps/api/Features/Participation/DTOs/EnhancedParticipationStatusDto.cs`

**Add Properties:**
```csharp
/// <summary>
/// Session IDs the user already has tickets for
/// Used to prevent duplicate session purchases and show partial ownership
/// </summary>
public List<Guid> OwnedSessionIds { get; set; } = new();

/// <summary>
/// Whether user can purchase tickets for additional sessions
/// (has available sessions they don't own, within timing window)
/// </summary>
public bool CanPurchaseAdditionalSessions { get; set; }

/// <summary>
/// Per-session availability information (for multi-session events)
/// </summary>
public List<SessionAvailabilityDto> SessionAvailability { get; set; } = new();
```

**Add DTO:**
```csharp
public class SessionAvailabilityDto
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public int Sold { get; set; }
    public int Available { get; set; }
    public bool IsSoldOut => Available <= 0;
}
```

#### UserEventDto - Add Ticket Sessions

**File**: `/apps/api/Features/Users/DTOs/UserEventDto.cs`

**Add Property:**
```csharp
/// <summary>
/// Sessions covered by user's ticket (for multi-session events)
/// NULL if user doesn't have a ticket
/// </summary>
public List<SessionSummaryDto>? TicketSessions { get; set; }
```

**Add DTO:**
```csharp
public class SessionSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
```

#### Populate DTOs in Service Logic:

**In participation status service:**
```csharp
// Get sessions user owns tickets for
var ownedSessionIds = await _context.EventAttendances
    .Where(ea =>
        ea.EventId == eventId &&
        ea.UserId == userId &&
        ea.Status == AttendanceStatus.Active &&
        ea.AttendanceType == AttendanceType.Ticket &&
        ea.SessionId.HasValue)
    .Select(ea => ea.SessionId.Value)
    .ToListAsync(cancellationToken);

// Calculate session availability
var sessionAvailability = await _context.Sessions
    .Where(s => s.EventId == eventId)
    .Select(s => new SessionAvailabilityDto
    {
        SessionId = s.Id,
        SessionName = s.Name,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Capacity = s.Capacity,
        Sold = _context.EventAttendances.Count(ea =>
            ea.SessionId == s.Id &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.Status == AttendanceStatus.Active),
        Available = s.Capacity - _context.EventAttendances.Count(ea =>
            ea.SessionId == s.Id &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.Status == AttendanceStatus.Active)
    })
    .ToListAsync(cancellationToken);

return new EnhancedParticipationStatusDto
{
    // ... existing fields ...
    OwnedSessionIds = ownedSessionIds,
    SessionAvailability = sessionAvailability,
    CanPurchaseAdditionalSessions = sessionAvailability.Any(sa =>
        !ownedSessionIds.Contains(sa.SessionId) &&
        sa.Available > 0)
};
```

**In UserEventDto mapping:**
```csharp
// Get ticket sessions if user has a ticket
List<SessionSummaryDto>? ticketSessions = null;
if (hasTicket)
{
    ticketSessions = await _context.EventAttendances
        .Where(ea =>
            ea.EventId == eventId &&
            ea.UserId == userId &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.Status == AttendanceStatus.Active &&
            ea.SessionId.HasValue)
        .Include(ea => ea.Session)
        .Select(ea => new SessionSummaryDto
        {
            Id = ea.Session!.Id,
            Name = ea.Session.Name,
            StartTime = ea.Session.StartTime,
            EndTime = ea.Session.EndTime
        })
        .ToListAsync(cancellationToken);
}

return new UserEventDto
{
    // ... existing fields ...
    TicketSessions = ticketSessions
};
```

---

## Frontend Changes (MINIMAL)

### 1. EventPaymentPage.tsx - Ticket Selection with Overlap Prevention

**File**: `/apps/web/src/features/payments/pages/EventPaymentPage.tsx`

#### Add Function: getDisabledTicketIds (Around Line 351)

**Purpose**: Disable tickets that share sessions with already-selected tickets

```typescript
/**
 * Check if selecting a ticket would create session overlap
 * Returns set of ticket IDs that should be disabled due to overlap
 */
const getDisabledTicketIds = (selectedIds: string[]): Set<string> => {
  const disabledIds = new Set<string>();

  // Get all sessions covered by currently selected tickets
  const coveredSessionIds = new Set<string>();
  selectedIds.forEach(ticketId => {
    const ticket = ticketTypes.find(tt => tt.id === ticketId);
    ticket?.sessionIdentifiers?.forEach(sessionId => {
      coveredSessionIds.add(sessionId);
    });
  });

  // Find tickets that have overlapping sessions (disable them)
  ticketTypes.forEach(ticket => {
    if (selectedIds.includes(ticket.id || '')) return; // Already selected, skip

    const hasOverlap = ticket.sessionIdentifiers?.some(
      sessionId => coveredSessionIds.has(sessionId)
    );

    if (hasOverlap) {
      disabledIds.add(ticket.id || '');
    }
  });

  return disabledIds;
};
```

#### Add: User's Already-Purchased Tickets Display

**Fetch user's existing ticket IDs** (add to existing useEffect):
```typescript
// Inside loadEventInfo function, after fetching event details
const userPurchasedTicketIds: string[] = []; // TODO: Fetch from API
// API should return list of ticket type IDs user already owns for this event
```

**Modify ticket card rendering** (Around Line 535-598):
```typescript
// Calculate disabled tickets
const disabledTicketIds = getDisabledTicketIds(selectedTicketTypeIds);
const isAlreadyPurchased = userPurchasedTicketIds?.includes(tt.id || '');
const isDisabled = disabledTicketIds.has(tt.id || '') || isAlreadyPurchased;

// ... in ticket card render:

// Show "Already Purchased" instead of price
{isAlreadyPurchased ? (
  <Text fw={700} size="lg" c="green" style={{ whiteSpace: 'nowrap' }}>
    Already Purchased
  </Text>
) : (
  <Text fw={700} size="lg" c="#880124" style={{ whiteSpace: 'nowrap' }}>
    {priceDisplay}
  </Text>
)}

// Show overlap message for disabled tickets
{disabledTicketIds.has(tt.id || '') && !isAlreadyPurchased && (
  <Text size="xs" c="dimmed" mt={4}>
    Sessions overlap with selected ticket
  </Text>
)}

// Disable checkbox
<Checkbox
  checked={isSelected || isAlreadyPurchased}
  disabled={isDisabled}
  onChange={(e) => {
    e.stopPropagation();
    handleTicketTypeToggle(tt.id, e.currentTarget.checked);
  }}
  color="wcr"
/>
```

**Behavior for Already Purchased Tickets:**
- Checkbox shown as checked and disabled
- NOT added to cart total
- NOT included in purchase API call
- Visual: Green "Already Purchased" text instead of price

---

### 2. PaymentConfirmation.tsx - Show Purchased Tickets

**File**: `/apps/web/src/features/payments/components/PaymentConfirmation.tsx`

**Add After Line 112** (under "Registration Details" title):

```typescript
{/* Ticket Information - NEW */}
{purchasedTickets && purchasedTickets.length > 0 && (
  <Stack gap="xs" mb="md">
    {purchasedTickets.map((ticket, index) => (
      <Group key={ticket.id || index} gap="sm">
        <IconTicket size={18} color="#6B0119" />
        <Box>
          <Text fw={600}>{ticket.name}</Text>
          {ticket.sessionDates && (
            <Text size="sm" c="dimmed">
              {ticket.sessionDates}
            </Text>
          )}
        </Box>
      </Group>
    ))}
  </Stack>
)}

{/* Existing Event Information continues... */}
```

**Add Interface:**
```typescript
interface PurchasedTicket {
  id: string;
  name: string;
  sessionDates: string; // e.g., "Sun, Dec 1 • Sat, Dec 7"
}

interface PaymentConfirmationProps {
  // ... existing props ...
  purchasedTickets?: PurchasedTicket[]; // NEW
}
```

---

### 3. ParticipationCard.tsx - Session Availability & Partial Ownership

**File**: `/apps/web/src/components/events/ParticipationCard.tsx`

#### Show Session Availability for Multi-Session Events

**Add After Capacity Section** (Around Line 457):

```typescript
{/* Session Availability Section - Only for multi-session events */}
{sessions && sessions.length > 1 && (
  <Stack gap="xs" mb="md">
    <Text fw={600} size="sm" c="dimmed" tt="uppercase">
      Session Availability
    </Text>
    {sessions.map(session => (
      <Group key={session.id} justify="space-between">
        <Text size="sm">
          {formatUtcToLocalDate(session.startTime, eventTimeZone, {
            weekday: 'short',
            month: 'short',
            day: 'numeric'
          })} • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
        </Text>
        <Text size="sm" c="dimmed">
          {session.soldCount} sold, {session.availableCount} Available
        </Text>
      </Group>
    ))}
  </Stack>
)}
```

#### Update Purchase Button Logic for Partial Session Ownership

**Replace Current Logic** (Around Line 227-229):

```typescript
// OLD: Hide purchase button if user has ANY ticket
const canPurchaseTicket = !hasTicket;
```

**NEW: Show purchase button if there are sessions user doesn't own**:

```typescript
// NEW: Show purchase button if there are sessions user doesn't have tickets for
const userOwnedSessionIds = participation?.ownedSessionIds || [];
const availableSessions = sessions.filter(session =>
  !userOwnedSessionIds.includes(session.id) &&
  session.availableCount > 0
);

// Check if any ticket types for available sessions are within purchase window
const canPurchaseMoreTickets = availableSessions.length > 0 &&
  ticketTypes.some(tt =>
    tt.canPurchase && // Within timing window
    tt.sessionIdentifiers?.some(sid =>
      availableSessions.map(s => s.sessionIdentifier).includes(sid)
    )
  );
```

#### Display Owned Sessions When User Has Partial Tickets

**Add Before Purchase Button** (when user has some tickets but not all):

```typescript
{/* Show owned sessions */}
{userOwnedSessionIds.length > 0 && canPurchaseMoreTickets && (
  <Alert color="green" variant="light" icon={<IconCheck />} mb="md">
    <Text size="sm" fw={500}>You have tickets for:</Text>
    <Stack gap={4} mt="xs">
      {ownedSessions.map(session => (
        <Text key={session.id} size="sm">
          • {formatUtcToLocalDate(session.startTime, eventTimeZone, {
              weekday: 'short',
              month: 'short',
              day: 'numeric'
            })} • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
        </Text>
      ))}
    </Stack>
  </Alert>
)}

{/* Show purchase option if more sessions available */}
{canPurchaseMoreTickets && (
  <>
    <Text size="sm" c="dimmed" mb="sm">
      Additional sessions available
    </Text>
    {/* Show available sessions list */}
    {/* Purchase Ticket button */}
  </>
)}

{/* If user has tickets for ALL sessions */}
{userOwnedSessionIds.length > 0 && !canPurchaseMoreTickets && (
  <Alert color="blue" variant="light">
    <Text size="sm" fw={500}>You have tickets for all sessions</Text>
  </Alert>
)}
```

---

### 4. Dashboard EventCard.tsx - Show Ticket Sessions

**File**: `/apps/web/src/pages/dashboard/components/EventCard.tsx`

**Add After Ticket Badge** (Around Line 310):

```typescript
{/* Show which sessions ticket covers */}
{event.hasTicket && event.ticketSessions && event.ticketSessions.length > 0 && (
  <Text size="xs" c="dimmed" mt={4}>
    Ticket covers: {event.ticketSessions.map(s =>
      formatUtcToLocalDate(s.startTime, eventTimeZone, {
        weekday: 'short',
        month: 'short',
        day: 'numeric'
      })
    ).join(' • ')}
  </Text>
)}
```

---

## API Contract Changes

### Participation Status Endpoint

**Endpoint**: `GET /api/events/{eventId}/participation/status`

**Response Changes** (EnhancedParticipationStatusDto):
```json
{
  "hasRSVP": true,
  "hasTicket": false,
  "canRSVP": false,
  "canPurchaseTicket": true,

  // NEW FIELDS
  "ownedSessionIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "canPurchaseAdditionalSessions": true,
  "sessionAvailability": [
    {
      "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "sessionName": "Friday Evening",
      "startTime": "2025-12-20T23:00:00Z",
      "endTime": "2025-12-21T02:00:00Z",
      "capacity": 30,
      "sold": 22,
      "available": 8,
      "isSoldOut": false
    },
    {
      "sessionId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "sessionName": "Saturday Workshop",
      "startTime": "2025-12-21T14:00:00Z",
      "endTime": "2025-12-21T21:00:00Z",
      "capacity": 30,
      "sold": 30,
      "available": 0,
      "isSoldOut": true
    }
  ],

  "capacity": {
    "total": 30,
    "current": 22,
    "available": 8
  }
}
```

### User Dashboard Events Endpoint

**Endpoint**: `GET /api/users/dashboard/events`

**Response Changes** (UserEventDto):
```json
{
  "id": "abc123",
  "title": "Rope Suspension Intensive",
  "startDate": "2025-12-20T23:00:00Z",
  "endDate": "2025-12-22T17:00:00Z",
  "hasTicket": true,
  "registrationStatus": "Ticket Purchased",

  // NEW FIELD
  "ticketSessions": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Friday Evening",
      "startTime": "2025-12-20T23:00:00Z",
      "endTime": "2025-12-21T02:00:00Z"
    },
    {
      "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
      "name": "Sunday Practice",
      "startTime": "2025-12-22T14:00:00Z",
      "endTime": "2025-12-22T17:00:00Z"
    }
  ]
}
```

---

## Database Migration Strategy

### Phase 1: Schema Migration (No User Impact)

1. **Apply Migration:**
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddSessionIdToEventAttendance --output-dir Data/Migrations
dotnet ef database update
```

2. **Verify Migration:**
```sql
-- Check for NULL SessionIds (expected after migration)
SELECT COUNT(*) FROM "EventAttendances" WHERE "SessionId" IS NULL;
```

### Phase 2: Data Backfill

**Purpose**: Populate SessionId for existing EventAttendance records

**Strategy:**
- For each EventAttendance with NULL SessionId
- Determine sessions from TicketPurchase → TicketType → Sessions
- For single-session tickets: Update SessionId
- For multi-session tickets: Create additional EventAttendance records

**Backfill Script** (run via EF Core migration or one-time service):
```csharp
var attendancesWithoutSession = await _context.EventAttendances
    .Include(ea => ea.TicketPurchase)
        .ThenInclude(tp => tp.TicketType)
            .ThenInclude(tt => tt.Sessions)
    .Include(ea => ea.Event)
        .ThenInclude(e => e.Sessions)
    .Where(ea => ea.SessionId == null)
    .ToListAsync();

foreach (var attendance in attendancesWithoutSession)
{
    List<Session> coveredSessions;

    if (attendance.TicketPurchase?.TicketType?.Sessions != null)
    {
        coveredSessions = attendance.TicketPurchase.TicketType.Sessions.ToList();
    }
    else if (attendance.Event?.Sessions != null)
    {
        // RSVP or missing data - use all event sessions
        coveredSessions = attendance.Event.Sessions.ToList();
    }
    else
    {
        continue; // No session data available
    }

    if (coveredSessions.Count == 1)
    {
        // Single session - just update SessionId
        attendance.SessionId = coveredSessions[0].Id;
    }
    else if (coveredSessions.Count > 1)
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
                UpdatedAt = DateTime.UtcNow,
                // ... copy all other fields ...
                Notes = $"Auto-created from multi-session backfill. Original: {attendance.Id}"
            };

            _context.EventAttendances.Add(additionalAttendance);
        }
    }
}

await _context.SaveChangesAsync();
```

### Phase 3: Validation

```sql
-- Verify 100% of records have SessionId
SELECT COUNT(*) FROM "EventAttendances" WHERE "SessionId" IS NULL;
-- Should return 0

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

## Test Scenarios

### 1. Happy Path - Non-Consecutive Session Purchase

**Steps:**
1. User purchases "Friday Only" ticket for 3-day workshop
2. EventAttendance created with SessionId = Friday session
3. User returns to event page
4. User selects "Sunday Only" ticket
5. System validates: No overlap with existing Friday ticket ✅
6. User purchases "Sunday Only" ticket
7. EventAttendance created with SessionId = Sunday session

**Expected:**
- Two separate EventAttendance records (same user, same event, different sessions)
- Dashboard shows "Ticket covers: Fri, Dec 20 • Sun, Dec 22"
- Capacity updates: Friday -1, Sunday -1

---

### 2. Error Path - Duplicate Session Coverage

**Steps:**
1. User purchases "Full Weekend" ticket (Friday, Saturday, Sunday)
2. Three EventAttendance records created (one per session)
3. User attempts to purchase "Saturday Only" ticket
4. System validates: ❌ Overlap detected on Saturday session
5. Error message: "You already have a ticket that includes the Saturday Workshop session (Full Weekend)"
6. Purchase blocked

**Expected:**
- Clear error message identifying which session overlaps
- Checkbox disabled for "Saturday Only" ticket in UI
- No duplicate purchase created

---

### 3. Partial Ownership Display

**Steps:**
1. User purchases "Friday Only" ticket
2. User views event detail page
3. UI shows:
   - Green alert: "You have tickets for: Fri, Dec 20"
   - "Additional sessions available" message
   - "Purchase Ticket" button enabled
4. User can purchase "Weekend Days" (Saturday + Sunday)

**Expected:**
- ParticipationCard shows owned sessions
- ParticipationCard shows available sessions
- Purchase button remains visible
- User can complete additional purchase

---

### 4. All Sessions Owned

**Steps:**
1. User purchases "Full Weekend" ticket
2. User views event detail page
3. UI shows:
   - Blue alert: "You have tickets for all sessions"
   - NO "Purchase Ticket" button

**Expected:**
- No purchase button visible
- Clear messaging that user has full access
- Dashboard shows all session dates under ticket

---

### 5. Sold-Out Session Handling

**Steps:**
1. Saturday session reaches capacity (30/30 sold)
2. User attempts to purchase "Full Weekend" ticket
3. System validates: Saturday session at capacity
4. Purchase blocked with error: "Saturday Workshop session is sold out. Please choose a different ticket type."

**Expected:**
- "Full Weekend" ticket disabled/unavailable
- "Friday Only" and "Sunday Only" still available
- Clear messaging about which session is sold out

---

### 6. Multi-Ticket Checkbox Selection

**Steps:**
1. User selects "Friday Only" ticket (checkbox checked)
2. System disables tickets with overlapping sessions:
   - "Full Weekend" (includes Friday) - DISABLED
   - "Weekend Intensive" (Friday + Saturday) - DISABLED
3. "Saturday Only" and "Sunday Only" remain enabled
4. User can select "Sunday Only" in addition to "Friday Only"

**Expected:**
- Disabled checkboxes show "Sessions overlap with selected ticket" message
- User can select multiple non-overlapping tickets
- Cart total updates correctly

---

## Implementation Order (RECOMMENDED)

1. **Database Migration** (Backend Developer)
   - Create and apply migration
   - Test on staging database

2. **Data Backfill** (Backend Developer)
   - Implement backfill script
   - Execute on staging
   - Validate data integrity

3. **AttendanceService Changes** (Backend Developer)
   - Update validation logic to session-level
   - Update ticket creation to create multiple records
   - Update cancellation logic

4. **DTO Changes** (Backend Developer)
   - Add OwnedSessionIds to EnhancedParticipationStatusDto
   - Add TicketSessions to UserEventDto
   - Update service methods to populate new fields

5. **Frontend - EventPaymentPage** (React Developer)
   - Add getDisabledTicketIds function
   - Add already-purchased display
   - Test checkbox overlap prevention

6. **Frontend - ParticipationCard** (React Developer)
   - Add session availability display
   - Update purchase button logic
   - Add owned sessions display

7. **Frontend - PaymentConfirmation** (React Developer)
   - Add purchased tickets display

8. **Frontend - Dashboard EventCard** (React Developer)
   - Add ticket sessions display

9. **Integration Testing** (Test Developer)
   - End-to-end purchase flows
   - Error scenarios
   - Multi-session combinations

10. **Production Deployment** (Orchestrator)
    - Deploy to staging
    - Smoke tests
    - Production deployment with monitoring

---

## Quality Checklist

- [ ] Migration creates nullable SessionId column
- [ ] Migration creates foreign key constraint
- [ ] Migration creates performance indexes
- [ ] Backfill script populates 100% of existing records
- [ ] Session-level validation prevents overlapping purchases
- [ ] Multi-session tickets create multiple EventAttendance records
- [ ] Cancellation logic handles multi-session tickets correctly
- [ ] OwnedSessionIds populated correctly in participation status
- [ ] TicketSessions populated correctly in user dashboard
- [ ] Frontend prevents overlapping ticket selection
- [ ] Already-purchased tickets shown with green text
- [ ] Session availability displays for multi-session events
- [ ] Partial ownership displays correctly
- [ ] Purchase button logic respects session ownership
- [ ] Dashboard shows ticket session coverage
- [ ] All E2E test scenarios pass
- [ ] Backward compatibility maintained for single-session events

---

## Document Status

**Version**: 1.0
**Created**: 2025-12-08
**Author**: Functional Spec Agent
**Status**: Draft - Ready for Review
**Next Steps**:
1. Backend Developer review and approve backend changes
2. React Developer review and approve frontend changes
3. Test Developer create test plan based on scenarios
4. Proceed to Implementation Phase

**Related Documentation**:
- **Business Requirements**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/requirements/business-requirements.md`
- **Database Design**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/design/database-design.md`
- **UI Design**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/design/ui-design.md`
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **React Architecture**: `/docs/architecture/react-migration/react-architecture.md`
- **Vertical Slice Architecture**: `/docs/architecture/react-migration/vertical-slice-architecture-guide.md`
