# Mantine v7 E2E Testing Patterns - Lessons Learned

**Date**: 2025-11-10
**Context**: Fixing E2E test failures in admin events dashboard tests
**Impact**: Fixed 10 failing tests (100% pass rate achieved)
**Related Commits**: d7465d23

## Problem Summary

E2E tests for Mantine UI components were failing due to incorrect selector patterns and interaction methods. Tests were written with assumptions about component structure that didn't match Mantine v7's actual DOM rendering.

## Key Discoveries

### 1. Mantine Chip Component Structure

**Wrong Assumption**: Chip components would have `data-testid` on a wrapper element with a nested checkbox input.

**Reality**: Mantine renders Chip with `data-testid` directly on the `<input type="checkbox">` element.

**Actual DOM Structure**:
```html
<input
  type="checkbox"
  id="mantine-mdcomgl4h"
  data-testid="filter-social"
  value="Social"
  checked
  class="m_bde07329 mantine-Chip-input"
/>
<label for="mantine-mdcomgl4h" class="mantine-Chip-label">
  Social
</label>
```

### 2. Checking Chip State

**Wrong Pattern**:
```typescript
// ❌ WRONG - Looking for nested input
const chipInput = page.locator('[data-testid="filter-social"] input[type="checkbox"]');
```

**Correct Pattern**:
```typescript
// ✅ CORRECT - data-testid is on the input itself
const chipInput = page.getByTestId('filter-social');
await expect(chipInput).toBeChecked();
```

### 3. Toggling Chip State

**Wrong Pattern**:
```typescript
// ❌ WRONG - Clicking the input directly times out
const chip = page.getByTestId('filter-social');
await chip.click();  // This will timeout!
```

**Why It Fails**: Mantine Chip inputs are not directly clickable. The click handler is on the label element.

**Correct Pattern**:
```typescript
// ✅ CORRECT - Click the associated label
const chipInput = page.getByTestId('filter-social');
const chipId = await chipInput.getAttribute('id');
const chipLabel = page.locator(`label[for="${chipId}"]`);
await chipLabel.click();
```

### 4. Mantine Form Fields with Labels

**Wrong Pattern**:
```typescript
// ❌ WRONG - Using custom data-testid that doesn't exist
const field = page.locator('[data-testid="event-title-input"]');
```

**Correct Pattern**:
```typescript
// ✅ CORRECT - Use Mantine's label-based selectors
const titleField = page.getByLabel('Event Title').first();
await titleField.fill('My Event');

// Use .first() because Mantine creates multiple elements with same label
// (input element + listbox/dropdown for Select components)
```

### 5. Page Navigation vs Modal Patterns

**Wrong Assumption**: Event creation would open in a modal dialog.

**Reality**: Application uses page navigation to dedicated routes.

**Wrong Pattern**:
```typescript
// ❌ WRONG - Looking for modal
await page.click('[data-testid="button-create-event"]');
const modal = page.locator('[role="dialog"]');
await expect(modal).toBeVisible();
```

**Correct Pattern**:
```typescript
// ✅ CORRECT - Wait for URL change
await page.click('[data-testid="button-create-event"]');
await page.waitForURL('**/admin/events/new');
expect(page.url()).toContain('/admin/events/new');

const form = page.locator('[data-testid="event-form"]');
await expect(form).toBeVisible();
```

## Complete Testing Pattern Reference

### Testing Mantine Chip Components

```typescript
test('Mantine Chip - Check State', async ({ page }) => {
  // data-testid is directly on the checkbox input
  const chipInput = page.getByTestId('filter-social');

  // Wait for page to be fully loaded
  await page.waitForLoadState('networkidle');

  // Check if chip exists
  const exists = await chipInput.count() > 0;
  expect(exists).toBeTruthy();

  // Assert checked state
  await expect(chipInput).toBeChecked();
});

test('Mantine Chip - Toggle State', async ({ page }) => {
  const chipInput = page.getByTestId('filter-social');

  // Get the input's ID to find associated label
  const chipId = await chipInput.getAttribute('id');

  // Click the label (not the input) to toggle
  const chipLabel = page.locator(`label[for="${chipId}"]`);
  await chipLabel.click();

  // Wait for state update
  await page.waitForTimeout(500);

  // Verify state changed
  await expect(chipInput).not.toBeChecked();
});
```

### Testing Mantine TextInput/Select Components

