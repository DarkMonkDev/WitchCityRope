# RSVP Persistence Bug - Root Cause Analysis

**Date**: 2025-11-16
**Bug ID**: Critical RSVP Persistence Failure
**Status**: Investigation Complete - Enhanced Logging Added

---

## Executive Summary

**Bug**: POST /api/events/{id}/rsvp returns 201 Created but RSVP record doesn't persist to database.

**Critical Findings**:
1. API returns 201 Created (endpoint completes successfully)
2. UI updates correctly (API response received)
3. Database shows NO new Active RSVP after API call completes
4. Only old cancelled RSVP exists in database
5. SaveChangesAsync defensive check PASSES (suggesting record is temporarily created)
6. Record appears to be created, verified, then rolled back or deleted

**Root Cause Hypothesis**: The RSVP record is being created and briefly persists, but something causes it to be rolled back or deleted AFTER the defensive verification check but BEFORE the transaction commits.

---

## Evidence Analysis

### Test Evidence
```
✅ API returns 201 Created
✅ UI updates correctly
🔍 DEBUG: Found 1 records WITHOUT status filter:
   [0] Status: 2, Type: 2, Updated: Sat Nov 15 2025 18:11:38
❌ No participation record found with status 1 (Active) and type 2 (RSVP)
```

**Interpretation**:
- Status 2 = Cancelled (per AttendanceStatus enum)
- Type 2 = RSVP (per AttendanceType enum)
- Only old cancelled RSVP exists
- New Active RSVP is missing

---

## Code Flow Analysis

### 1. Entity Creation (Lines 241-247)
```csharp
var attendance = new EventAttendance(request.EventId, userId, AttendanceType.RSVP)
{
    Notes = request.Notes,
    EventWaiverAccepted = true,
    EventWaiverAcceptedAt = DateTime.UtcNow,
    CreatedBy = userId
};
```

**Key Points**:
- Constructor sets `CreatedAt = DateTime.UtcNow`
- Constructor sets `UpdatedAt = DateTime.UtcNow`
- Constructor sets `Status = AttendanceStatus.Active` (default)
- ID is NOT set (per EF Core best practices - `Id = Guid.Empty`)

### 2. Entity Framework Configuration (EventAttendanceConfiguration.cs Line 21)
```csharp
builder.Property(e => e.Id)
       .ValueGeneratedOnAdd(); // Let PostgreSQL generate UUIDs
```

**Critical Implication**:
- EF Core expects database to generate the ID
- In-memory object has `Id = Guid.Empty` until SaveChanges
- After SaveChanges, EF Core should update the in-memory object with database-generated ID

### 3. DbContext Add (Line 253)
```csharp
_context.EventAttendances.Add(attendance);
```

**What Happens**:
- Entity state: `Added`
- ID remains `Guid.Empty` in memory
- EF Core tracks this entity for insertion

### 4. EventAttendee Creation (Lines 260-271)
```csharp
var existingAttendee = await _context.EventAttendees
    .FirstOrDefaultAsync(ea => ea.EventId == request.EventId && ea.UserId == userId, cancellationToken);

if (existingAttendee == null)
{
    var attendee = new CheckIn.Entities.EventAttendee
    {
        Id = Guid.NewGuid(), // MANUAL ID GENERATION
        EventId = request.EventId,
        UserId = userId,
        RegistrationStatus = "confirmed",
        HasCompletedWaiver = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = userId
    };

    _context.EventAttendees.Add(attendee);
}
```

**Potential Issue**:
- EventAttendee uses `Id = Guid.NewGuid()` (manual generation)
- EventAttendance uses database generation
- Mixed ID generation strategies in same transaction

### 5. AttendanceHistory Creation (Lines 286-299)
```csharp
var history = new AttendanceHistory(attendance.Id, "Created")
{
    NewValues = System.Text.Json.JsonSerializer.Serialize(new
    {
        EventId = attendance.EventId,
        UserId = attendance.UserId,
        AttendanceType = attendance.AttendanceType,
        Notes = attendance.Notes
    }),
    ChangedBy = userId,
    ChangeReason = "RSVP created by user"
};

_context.AttendanceHistory.Add(history);
```

**CRITICAL BUG IDENTIFIED**:
- `AttendanceHistory` constructor receives `attendance.Id`
- At this point, `attendance.Id = Guid.Empty` (not yet generated)
- AttendanceHistory is created with `AttendanceId = Guid.Empty`
- This creates a foreign key constraint violation!

### 6. SaveChangesAsync (Line 314)
```csharp
var savedCount = await _context.SaveChangesAsync(cancellationToken);
```

**Expected Behavior**:
1. Database generates ID for EventAttendance
2. EF Core updates `attendance.Id` in memory with generated value
3. Returns number of rows affected

**Actual Behavior** (Hypothesis):
- Foreign key constraint violation due to `AttendanceHistory.AttendanceId = Guid.Empty`
- OR: Transaction rollback due to constraint violation
- SaveChanges returns success but transaction rolls back

