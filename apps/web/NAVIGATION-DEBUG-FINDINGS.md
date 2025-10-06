# Navigation Debug Findings - Vetting Admin Page

## Executive Summary

**Problem:** Navigation on `/admin/vetting` is completely broken. URL changes but page doesn't re-render.

**Root Cause (Suspected):** The `SendReminderModal` component is always rendered and always running the `useVettingApplications` React Query hook, which may be preventing React Router's `<Outlet />` from re-rendering on route changes.

**Confidence Level:** 80%

**Recommended Fix:** Conditionally render `SendReminderModal` only when `sendReminderModalOpen === true`

---

## Detailed Findings

### 1. SendReminderModal Always Mounted ⚠️

**Location:** `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminVettingPage.tsx:186-195`

**Current Code:**
```tsx
<SendReminderModal
  opened={sendReminderModalOpen}
  onClose={() => setSendReminderModalOpen(false)}
  onSuccess={() => {
    setSelectedApplications(new Set());
    setSelectedApplicationsData([]);
  }}
/>
```

**Problem:**
- Component is **ALWAYS** in the React tree
- Its `useVettingApplications` hook runs continuously
- Creates React Query subscription that never unsubscribes
- May cause constant re-renders of parent component

**Evidence:**
```tsx
// Inside SendReminderModal.tsx:42
const { data: applicationsData } = useVettingApplications(filters);
```

This hook runs even when the modal is closed!

---

### 2. Comparison with OnHoldModal ✓

**Location:** Same file, lines 170-184

**Code:**
```tsx
{hasSelectedApplications && selectedApplicationsData.length > 0 && (
  <OnHoldModal
    opened={onHoldModalOpen}
    onClose={() => setOnHoldModalOpen(false)}
    ...
  />
)}
```

**Finding:** `OnHoldModal` is **conditionally rendered** - only exists when needed.
- No data fetching when closed
- Clean component lifecycle
- **This pattern works correctly**

---

### 3. Navigation Code is Correct ✓

**Location:** `/home/chad/repos/witchcityrope/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx:101-115`

```tsx
const handleRowClick = useCallback((applicationId: string) => {
  console.log('VettingApplicationsList: Row click navigation:', {
    applicationId,
    url: `/admin/vetting/applications/${applicationId}`,
    timestamp: new Date().toISOString()
  });

  try {
    navigate(`/admin/vetting/applications/${applicationId}`);
    console.log('VettingApplicationsList: Navigation called successfully');
  } catch (error) {
    console.error('VettingApplicationsList: Navigation failed:', error);
  }
}, [navigate]);
```

**Finding:** Navigation code is well-structured with proper error handling and logging.

---

### 4. React Router Configuration is Correct ✓

**Location:** `/home/chad/repos/witchcityrope/apps/web/src/routes/router.tsx:256-264`

```tsx
{
  path: "admin/vetting",
  element: <AdminVettingPage />,
  loader: adminLoader
},
{
  path: "admin/vetting/applications/:applicationId",
  element: <AdminVettingApplicationDetailPage />,
  loader: adminLoader
},
```

**Finding:** Routes are properly configured.

---

### 5. RootLayout Uses Outlet Correctly ✓

**Location:** `/home/chad/repos/witchcityrope/apps/web/src/components/layout/RootLayout.tsx:25-27`

```tsx
<Box component="main">
  <Outlet />
</Box>
```

**Finding:** `<Outlet />` is in the right place. However, if parent component (`AdminVettingPage`) is constantly re-rendering due to React Query updates, the Outlet may not detect route changes properly.

---

## The Problem Mechanism

Here's what we believe is happening:

1. User navigates to `/admin/vetting`
2. `AdminVettingPage` renders
3. `AdminVettingPage` **always** renders `SendReminderModal`
4. `SendReminderModal` calls `useVettingApplications` hook
5. React Query creates a subscription and fetches data
6. Data updates cause `SendReminderModal` to re-render
7. This causes `AdminVettingPage` to re-render
8. Re-render cycle may be preventing React Router from detecting URL changes
9. When user clicks a row:
   - `navigate()` is called successfully
   - URL changes
   - But `<Outlet />` doesn't re-render because parent is in a render loop
   - Page appears "frozen" with old content

---

## Why This Matters

### React Query + React Router Interaction

React Query and React Router both manage state independently:
- **React Query:** Manages server state and triggers re-renders when data updates
- **React Router:** Manages route state and triggers re-renders when URL changes

