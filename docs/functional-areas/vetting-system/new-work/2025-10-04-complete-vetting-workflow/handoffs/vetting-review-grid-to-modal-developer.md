# Handoff: VettingReviewGrid Quick Actions - Modal Implementation Needed

**From**: React Developer Agent
**To**: React Developer (Next Session)
**Date**: 2025-10-04
**Status**: Grid Complete, Modals Needed

## What's Complete ✅

### VettingReviewGrid Component
**Location**: `/apps/web/src/features/admin/vetting/components/VettingReviewGrid.tsx`

The main admin review grid is **production-ready** with:
- ✅ Data grid with filtering, sorting, search
- ✅ Pagination (25 items per page)
- ✅ Status badges and row navigation
- ✅ Quick actions menu with placeholder handlers
- ✅ Loading, error, and empty states
- ✅ TypeScript strict compliance
- ✅ Build successful
- ✅ Documentation complete

### What Works Now
1. **Grid displays applications** from API
2. **Search** by scene name or email
3. **Filter** by status dropdown
4. **Click row** → navigates to detail page
5. **Actions menu** → currently logs to console

## What's Needed ❌

### Quick Action Modals
Five modal components need to be implemented and wired up:

#### 1. Approve Application Modal
**Handler**: `handleApprove` (line 130)
```typescript
const handleApprove = useCallback((applicationId: string) => {
  // TODO: Open approval modal
  console.log('Approve:', applicationId);
}, []);
```

**Requirements**:
- Confirmation dialog
- Optional reasoning text area
- Calls `vettingAdminApi.approveApplication(id, reasoning)`
- Shows success notification
- Refreshes grid data
- Handles errors gracefully

**Reference**: Check if ApproveApplicationModal exists or create new

---

#### 2. Deny Application Modal
**Handler**: `handleDeny` (line 135)
```typescript
const handleDeny = useCallback((applicationId: string) => {
  // TODO: Open deny modal
  console.log('Deny:', applicationId);
}, []);
```

**Status**: ✅ **Modal already exists!**
**Location**: `/apps/web/src/features/admin/vetting/components/DenyApplicationModal.tsx`

**TODO**: Import and wire up existing modal

---

#### 3. Put On Hold Modal
**Handler**: `handlePutOnHold` (line 140)
```typescript
const handlePutOnHold = useCallback((applicationId: string) => {
  // TODO: Open on-hold modal
  console.log('Put On Hold:', applicationId);
}, []);
```

**Status**: ✅ **Modal already exists!**
**Location**: `/apps/web/src/features/admin/vetting/components/OnHoldModal.tsx`

**TODO**: Import and wire up existing modal

---

#### 4. Schedule Interview Modal
**Handler**: `handleScheduleInterview` (line 145)
```typescript
const handleScheduleInterview = useCallback((applicationId: string) => {
  // TODO: Open schedule interview modal
  console.log('Schedule Interview:', applicationId);
}, []);
```

**Requirements**:
- Date/time picker
- Optional notes field
- Calls API to schedule interview
- Sends notification email to applicant
- Updates application status

**Reference**: Check if exists, create if needed

---

#### 5. Send Reminder Modal
**Handler**: `handleSendReminder` (line 150)
```typescript
const handleSendReminder = useCallback((applicationId: string) => {
  // TODO: Open send reminder modal
  console.log('Send Reminder:', applicationId);
}, []);
```

**Status**: ✅ **Modal already exists!**
**Location**: `/apps/web/src/features/admin/vetting/components/SendReminderModal.tsx`

**TODO**: Import and wire up existing modal

---

## Implementation Steps

### Step 1: Check Existing Modals
```bash
# List existing modal components
ls -la /apps/web/src/features/admin/vetting/components/*Modal.tsx
```

**Known existing**:
- ✅ DenyApplicationModal
- ✅ OnHoldModal
- ✅ SendReminderModal
- ❓ ApproveApplicationModal (check if exists)
- ❓ ScheduleInterviewModal (check if exists)

### Step 2: Wire Up Existing Modals

Add modal state to VettingReviewGrid:
```typescript
// Add at top of component
const [denyModalOpen, setDenyModalOpen] = useState(false);
const [holdModalOpen, setHoldModalOpen] = useState(false);
const [reminderModalOpen, setReminderModalOpen] = useState(false);
const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);

// Update handlers
const handleDeny = useCallback((applicationId: string) => {
  setSelectedApplicationId(applicationId);
  setDenyModalOpen(true);
}, []);

// Add modal components at bottom
<DenyApplicationModal
  applicationId={selectedApplicationId}
  opened={denyModalOpen}
  onClose={() => {
    setDenyModalOpen(false);
    setSelectedApplicationId(null);
  }}
  onSuccess={() => {
    refetch();
    onActionComplete?.();
  }}
/>
```

