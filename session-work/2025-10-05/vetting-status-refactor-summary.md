# Vetting Status Enum Refactor Summary
**Date**: 2025-10-05
**Task**: Refactor VettingStatus enum to remove redundant statuses and implement cleaner workflow

## Changes Completed

### 1. Enum Definition (Already Done by User)
Updated `/apps/api/Features/Vetting/Entities/VettingApplication.cs`

**Old Enum (10 values):**
```csharp
Draft = 0, Submitted = 1, UnderReview = 2, InterviewApproved = 3,
PendingInterview = 4, InterviewScheduled = 5, OnHold = 6, Approved = 7, Denied = 8, Withdrawn = 9
```

**New Enum (8 values):**
```csharp
UnderReview = 0, InterviewApproved = 1, InterviewScheduled = 2, FinalReview = 3,
Approved = 4, Denied = 5, OnHold = 6, Withdrawn = 7
```

### 2. Status Mapping Strategy

| Old Status | Old Value | New Status | New Value | Notes |
|------------|-----------|------------|-----------|-------|
| Draft | 0 | UnderReview | 0 | Removed - applications start in review |
| Submitted | 1 | UnderReview | 0 | Merged with UnderReview |
| UnderReview | 2 | UnderReview | 0 | Renumbered |
| InterviewApproved | 3 | InterviewApproved | 1 | Renumbered |
| PendingInterview | 4 | InterviewApproved | 1 | Merged with InterviewApproved |
| InterviewScheduled | 5 | InterviewScheduled | 2 | Renumbered |
| OnHold | 6 | OnHold | 6 | Unchanged |
| Approved | 7 | Approved | 4 | Renumbered |
| Denied | 8 | Denied | 5 | Renumbered |
| Withdrawn | 9 | Withdrawn | 7 | Renumbered |
| N/A | N/A | FinalReview | 3 | NEW - post-interview review |

### 3. New Workflow

```
1. Application submitted → UnderReview (0)
2. Team reviews → InterviewApproved (1)
3. Interview scheduled → InterviewScheduled (2)
4. Interview completed → FinalReview (3) ← NEW STATUS
5. Final decision → Approved (4), Denied (5), or OnHold (6)
6. User can withdraw → Withdrawn (7)
```

**Terminal States:** Approved, Denied, Withdrawn (no further transitions)
**Hold State:** OnHold can return to UnderReview or InterviewApproved

### 4. Files Modified

#### Core Entity Files
1. `/apps/api/Features/Vetting/Entities/VettingApplication.cs`
   - Updated constructor default: `Draft` → `UnderReview`
   - Added comprehensive workflow documentation in header comments

#### Service Files
2. `/apps/api/Services/SeedDataService.cs`
   - Updated seed data status values with comments
   - Updated audit log generation to reflect new workflow
   - Removed `Draft → Submitted` transitions
   - Added `FinalReview` status in approval workflow

3. `/apps/api/Features/Vetting/Services/VettingService.cs`
   - Updated `GetStatusDescription()` method
   - Updated `GetNextSteps()` method
   - Updated `ValidateStatusTransition()` with new workflow rules
   - Updated terminal state checks to include `Withdrawn`
   - Updated `SubmitReviewDecision()` decision type mapping
   - Updated `GetEstimatedDaysRemaining()`
   - Updated `GetCurrentPhase()` method
   - Updated progress percentage calculations
   - Changed application creation status: `Submitted` → `UnderReview` (2 occurrences)

4. `/apps/api/Features/Vetting/Services/VettingEmailService.cs`
   - Updated `GetTemplateTypeForStatus()` - added FinalReview (no email)
   - Updated `GetStatusDescription()` method
   - Updated `GetNextStepsForStatus()` method

5. `/apps/api/Features/Vetting/Endpoints/VettingEndpoints.cs`
   - Updated `GetStatusMessage()` helper method
   - Updated `GetNextStepsMessage()` helper method

### 5. State Transition Rules

```csharp
// Valid transitions defined in ValidateStatusTransition()
UnderReview → InterviewApproved, OnHold, Denied, Withdrawn
InterviewApproved → InterviewScheduled, OnHold, Withdrawn
InterviewScheduled → FinalReview, OnHold, Withdrawn
FinalReview → Approved, Denied, OnHold, Withdrawn  // NEW
OnHold → UnderReview, InterviewApproved, Denied, Withdrawn
Approved → (none - terminal)
Denied → (none - terminal)
Withdrawn → (none - terminal)
```

### 6. Database Migration

**File Created:** `/apps/api/Infrastructure/Data/Migrations/20251005000000_RefactorVettingStatusEnum.cs`

**Migration Strategy:**
1. Add temporary `StatusTemp` column
2. Map old values to new values using SQL UPDATE statements
3. Drop old `Status` column
4. Rename `StatusTemp` to `Status`
5. Update `VettingAuditLogs` string values (Draft→UnderReview, Submitted→UnderReview, PendingInterview→InterviewApproved)

**Rollback Support:** Includes `Down()` migration to restore old enum values (best-effort)

### 7. Email Templates

Updated to handle new statuses:
- **UnderReview** - Initial review email
- **InterviewApproved** - Interview approval email
- **InterviewScheduled** - Interview details email
- **FinalReview** - No email (internal status)
- **Approved** - Approval email
- **Denied** - Denial email
- **OnHold** - On-hold email
- **Withdrawn** - No email

### 8. Progress Tracking

Updated progress percentages:
- UnderReview: 20%
- InterviewApproved: 40%
- InterviewScheduled: 60%
- FinalReview: 80% ← NEW
- Approved/Denied/Withdrawn: 100%
- OnHold: 50%

## Migration Commands

### To Apply Migration
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add RefactorVettingStatusEnum --context ApplicationDbContext --output-dir Infrastructure/Data/Migrations
dotnet ef database update --context ApplicationDbContext
```

### To Rollback (if needed)
```bash
dotnet ef database update <PreviousMigrationName> --context ApplicationDbContext
```

## Testing Recommendations

1. **Unit Tests** - Update to use new enum values
2. **Integration Tests** - Verify status transitions work correctly
3. **E2E Tests** - Test full vetting workflow from submission to approval
4. **Database Migration** - Test on staging environment before production

## Potential Issues & Conflicts

### No Major Issues Found
- All C# code references updated
- Email templates updated
- Status messages updated
- State transitions properly defined
- Migration handles data transformation

### Notes
- **Frontend NOT updated** - React/TypeScript components will need separate updates
- **Admin UI NOT updated** - Admin vetting components need status display updates
- **Tests NOT updated** - Existing tests may reference old enum values

## Next Steps (For Frontend/Test Teams)

1. **React Developer**: Update TypeScript enum and all status references
2. **Test Developer**: Update test data and assertions to use new enum values
3. **Database Admin**: Review and approve migration script
4. **Test Executor**: Run integration and E2E tests after migration

## Files NOT Modified (As Requested)

- `/apps/web/**` - All React/TypeScript files
- `/tests/**` - All test files
- Admin UI components

## Summary

✅ All C# backend code updated
✅ Database migration created
✅ State transitions properly defined
✅ Email templates updated
✅ Comprehensive workflow documentation added
✅ No compilation errors expected

⏳ Frontend updates required (separate task)
⏳ Test updates required (separate task)
⏳ Database migration needs review and execution