When both try to re-render the same component tree simultaneously, **race conditions** can occur where:
- React Router's navigation is queued
- React Query's data update renders first
- React Router's navigation event is lost
- Component shows old content even though URL changed

---

## The Fix

### Recommended Solution: Conditional Rendering

**Change in:** `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminVettingPage.tsx`

**Before:**
```tsx
{/* Send Reminder works independently of selections */}
<SendReminderModal
  opened={sendReminderModalOpen}
  onClose={() => setSendReminderModalOpen(false)}
  onSuccess={() => {
    setSelectedApplications(new Set());
    setSelectedApplicationsData([]);
  }}
/>
```

**After:**
```tsx
{/* Send Reminder Modal - Only render when open */}
{sendReminderModalOpen && (
  <SendReminderModal
    opened={sendReminderModalOpen}
    onClose={() => setSendReminderModalOpen(false)}
    onSuccess={() => {
      setSelectedApplications(new Set());
      setSelectedApplicationsData([]);
    }}
  />
)}
```

**Why This Works:**
- Modal only mounts when `sendReminderModalOpen === true`
- `useVettingApplications` hook only runs when modal is open
- No continuous React Query subscription
- No constant re-renders
- React Router can properly detect and respond to navigation
- Matches the pattern used for `OnHoldModal` (which works)

---

## Testing Instructions

### Before Fix Test
1. Open http://localhost:5173/admin/vetting
2. Open DevTools Console
3. Click a table row
4. **Expected:** Console shows "Navigation called successfully" but page doesn't change
5. Check Network tab - may see API calls from SendReminderModal's hook

### After Fix Test
1. Apply the fix (conditional rendering)
2. Refresh the page
3. Click a table row
4. **Expected:** Page navigates to detail view immediately
5. Check Network tab - should NOT see unnecessary API calls

### Verify Modal Still Works
1. Click "SEND REMINDER" button
2. Modal should open (and fetch data at this point)
3. Should see pending applications
4. Click "CANCEL"
5. Modal should close and unmount

---

## Alternative Fixes (Not Recommended)

### Alternative 1: Move Hook to Parent
Move `useVettingApplications` to `AdminVettingPage` and pass data as props.

**Pros:** Data is pre-loaded
**Cons:** Makes unnecessary API call even when modal never opens

### Alternative 2: Lazy Load Data
Use React Query's `enabled` option to prevent fetching when modal is closed.

**Pros:** Modal stays mounted but doesn't fetch
**Cons:** More complex, doesn't address the core issue of unnecessary mounting

### Alternative 3: Use React Router's Key Prop
Force Outlet to remount on every route change.

**Pros:** Forces re-render
**Cons:** Loses component state, not a proper fix

---

## Files to Modify

1. `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminVettingPage.tsx`
   - Add conditional rendering for `SendReminderModal`
   - Match pattern used for `OnHoldModal`

---

## Debug Tools Created

1. **`/home/chad/repos/witchcityrope/apps/web/debug-navigation.js`**
   - Comprehensive browser console script
   - Checks for overlays, modals, z-index issues
   - Monitors click events and React Router navigation
   - Run in browser console on the vetting page

2. **`/tmp/navigation-test.html`**
   - Diagnostic page with instructions
   - Opened in Chrome for reference

---

## Verification Checklist

After applying the fix:

- [ ] Navigation works on vetting list page
- [ ] Clicking table rows navigates to detail page
- [ ] Nav menu links work correctly
- [ ] "SEND REMINDER" button still opens modal
- [ ] Modal fetches data when opened
- [ ] Modal closes properly
- [ ] No console errors
- [ ] No unnecessary API calls when modal is closed
- [ ] Compare behavior with /admin/events (reference implementation)

---

## Related Documentation

- **File Registry:** `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md`
- **React Router Docs:** https://reactrouter.com/en/main/components/outlet
- **React Query Docs:** https://tanstack.com/query/latest/docs/react/guides/window-focus-refetching
- **Mantine Modal Docs:** https://mantine.dev/core/modal/

---

## Confidence Assessment

**90% confident** this is the root cause based on:
1. Code pattern difference (OnHoldModal is conditional, SendReminderModal is not)
2. React Query + React Router interaction is a known issue
3. Continuous hook execution aligns with symptoms
4. Fix is minimal, low-risk, and follows established patterns

**To confirm:** Apply the fix and test. If navigation still doesn't work, investigate:
- Browser DevTools for actual DOM overlay issues
- React Router's internal state
- Mantine Portal configuration
- Event propagation in table rows
