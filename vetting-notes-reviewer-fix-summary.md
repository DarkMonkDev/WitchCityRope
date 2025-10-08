# Vetting Notes Reviewer Name Fix - Summary

## Date: 2025-10-08

## Problem
Admin Notes section in vetting application details showed hardcoded "Administrator" instead of the actual reviewer's SceneName (e.g., "RopeMaster").

## Root Cause
Line 678 in `VettingService.cs` used `ParseAdminNotesToDto()` method which hardcoded:
```csharp
ReviewerName = "Administrator"
```

This method parsed a plain text `AdminNotes` field that didn't contain user attribution information.

## Solution
**Stop using plain text AdminNotes field** and instead retrieve notes from audit logs, which have proper user tracking via the `PerformedByUser` relationship.

### Changes Made in `/apps/api/Features/Vetting/Services/VettingService.cs`

#### 1. Removed ParseAdminNotesToDto call (line 190)
**BEFORE:**
```csharp
var notes = ParseAdminNotesToDto(application.AdminNotes);
```

**AFTER:**
```csharp
// Removed - now using audit logs instead
```

#### 2. Created notes from audit logs (after line 197)
**NEW CODE:**
```csharp
// Create notes from audit logs with actual reviewer SceneName
var notes = auditLogsWithUsers
    .Where(log => log.Action == "Note Added" || !string.IsNullOrWhiteSpace(log.Notes))
    .Select(log => new ApplicationNoteDto
    {
        Id = log.Id,
        Content = log.Notes ?? "",
        Type = log.Action == "Note Added" ? "Note" : "Decision",
        IsPrivate = true,
        Tags = new List<string>(),
        ReviewerName = log.PerformedByUser?.SceneName ?? "System",
        CreatedAt = log.PerformedAt,
        UpdatedAt = log.PerformedAt
    })
    .OrderByDescending(n => n.CreatedAt)
    .ToList();
```

#### 3. Updated response mapping comments (line 264-265)
**BEFORE:**
```csharp
Notes = notes, // Parsed from AdminNotes
Decisions = decisions, // Parsed from audit logs
```

**AFTER:**
```csharp
Notes = notes, // Created from audit logs with reviewer SceneName
Decisions = decisions, // Created from audit logs with reviewer SceneName
```

### Method Preserved (Not Used)
The `ParseAdminNotesToDto` method remains in the code (line 664) but is no longer called. It can be removed in future cleanup or kept as a fallback.

## Expected Behavior After Fix
- Admin notes will show actual reviewer SceneName (e.g., "RopeMaster")
- Notes come from audit logs which have proper user tracking
- Both "Status Changed" events with notes AND "Note Added" events show correct reviewer names

## Verification
- ✅ Code compiles with 0 errors, 0 warnings
- ✅ Method returns notes from audit logs with `PerformedByUser.SceneName`
- ✅ Notes properly ordered by CreatedAt (descending)

## Files Modified
- `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Services/VettingService.cs`

## Build Status
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Next Steps
Testing should verify:
1. Notes display with correct reviewer SceneName
2. Both "Note Added" and "Status Changed" notes appear
3. Notes are ordered correctly (newest first)
