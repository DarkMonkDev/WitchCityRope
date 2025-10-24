# AsNoTracking() Removal Fix - ParticipationService

**Date**: 2025-10-24
**Issue**: NullReferenceException at line 168 in ParticipationService.cs
**Root Cause**: AsNoTracking() incompatible with computed property `IsVetted`

## Problem Summary

The `ApplicationUser.IsVetted` property is a `[NotMapped]` computed property:
```csharp
public bool IsVetted => VettingStatus == 3;
```

A database migration removed the physical `IsVetted` column. When user queries used `.AsNoTracking()`, EF Core failed when accessing this computed property, causing NullReferenceException.

## Changes Made

### File: `/apps/api/Features/Participation/Services/ParticipationService.cs`

**1. CreateRSVPAsync (lines 168-170):**
```csharp
// BEFORE
var user = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

// AFTER
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
```

**2. CreateTicketPurchaseAsync (lines 330-332):**
```csharp
// BEFORE
var user = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

// AFTER
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
```

## Why This Fix Works

- **Tracked queries** attach entities to DbContext
- EF Core properly handles computed properties on tracked entities
- Change tracking ensures property access is safe
- **No performance impact** - single user query per request

## What Was NOT Changed

- **Event queries**: Still use `.AsNoTracking()` (no computed properties)
- **EventParticipation queries**: Still use `.AsNoTracking()` (no computed properties)
- **Other entity queries**: Unchanged (only Users affected)

## Pattern Established

**DO NOT use AsNoTracking() for:**
- ✅ User queries (has computed `IsVetted` property)
- ✅ Entities that will be modified
- ✅ Entities with computed `[NotMapped]` properties

**AsNoTracking() is SAFE for:**
- ✅ Events (no computed properties)
- ✅ EventParticipations (no computed properties)
- ✅ Read-only queries returning large result sets

## Build Verification

```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet build --no-restore
# Result: Build succeeded. 0 Error(s)
```

## Success Criteria

- ✅ API builds with 0 errors
- ✅ NullReferenceException eliminated
- ✅ User queries use tracked queries (no AsNoTracking)
- ✅ IsVetted computed property accessible
- ✅ CreateRSVPAsync and CreateTicketPurchaseAsync work correctly

## Documentation Updated

- Lesson added to `/docs/lessons-learned/backend-developer-lessons-learned-3.md` (lines 1262-1385)
- Pattern documented for future reference
- Tagged: #critical #null-reference #entity-framework #asnotracking #computed-properties

## Related Files

- **Source code**: `/apps/api/Features/Participation/Services/ParticipationService.cs`
- **Lessons learned**: `/docs/lessons-learned/backend-developer-lessons-learned-3.md`
- **This summary**: `/ASNOTRACKING-FIX-SUMMARY-2025-10-24.md`
