# Backend Developer Lessons Learned - Part 4

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 4 of 4**
**Part 1**: backend-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Part 2**: backend-developer-lessons-learned-2.md (MUST READ)
**Part 3**: backend-developer-lessons-learned-3.md (MUST READ)
**Part 4**: backend-developer-lessons-learned-4.md (THIS FILE)
**Read ALL**: Parts 1, 2, 3, AND 4 are MANDATORY
**Write to**: Part 4 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

---

## 🚨 CRITICAL: Missing ThenInclude for Navigation Properties Causes Null Values 🚨

**Problem**: Calculated properties returning null even though navigation properties are included
**Date**: 2025-11-08
**Impact**: All sold counts showing null in API responses despite EventAttendances existing in database

**Root Cause**:
- EventService included `.Include(e => e.EventAttendances)` to load attendance records
- But Session.CurrentAttendees and TicketType.Sold calculated properties need nested navigation properties:
  - `ea.TicketPurchase` (to check if it's not null)
  - `ea.TicketPurchase.TicketType` (to match session/ticket type)
- Without `.ThenInclude()`, these nested properties were null
- Caused calculated properties to return null instead of actual counts

**Specific Calculated Properties Affected**:

1. **Session.CurrentAttendees** (`apps/api/Models/Session.cs:71-86`):
```csharp
// Needs ea.TicketPurchase and ea.TicketPurchase.TicketType.SessionId
public int CurrentAttendees
{
    get
    {
        if (Event?.EventAttendances == null) return 0;

        return Event.EventAttendances.Count(ea =>
            ea.Status == AttendanceStatus.Active &&
            ea.AttendanceType == AttendanceType.Ticket &&
            ea.TicketPurchase != null &&  // ← NULL without ThenInclude
            (ea.TicketPurchase.TicketType.SessionId == Id ||  // ← NULL
             (ea.TicketPurchase.TicketType.SessionId == null &&
              ea.TicketPurchase.TicketType.EventId == EventId)));
    }
}
```

2. **TicketType.Sold** (`apps/api/Models/TicketType.cs`):
```csharp
// Similar dependency on TicketPurchase navigation
```

**Solution**: Add ThenInclude chain for complete navigation property loading

**Fix Applied** (`apps/api/Features/Events/Services/EventService.cs`):

```csharp
// ❌ BEFORE - Loads EventAttendances but NOT nested properties
.Include(e => e.EventAttendances)

// ✅ AFTER - Loads full navigation chain
.Include(e => e.EventAttendances)
    .ThenInclude(ea => ea.TicketPurchase)  // Load purchase for each attendance
        .ThenInclude(tp => tp.TicketType);  // Load ticket type for each purchase
```

**Applied to Both Query Methods**:
1. `GetEventsAsync()` - Line 59-61
2. `GetEventAsync()` - Line 144-146

**Testing Verification**:
```bash
# BEFORE fix
curl http://localhost:5655/api/events | jq '.data[0].sessions[0].registrationCount'
# Output: null

# AFTER fix
curl http://localhost:5655/api/events | jq '.data[0].sessions[0].registrationCount'
# Output: 5

# TicketType sold counts also working
curl http://localhost:5655/api/events | jq '.data[0].ticketTypes[0].quantitySold'
# Output: 5
```

**Integration Tests**: 47/71 passing (no regressions, all failures pre-existing auth issues)

**Prevention**:
1. **When using calculated properties**, trace ALL navigation properties accessed
2. **For nested access** (e.g., `ea.TicketPurchase.TicketType.SessionId`), need TWO `.ThenInclude()` calls
3. **Test calculated properties** after adding Include statements - verify they return values, not null
4. **Use AsNoTracking queries** with caution - ensure all needed navigation loaded upfront
5. **Check for null** in calculated properties AND ensure Include chain loads the data

**Pattern for Nested Navigation**:
```csharp
// For property: entity.Child.GrandChild.Property
.Include(e => e.Children)           // First level
    .ThenInclude(c => c.GrandChild) // Second level
        .ThenInclude(gc => gc.Property); // Third level if needed
```

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs` (lines 59-61, 144-146)

**Related Issue**: Seed data was also missing initially (separate fix)

---