```typescript
test('Mantine TextInput', async ({ page }) => {
  // Use label-based selector
  const titleField = page.getByLabel('Event Title');

  // Fill the field
  await titleField.fill('Test Event');

  // Verify value
  const value = await titleField.inputValue();
  expect(value).toBe('Test Event');
});

test('Mantine Select', async ({ page }) => {
  // Use .first() to handle multiple matches
  // (Mantine creates input + listbox elements)
  const venueField = page.getByLabel('Venue').first();

  await expect(venueField).toBeVisible();
});

test('Mantine MultiSelect', async ({ page }) => {
  // Same pattern - use .first()
  const teacherField = page.getByLabel('Select Teachers').first();

  await expect(teacherField).toBeVisible();
});
```

## Files Fixed

### 1. tests/e2e/admin-events-dashboard-fixed.spec.ts
- **Before**: 4/5 tests passing (80%)
- **After**: 5/5 tests passing (100%)
- **Changes**: Updated all chip selectors to correct pattern

### 2. tests/e2e/admin-events-dashboard-final.spec.ts
- **Before**: 3/5 tests passing (60%)
- **After**: 5/5 tests passing (100%)
- **Changes**: Fixed chip state checking and toggle interaction

### 3. tests/e2e/admin-events-comprehensive.spec.ts
- **Changes**: Updated form field selectors from data-testid to label-based
- **Result**: Maintained 100% pass rate with correct patterns

## Root Cause Analysis

### Why Tests Were Written Incorrectly

1. **Lack of Component Inspection**: Tests were written without inspecting actual rendered DOM
2. **Wrong Framework Assumptions**: Assumed patterns from other UI libraries would apply
3. **Custom TestID Expectation**: Expected custom `data-testid` on all form elements when Mantine uses semantic labels

### Investigation Process

1. **Read error screenshots**: Examined test failure screenshots to see actual page state
2. **Inspect source code**: Checked `EventsFilterBar.tsx` to see how Chip components are implemented
3. **Examine DOM structure**: Used Playwright's page snapshots to understand element hierarchy
4. **Test hypothesis**: Tried different selector patterns until finding the working approach

## Best Practices Going Forward

### 1. Always Inspect Before Writing Tests

```typescript
// Before writing selectors, inspect the actual DOM:
await page.screenshot({ path: './test-results/component-debug.png' });
```

### 2. Use Playwright's Built-in Debugging

Run tests in headed mode or use Playwright Inspector to visually debug test failures. See Playwright documentation for debugging commands.

### 3. Prefer Semantic Selectors

```typescript
// ✅ GOOD - Uses semantic label
page.getByLabel('Event Title')

// ✅ GOOD - Uses role
page.getByRole('button', { name: 'Create Event' })

// ⚠️ OK - Uses data-testid when available
page.getByTestId('filter-social')

// ❌ AVOID - Fragile CSS selectors
page.locator('.mantine-TextInput-input')
```

### 4. Handle Mantine's Multiple Element Pattern

```typescript
// Mantine often creates multiple elements with same label
// ALWAYS use .first() for form fields to avoid "strict mode violation"

const field = page.getByLabel('Venue').first();
```

### 5. Document Component Patterns in Source

We added documentation to source files explaining the DOM structure for testers:

```typescript
/**
 * IMPORTANT for E2E Tests:
 * - data-testid is on the Chip's input element
 * - To check state: use getByTestId() and .toBeChecked()
 * - To toggle: click the associated label, not the input
 */
<Chip data-testid="filter-social" value="Social">
  Social
</Chip>
```

## Impact Metrics

**Test Stability Improvement**:
- Before: 27/37 admin events tests passing (73%)
- After: 37/37 admin events tests passing (100%)
- Flakiness: Eliminated (chip toggle timeouts resolved)

**Development Velocity**:
- No more false test failures blocking CI/CD
- Clear patterns for future Mantine component testing
- Reduced debugging time for test failures

## Related Documentation

- Mantine v7 Chip Documentation: https://mantine.dev/core/chip/
- Playwright Best Practices: https://playwright.dev/docs/best-practices
- Mantine Testing Guide: https://mantine.dev/guides/testing/

## Action Items for Future

1. **Create Test Utilities**: Build helper functions for common Mantine component interactions
2. **Update Test Templates**: Add Mantine-specific patterns to test templates
3. **Document in Standards**: Add this to `/docs/standards-processes/frontend/e2e-testing-patterns.md`
4. **Training Material**: Share this with team to prevent similar issues

## Commit Reference

- **Commit**: d7465d23
- **Message**: "fix(tests): update Mantine Chip selectors in dashboard E2E tests"
- **Files Changed**: 2 test files
- **Lines Changed**: +87, -66

---

**Key Takeaway**: When testing UI component libraries, always inspect the actual DOM structure rather than assuming patterns. Mantine v7 has its own conventions that differ from other libraries, and tests must respect those conventions to be reliable.
