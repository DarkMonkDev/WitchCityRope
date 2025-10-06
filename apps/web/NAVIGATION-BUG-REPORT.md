# Vetting Admin Page Navigation Bug Report

## Problem Description
At http://localhost:5173/admin/vetting, ALL navigation is broken:
- Clicking table rows changes URL but page doesn't load/render
- Nav menu links don't work
- User must manually refresh browser to see new pages

## Analysis Date
2025-10-05

## Symptoms
1. URL in address bar changes when clicking links or table rows
2. Page content does not update to reflect the new URL
3. Manual browser refresh shows the correct page for the URL
4. Console logs show that `navigate()` is being called successfully
5. Issue affects ALL navigation on the page (table rows, nav menu, etc.)

## Code Review Findings

### 1. VettingApplicationsList Component
**File:** `/home/chad/repos/witchcityrope/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx`

**Row Click Handler (Lines 101-115):**
```tsx
const handleRowClick = useCallback((applicationId: string) => {
  console.log('VettingApplicationsList: Row click navigation:', {
    applicationId,
    url: `/admin/vetting/applications/${applicationId}`,
    timestamp: new Date().toISOString()
  });

  try {
    // Navigate to detail page instead of calling onViewItem callback
    navigate(`/admin/vetting/applications/${applicationId}`);
    console.log('VettingApplicationsList: Navigation called successfully');
  } catch (error) {
    console.error('VettingApplicationsList: Navigation failed:', error);
  }
}, [navigate]);
```

**Finding:** ✓ Handler looks correct, includes logging, uses `navigate()` properly

**Table Row Click (Lines 315-323):**
```tsx
onClick={(event) => {
  // Only navigate if clicking on the row itself, not the checkbox
  if (!(event.target as HTMLElement).closest('input[type="checkbox"]')) {
    console.log('Row clicked, initiating navigation:', application.id);
    handleRowClick(application.id);
  } else {
    console.log('Checkbox clicked, skipping navigation');
  }
}}
```

**Finding:** ✓ Logic correctly prevents navigation on checkbox clicks

### 2. AdminVettingPage Component
**File:** `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminVettingPage.tsx`

**Potential Issues:**

#### Issue A: Modal Components Always Rendered
```tsx
{/* Put on Hold Modal - Conditionally rendered */}
{hasSelectedApplications && selectedApplicationsData.length > 0 && (
  <OnHoldModal
    opened={onHoldModalOpen}
    onClose={() => setOnHoldModalOpen(false)}
    ...
  />
)}

{/* Send Reminder Modal - ALWAYS RENDERED */}
<SendReminderModal
  opened={sendReminderModalOpen}
  onClose={() => setSendReminderModalOpen(false)}
  onSuccess={() => { ... }}
/>
```

**Finding:** ⚠️ `SendReminderModal` is always rendered (not conditionally)
- This component uses `useVettingApplications` hook internally (line 42)
- Hook runs even when modal is closed
- Could cause:
  - Extra API calls
  - React re-renders that interrupt navigation
  - Mantine Portal elements that aren't cleaned up
  - Possible invisible overlays blocking clicks

### 3. RootLayout Component
**File:** `/home/chad/repos/witchcityrope/apps/web/src/components/layout/RootLayout.tsx`

```tsx
export const RootLayout: React.FC = () => {
  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      <UtilityBar />
      <Navigation />
      <Box component="main">
        <Outlet />  {/* This should re-render when route changes */}
      </Box>
    </Box>
  );
};
```

**Finding:** ✓ Structure looks correct
- However, if `RootLayout` is not re-rendering, the `<Outlet />` won't update

### 4. Router Configuration
**File:** `/home/chad/repos/witchcityrope/apps/web/src/routes/router.tsx`

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

**Finding:** ✓ Routes are configured correctly
- Both use `adminLoader` for auth
- Paths are correct
- Elements are properly defined

## Root Cause Hypothesis

### Primary Suspect: React State Causing Re-render Prevention

The `SendReminderModal` component is **always mounted** and **always fetching data** via its internal `useVettingApplications` hook. This means:

1. When `AdminVettingPage` renders, it always mounts `SendReminderModal`
2. `SendReminderModal` calls `useVettingApplications` on every render
3. This creates a continuous data-fetching subscription
4. React Query may be causing the parent component to re-render frequently
5. These re-renders may be **preventing React Router from detecting the route change**