### Step 3: Create Missing Modals

If ApproveApplicationModal or ScheduleInterviewModal don't exist:

**Create ApproveApplicationModal.tsx**:
```typescript
interface ApproveApplicationModalProps {
  applicationId: string | null;
  opened: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ApproveApplicationModal: React.FC<ApproveApplicationModalProps> = ({
  applicationId,
  opened,
  onClose,
  onSuccess
}) => {
  // Similar pattern to DenyApplicationModal
  // Use vettingAdminApi.approveApplication()
};
```

### Step 4: Import Modals
```typescript
import { DenyApplicationModal } from './DenyApplicationModal';
import { OnHoldModal } from './OnHoldModal';
import { SendReminderModal } from './SendReminderModal';
// Import others as created
```

### Step 5: Test Flow
1. Click action menu on any row
2. Select action (Approve, Deny, etc.)
3. Modal opens with form
4. Fill out form and submit
5. API call executes
6. Success notification shows
7. Grid refreshes
8. Modal closes

## Data Flow

```
User clicks action
  ↓
Handler sets state (applicationId, modalOpen)
  ↓
Modal renders with applicationId
  ↓
User fills form and submits
  ↓
API call (vettingAdminApi.{action}Application)
  ↓
Success → refetch grid + notification + close modal
Error → show error in modal
```

## API Endpoints Available

From `vettingAdminApi.ts`:
```typescript
// Already implemented
vettingAdminApi.approveApplication(applicationId, reasoning)
vettingAdminApi.denyApplication(applicationId, reasoning)
vettingAdminApi.putApplicationOnHold(applicationId, reason)
vettingAdminApi.sendApplicationReminder(applicationId, message) // Simulated
// TODO: Add scheduleInterview if doesn't exist
```

## Files to Modify

1. **VettingReviewGrid.tsx**:
   - Add modal state
   - Update handlers to open modals
   - Add modal components at bottom

2. **Create missing modals** (if needed):
   - ApproveApplicationModal.tsx
   - ScheduleInterviewModal.tsx

3. **Update index.ts** with new modal exports

## Testing Checklist

After implementation:
- [ ] Each modal opens when action clicked
- [ ] Modals show correct application data
- [ ] Form validation works
- [ ] API calls execute successfully
- [ ] Success notifications appear
- [ ] Grid refreshes after action
- [ ] Modals close properly
- [ ] Error handling works
- [ ] Can cancel without changes
- [ ] Loading states show during API calls

## Design References

**UI/UX Spec**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/design/ui-ux-specification.md`

**Modal patterns**:
- Confirmation dialogs with reason/notes
- Primary action button (colored)
- Cancel button (subtle)
- Loading state during API call
- Error alert if API fails
- Success notification on complete

## Current Build Status

✅ **Build successful**: `npm run build` passes
✅ **No TypeScript errors**
✅ **All imports resolve**
✅ **Grid renders correctly**

## Questions to Answer

1. Does ApproveApplicationModal exist?
2. Does ScheduleInterviewModal exist?
3. Do modals need approval workflow (multi-step)?
4. Should modals handle their own API calls or use passed callbacks?
5. Should we add bulk operations (select multiple, apply action)?

## Next Developer Notes

**Start here**:
1. Read VettingReviewGrid.tsx (lines 130-155) for handler TODO comments
2. Check existing modal components for patterns
3. Wire up existing modals first (quick win)
4. Create missing modals following existing patterns
5. Test each modal individually
6. Test complete workflow end-to-end

**Estimated time**: 2-4 hours
- Wire up existing: 30 min
- Create missing: 1-2 hours
- Testing: 1 hour

## Related Documentation

- **Component README**: `/apps/web/src/features/admin/vetting/components/VettingReviewGrid.README.md`
- **Implementation Summary**: `/session-work/2025-10-04/vetting-review-grid-implementation-summary.md`
- **UI Specification**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/design/ui-ux-specification.md`

## Success Criteria

When complete, admin should be able to:
- ✅ View all applications in grid
- ✅ Search and filter applications
- ✅ Click row to view details
- ✅ Click "Approve" → modal → approve application
- ✅ Click "Deny" → modal → deny application
- ✅ Click "Put On Hold" → modal → place on hold
- ✅ Click "Schedule Interview" → modal → schedule interview
- ✅ Click "Send Reminder" → modal → send reminder
- ✅ See success notifications
- ✅ See grid refresh with updated data

## Contact / Questions

For questions about this handoff:
- Review the component README
- Check existing modal implementations
- Follow patterns from DenyApplicationModal (most complete example)
- Consult React developer lessons learned for modal patterns