### 7. Defensive Verification (Lines 325-327)
```csharp
var savedAttendance = await _context.EventAttendances
    .AsNoTracking()
    .FirstOrDefaultAsync(ea => ea.Id == attendance.Id, cancellationToken);
```

**Mystery**:
- If SaveChanges failed, defensive check should find nothing and return error
- But API returns 201, suggesting defensive check PASSED
- This suggests record existed briefly then disappeared

---

## Root Cause: Foreign Key Constraint Issue

**THE SMOKING GUN**: Line 286 creates `AttendanceHistory` with `attendance.Id` BEFORE SaveChanges.

At this point:
- `attendance.Id = Guid.Empty` (not yet generated by database)
- `AttendanceHistory.AttendanceId = Guid.Empty`
- Foreign key relationship: `AttendanceHistory.AttendanceId → EventAttendance.Id`

**When SaveChanges is called**:
1. PostgreSQL generates new UUID for EventAttendance
2. PostgreSQL tries to insert AttendanceHistory with `AttendanceId = Guid.Empty`
3. Foreign key constraint violation (no EventAttendance with Id = Guid.Empty)
4. **BUT**: The defensive check might pass if there's a timing issue

---

## Alternative Hypothesis: Database Trigger or Constraint

**Possibility**:
- Database-level trigger or constraint that deletes/rolls back the record
- Partial unique constraint (Line 139 in EventAttendanceConfiguration.cs):
  ```csharp
  builder.HasIndex(e => new { e.UserId, e.EventId, e.AttendanceType })
         .IsUnique()
         .HasDatabaseName("UQ_EventAttendances_User_Event_Type_Active")
         .HasFilter("\"Status\" = 1"); // Only Active attendances
  ```

**But**: This constraint should throw an exception if violated, not silently delete the record.

---

## Enhanced Diagnostic Logging Added

### Service Layer Logging (AttendanceService.cs)

**Line 249-251**: Before adding to DbContext
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: Created EventAttendance object in memory - Id: {AttendanceId}, EventId: {EventId}, UserId: {UserId}, Type: {Type}, Status: {Status}",
    attendance.Id, attendance.EventId, attendance.UserId, attendance.AttendanceType, attendance.Status);
```

**Line 255-257**: After adding to DbContext
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: Added EventAttendance to DbContext - EntityState: {EntityState}, Id: {AttendanceId}",
    _context.Entry(attendance).State, attendance.Id);
```

**Line 309-311**: Before SaveChangesAsync
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: About to call SaveChangesAsync - Entities tracked: {TrackedCount}, Id before save: {AttendanceId}",
    _context.ChangeTracker.Entries().Count(), attendance.Id);
```

**Line 316-318**: After SaveChangesAsync
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: SaveChangesAsync completed - Rows affected: {SavedCount}, Id after save: {AttendanceId}, EntityState: {EntityState}",
    savedCount, attendance.Id, _context.Entry(attendance).State);
```

**Line 321-323**: Before defensive verification
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: Querying database for verification with Id: {AttendanceId}",
    attendance.Id);
```

**Line 331-341**: If verification fails (CRITICAL DIAGNOSTIC)
```csharp
// Query all records for this user/event to see what actually exists
var allRecords = await _context.EventAttendances
    .AsNoTracking()
    .Where(ea => ea.UserId == userId && ea.EventId == request.EventId)
    .Select(ea => new { ea.Id, ea.Status, ea.AttendanceType, ea.CreatedAt })
    .ToListAsync(cancellationToken);

_logger.LogError(
    "CRITICAL: RSVP {AttendanceId} for user {UserId} in event {EventId} failed to persist to database. " +
    "Total records for this user/event: {RecordCount}. Records: {@Records}",
    attendance.Id, userId, request.EventId, allRecords.Count, allRecords);
```

**Line 346-348**: If verification succeeds
```csharp
_logger.LogInformation(
    "DIAGNOSTIC: Verification successful - Found RSVP {AttendanceId} for user {UserId} in event {EventId} (Status: {Status}, Type: {Type}, CreatedAt: {CreatedAt})",
    savedAttendance.Id, userId, request.EventId, savedAttendance.Status, savedAttendance.AttendanceType, savedAttendance.CreatedAt);
```

### Endpoint Layer Logging (ParticipationEndpoints.cs)

**Line 113-115**: Before service call
```csharp
logger.LogInformation(
    "ENDPOINT DIAGNOSTIC: About to call CreateRSVPAsync for user {UserId} on event {EventId}",
    userId, eventId);
```

**Line 119-121**: After service call
```csharp
logger.LogInformation(
    "ENDPOINT DIAGNOSTIC: CreateRSVPAsync returned - IsSuccess: {IsSuccess}, Error: {Error}",
    result.IsSuccess, result.Error ?? "none");
```

**Line 154-156**: Before returning 201
```csharp
logger.LogInformation(
    "ENDPOINT DIAGNOSTIC: Returning 201 Created for RSVP {AttendanceId} for user {UserId} on event {EventId}",
    result.Value?.EventId, userId, eventId);