### Secondary Suspect: Mantine Modal Portal Issues

Mantine modals use React Portals to render outside the normal component tree. Even when `opened={false}`, the modal component is still mounted, and may:

1. Create portal containers in the DOM
2. Leave behind overlay elements
3. Have z-index issues that block clicks
4. Interfere with event bubbling

### Tertiary Suspect: React Router Outlet Not Re-rendering

If the RootLayout component is not detecting route changes properly, the `<Outlet />` component won't re-render, even though the URL changes.

## Comparison with Working Events Page

**File:** `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventsPage.tsx`

Need to compare the structure of the Events page (which works) with the Vetting page (which doesn't) to identify differences.

## Recommended Debug Steps

1. **Check Console Logs:**
   - Open http://localhost:5173/admin/vetting
   - Open DevTools Console
   - Click a table row
   - Verify that console.log statements execute
   - Check if there are any React errors or warnings

2. **Check DOM for Overlays:**
   - Open DevTools Elements tab
   - Look for elements with class containing "overlay" or "modal"
   - Check z-index values (look for anything > 100)
   - Check for elements with `pointer-events: auto` that might block clicks

3. **Monitor React Router:**
   - Run the debug script from `debug-navigation.js`
   - Verify that `history.pushState` is being called
   - Check if route actually changes in React Router's internal state

4. **Test Modal Removal:**
   - Temporarily comment out the `SendReminderModal` component
   - Test if navigation starts working
   - This will confirm if the modal is the issue

5. **Compare with Events Page:**
   - Navigate to http://localhost:5173/admin/events
   - Click on an event row
   - Verify that navigation works
   - Compare DOM structure and console output

## Proposed Fixes

### Fix 1: Conditionally Render SendReminderModal
```tsx
{/* Only render when needed */}
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

**Pros:**
- Prevents unnecessary API calls
- Removes potential overlay issues
- Cleaner component lifecycle

**Cons:**
- Modal will need to fetch data on every open
- Slightly longer modal open time

### Fix 2: Move Data Fetching Out of Modal
```tsx
// In AdminVettingPage
const pendingApplicationsFilters: ApplicationFilterRequest = {
  statusFilters: ['PendingInterview'],
  // ... other filters
};
const { data: pendingApplications } = useVettingApplications(pendingApplicationsFilters);

// Pass data to modal as props
<SendReminderModal
  opened={sendReminderModalOpen}
  onClose={() => setSendReminderModalOpen(false)}
  pendingApplications={pendingApplications?.items || []}
  onSuccess={() => { ... }}
/>
```

**Pros:**
- Data is pre-loaded
- Modal component is simpler
- Better separation of concerns

**Cons:**
- Extra API call even when modal never opens
- More complex prop passing

### Fix 3: Use React Router's useNavigate with replace option
```tsx
// In handleRowClick
navigate(`/admin/vetting/applications/${applicationId}`, { replace: false });
```

**Pros:**
- Might force React Router to re-render

**Cons:**
- Likely won't fix the root cause

### Fix 4: Force Re-render with Key Prop
```tsx
<Outlet key={location.pathname} />
```

**Pros:**
- Forces React to unmount and remount on route change

**Cons:**
- Loses component state on navigation
- Not a proper fix for the underlying issue

## Next Steps

1. **IMMEDIATE:** Run debug script to identify exact blocker
2. **TEST:** Comment out `SendReminderModal` to verify hypothesis
3. **FIX:** Implement Fix #1 (conditional rendering)
4. **VERIFY:** Test navigation works after fix
5. **DOCUMENT:** Update file registry and commit changes

## Files Involved

- `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminVettingPage.tsx`
- `/home/chad/repos/witchcityrope/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx`
- `/home/chad/repos/witchcityrope/apps/web/src/features/admin/vetting/components/SendReminderModal.tsx`
- `/home/chad/repos/witchcityrope/apps/web/src/components/layout/RootLayout.tsx`
- `/home/chad/repos/witchcityrope/apps/web/src/routes/router.tsx`

## Related Issues

- None identified yet

## References

- React Router v6 Navigation: https://reactrouter.com/en/main/hooks/use-navigate
- Mantine Modal: https://mantine.dev/core/modal/
- React Query: https://tanstack.com/query/latest
