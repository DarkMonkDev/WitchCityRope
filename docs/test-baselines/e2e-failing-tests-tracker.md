# E2E Failing Tests Tracker

## Purpose

Track currently failing E2E tests, their root causes, and fix instructions for the next agent.

## Current Test Run (December 13, 2025)

| Metric | Value |
|--------|-------|
| **Total Tests** | 795 |
| **Passed** | 699 |
| **Failed** | 68 |
| **Skipped** | 28 |
| **Pass Rate** | **87.9%** |

---

## 🔴 EVENTS ADMIN FAILURES - ROOT CAUSES IDENTIFIED

### 1. admin-events-sessions.spec.ts (4 failures) - MANTINE SELECT ISSUE

**Tests Failing**:
- `should add a new session via modal without page refresh`
- `should assign S# IDs sequentially to new sessions`
- `should edit existing session via modal`
- `should validate session form fields`

**Exact Error**: `TimeoutError: waiting for getByTestId('input-session-id').locator('input')`

**Root Cause**: The `selectSessionId()` helper at lines 21-50 uses:
```typescript
const sessionIdInput = page.getByTestId('input-session-id');
const sessionIdTextbox = sessionIdInput.locator('input');  // ❌ WRONG
```

Mantine v7 Select does NOT expose a nested `input` element. The component uses `<Select>` not `<TextInput>`.

**UI Component**: `/apps/web/src/components/events/SessionFormModal.tsx` lines 239-251

**Fix Required**: Update `selectSessionId()` to use correct Mantine Select interaction:
- Option A: Click the Select wrapper directly: `page.getByTestId('input-session-id').click()`
- Option B: Use `getByRole('combobox')` for searchable selects
- Option C: Use browser DevTools to find actual DOM structure

**Additional Issue** for "edit existing session" test:
- Error: `waiting for locator('[role="dialog"]') to be detached` - Modal not closing after save
- This may resolve after the Select fix is applied

---

### 2. admin-events-volunteers.spec.ts (2 failures) - POSITION NOT SAVING

**Tests Failing**:
- `should add volunteer position via inline form`
- `should validate volunteer position form fields`

**Exact Error**: `expect(locator).toHaveCount(expected) - Expected: 1, Received: 0`
- Locator: `[data-testid="grid-volunteer-positions"] [data-testid="position-row"]`

**Root Cause**: After filling and saving the form, no position row appears. Possible causes:
1. Form submission not triggering API call
2. API call failing silently
3. Grid not refreshing after save
4. Form validation preventing submission

**UI Components**:
- Grid: `/apps/web/src/components/events/VolunteerPositionsGrid.tsx`
- Form: `/apps/web/src/components/events/VolunteerPositionInlineForm.tsx`

**Debug Steps**:
1. Check if `data-testid="position-row"` exists on table rows in the component
2. Add console logging to see if form onSubmit is triggered
3. Check Network tab for API call
4. Verify save button click works (may need React Strict Mode `.last()` fix)

---

### 3. events-policies-field-comprehensive.spec.ts (2 failures) - TIPTAP EDITOR

**Tests Failing**:
- `should save policies field and persist after page refresh`
- `should handle empty policies field gracefully`

**Exact Error**: `TimeoutError: locator.click: Timeout 30000ms exceeded`

**Root Cause**: TipTap/ProseMirror contenteditable interaction issue.

**Fix Already Attempted** (still failing):
```typescript
await policiesField.click();
await page.keyboard.press('Control+a');
await page.keyboard.type(TEST_POLICIES);
```

**Remaining Issues to Debug**:
1. Verify the policies field locator finds the correct element
2. Add explicit wait for editor to be ready
3. Check if the element is visible/interactable

**UI Component**: `/apps/web/src/components/events/EventForm.tsx` line 1544-1553 uses MantineTiptapEditor

---

## 🟢 RECENTLY FIXED (Verified Dec 13, 2025)

| Test | Fix Applied |
|------|-------------|
| `admin-events-workflow.spec.ts` - toggle draft/published | Changed radio to button selectors for SegmentedControl |
| `admin-event-copy.spec.ts` - 2 tests | Fixed variable name typos (`event` → `sourceEvent`) |

---

## Key Mantine v7 Testing Patterns

### SegmentedControl
- Renders as **buttons**, NOT radio inputs
- Use `getByRole('button', { name: 'OPTION' })`
- Check state with `getAttribute('data-active')` not `isChecked()`

### Select (searchable)
- Does NOT have simple nested `input` element
- Need to investigate actual DOM structure with DevTools
- May need `getByRole('combobox')` or click wrapper directly

### React Strict Mode
- Components render twice in dev mode
- Use `.last()` on button selectors to get actual interactive element

### TipTap/ProseMirror
- Don't use `.fill()` on contenteditable divs
- Use `click()` → `keyboard.press('Control+a')` → `keyboard.type(text)`

---

## Other Failing Tests by Category (60 remaining)

| Category | Count | Notes |
|----------|-------|-------|
| Session Timing | 8 | Business logic for ticket availability |
| Vetting Workflows | 8 | Requires backend endpoint for test data |
| Check-in | 4 | Check-in staff and attendee workflows |
| Venue | 4 | Venue CRUD operations |
| RSVP/Profile | 4 | Persistence issues |
| Tiptap Editors | 2 | Email content editor rendering |
| Other | 30 | Various - see test-results.json for details |

---

## Test Results Location

- `/test-results/test-results.json` - Full Playwright JSON report
- `/test-results/test-summary.txt` - Human-readable summary
- `/test-results/html-report/` - Interactive HTML report

---

## Next Steps for Next Agent

1. **HIGH PRIORITY**: Fix `selectSessionId()` helper in admin-events-sessions.spec.ts
   - Location: lines 21-50
   - Will unblock 4 tests

2. **MEDIUM**: Debug volunteer position save flow
   - Check if data-testid exists, check API calls

3. **MEDIUM**: Debug TipTap editor interaction
   - Verify locator, add explicit waits