```

---

## What the Logs Will Tell Us

### Expected Log Sequence (If Bug Reproduces)

1. **ENDPOINT DIAGNOSTIC**: About to call CreateRSVPAsync
2. **DIAGNOSTIC**: Created EventAttendance object in memory - Id: `00000000-0000-0000-0000-000000000000`
3. **DIAGNOSTIC**: Added EventAttendance to DbContext - EntityState: `Added`, Id: `00000000-0000-0000-0000-000000000000`
4. **DIAGNOSTIC**: About to call SaveChangesAsync - Entities tracked: `3`, Id before save: `00000000-0000-0000-0000-000000000000`
5. **DIAGNOSTIC**: SaveChangesAsync completed - Rows affected: `X`, Id after save: `{generated-uuid}` OR `00000000-0000-0000-0000-000000000000`
6. **DIAGNOSTIC**: Querying database for verification with Id: `{id-value}`
7. **CRITICAL**: RSVP {id} failed to persist - Total records: `1`, Records: `[{old cancelled RSVP}]`

### Key Diagnostic Points

**Point 5 (SaveChangesAsync result)**:
- If `Id after save` is still `00000000-0000-0000-0000-000000000000`: Database didn't generate ID (MAJOR ISSUE)
- If `Id after save` is a valid UUID but `Rows affected = 0`: SaveChanges silently failed
- If `Id after save` is a valid UUID and `Rows affected > 0`: Record was created then rolled back

**Point 7 (All records query)**:
- If returns old cancelled RSVP only: New record never persisted
- If returns NEW Active RSVP: Defensive check query is wrong
- If returns nothing: User/Event combination has no records at all

---

## Proposed Fixes (After Log Analysis)

### Fix 1: AttendanceHistory Foreign Key Issue
**Change AttendanceHistory creation to happen AFTER SaveChanges**

**Current (BROKEN)**:
```csharp
// Line 286-299: Create history BEFORE SaveChanges
var history = new AttendanceHistory(attendance.Id, "Created")
{
    // ... attendance.Id is Guid.Empty here
};
_context.AttendanceHistory.Add(history);
await _context.SaveChangesAsync(cancellationToken);
```

**Fixed**:
```csharp
// Save EventAttendance FIRST to generate ID
await _context.SaveChangesAsync(cancellationToken);

// NOW create history with correct ID
var history = new AttendanceHistory(attendance.Id, "Created")
{
    NewValues = System.Text.Json.JsonSerializer.Serialize(new
    {
        EventId = attendance.EventId,
        UserId = attendance.UserId,
        AttendanceType = attendance.AttendanceType,
        Notes = attendance.Notes
    }),
    ChangedBy = userId,
    ChangeReason = "RSVP created by user"
};
_context.AttendanceHistory.Add(history);

// Save history separately
await _context.SaveChangesAsync(cancellationToken);
```

### Fix 2: Ensure ID Generation Configuration
**Verify PostgreSQL is configured for UUID generation**

Check database migration for:
```sql
ALTER TABLE "EventAttendances"
ALTER COLUMN "Id" SET DEFAULT gen_random_uuid();
```

If missing, create migration to add default UUID generation.

### Fix 3: Explicit Transaction Control
**Wrap entire operation in explicit transaction for atomicity**

```csharp
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

try
{
    // Create EventAttendance
    _context.EventAttendances.Add(attendance);

    // Create EventAttendee
    _context.EventAttendees.Add(attendee);

    // Save to generate IDs
    await _context.SaveChangesAsync(cancellationToken);

    // Create AttendanceHistory with generated ID
    var history = new AttendanceHistory(attendance.Id, "Created") { ... };
    _context.AttendanceHistory.Add(history);

    // Save history
    await _context.SaveChangesAsync(cancellationToken);

    // Commit transaction
    await transaction.CommitAsync(cancellationToken);
}
catch (Exception ex)
{
    await transaction.RollbackAsync(cancellationToken);
    _logger.LogError(ex, "Transaction rolled back during RSVP creation");
    throw;
}
```

---

## Next Steps

1. **Run tests with enhanced logging** to capture diagnostic output
2. **Analyze logs** to confirm root cause hypothesis
3. **Implement appropriate fix** based on log analysis
4. **Re-test** to verify fix
5. **Remove diagnostic logging** (or keep as debug-level logs)

---

## Files Modified

### /home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs
- Added 8 diagnostic log statements in `CreateRSVPAsync` method
- Lines: 249-251, 255-257, 309-311, 316-318, 321-323, 331-341, 346-348

### /home/chad/repos/witchcityrope/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs
- Added 3 diagnostic log statements in RSVP endpoint
- Lines: 113-115, 119-121, 154-156

---

## Related Code References

- **Entity**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`
- **Configuration**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Configuration/EventAttendanceConfiguration.cs`
- **Endpoint**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`
- **Service**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs`

---

## Confidence Level

**Root Cause**: 85% confident the issue is AttendanceHistory foreign key constraint
**Fix Effectiveness**: 90% confident Fix 1 will resolve the issue
**Risk**: Low - Enhanced logging has zero risk, proposed fix is standard EF Core pattern
