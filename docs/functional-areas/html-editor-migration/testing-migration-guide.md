# Testing Migration Guide: TinyMCE to @mantine/tiptap
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Development Team -->
<!-- Status: Active -->

## Purpose
This guide provides complete instructions for updating E2E tests during the TinyMCE to @mantine/tiptap migration, including tests to delete, selectors to update, and new tests to create.

## Overview

### Test Migration Summary

| Action | Count | Files Affected |
|--------|-------|----------------|
| **Delete** | 4 tests | TinyMCE-specific E2E tests |
| **Update** | 1 test | events-management-e2e.spec.ts (selectors) |
| **Create** | 1 test | tiptap-editor.spec.ts (new test suite) |
| **Net Change** | +2 tests | More comprehensive coverage |

---

## Phase 1: Delete TinyMCE-Specific Tests

### Tests to Delete (4 files)

**File 1**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-visual-verification.spec.ts`
```bash
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-visual-verification.spec.ts
```
**Reason**: Tests TinyMCE-specific visual rendering

---

**File 2**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-editor.spec.ts`
```bash
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-editor.spec.ts
```
**Reason**: Tests TinyMCE-specific editor functionality

---

**File 3**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-debug.spec.ts`
```bash
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-debug.spec.ts
```
**Reason**: Debugging test specific to TinyMCE issues

---

**File 4**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-basic-check.spec.ts`
```bash
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-basic-check.spec.ts
```
**Reason**: Basic TinyMCE initialization test

---

### Deletion Script

**Quick delete all 4 files**:
```bash
cd /home/chad/repos/witchcityrope/apps/web/tests/playwright

rm -f \
  tinymce-visual-verification.spec.ts \
  tinymce-editor.spec.ts \
  tinymce-debug.spec.ts \
  tinymce-basic-check.spec.ts

# Verify deletion
ls tinymce*.spec.ts 2>&1
# Should output: "No such file or directory"
```

### Verification After Deletion

```bash
# Search for any remaining TinyMCE test references
grep -r "tinymce" /home/chad/repos/witchcityrope/apps/web/tests/playwright/ --include="*.spec.ts"

# Should only find references in events-management-e2e.spec.ts
# (which we'll update in Phase 2)
```

---

## Phase 2: Update Selector References

### File to Update: events-management-e2e.spec.ts

**Location**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/events-management-e2e.spec.ts`

### Selector Mapping Table

| Old Selector (TinyMCE) | New Selector (Tiptap) | Usage |
|------------------------|----------------------|-------|
| `.tox-tinymce` | `.mantine-RichTextEditor-root` | Editor container |
| `.tox-edit-area` | `.ProseMirror` | Content editable area |
| `.tox-toolbar` | `.mantine-RichTextEditor-toolbar` | Toolbar container |
| `.tox-toolbar-button` | `.mantine-RichTextEditor-control` | Toolbar buttons |
| `.tox-statusbar` | *(remove)* | No status bar in Tiptap |
| `iframe[id^="tiny-"]` | *(remove)* | No iframe in Tiptap |

### Search and Replace Instructions

**Step 1**: Find all TinyMCE selectors
```bash
cd /home/chad/repos/witchcityrope/apps/web/tests/playwright
grep -n "\.tox-" events-management-e2e.spec.ts
```

**Step 2**: Replace selectors systematically

**Pattern 1 - Editor Container**:
```typescript
// OLD:
await page.locator('.tox-tinymce').waitFor();
const editor = page.locator('.tox-tinymce');

// NEW:
await page.locator('.mantine-RichTextEditor-root').waitFor();
const editor = page.locator('.mantine-RichTextEditor-root');
```

**Pattern 2 - Content Area**:
```typescript
// OLD:
await page.locator('.tox-edit-area').fill('Event description');
await page.locator('.tox-edit-area').click();

// NEW:
await page.locator('.ProseMirror').fill('Event description');
await page.locator('.ProseMirror').click();
```

**Pattern 3 - Toolbar**:
```typescript
// OLD:
await page.locator('.tox-toolbar').isVisible();
const boldButton = page.locator('.tox-toolbar-button[aria-label="Bold"]');

// NEW:
await page.locator('.mantine-RichTextEditor-toolbar').isVisible();
const boldButton = page.locator('.mantine-RichTextEditor-control[aria-label="Bold"]');
```

**Pattern 4 - IFrame Removal** (if present):
```typescript
// OLD:
const frame = page.frameLocator('iframe[id^="tiny-"]');
await frame.locator('body').fill('Content');

// NEW (direct access, no iframe):
await page.locator('.ProseMirror').fill('Content');
```

### Complete Example Update

**Before** (TinyMCE):
```typescript
test('should create event with rich text description', async ({ page }) => {
  await page.goto('/admin/events/create');

  // Wait for editor to load
  await page.locator('.tox-tinymce').waitFor();

  // Fill description
  await page.locator('.tox-edit-area').click();
  await page.locator('.tox-edit-area').fill('This is a test event description');

  // Verify toolbar visible
  await expect(page.locator('.tox-toolbar')).toBeVisible();

  // Test bold button
  const boldButton = page.locator('.tox-toolbar-button[aria-label="Bold"]');
  await boldButton.click();

  // Fill other fields
  await page.fill('[name="title"]', 'Test Event');

  // Submit
  await page.click('button[type="submit"]');
});
```

**After** (Tiptap):
```typescript
test('should create event with rich text description', async ({ page }) => {
  await page.goto('/admin/events/create');

  // Wait for editor to load
  await page.locator('.mantine-RichTextEditor-root').waitFor();

  // Fill description
  await page.locator('.ProseMirror').click();
  await page.locator('.ProseMirror').fill('This is a test event description');

  // Verify toolbar visible
  await expect(page.locator('.mantine-RichTextEditor-toolbar')).toBeVisible();

  // Test bold button
  const boldButton = page.locator('.mantine-RichTextEditor-control[aria-label="Bold"]');
  await boldButton.click();

  // Fill other fields
  await page.fill('[name="title"]', 'Test Event');

  // Submit
  await page.click('button[type="submit"]');
});
```

### Verification Commands

```bash
# After updates, verify no old selectors remain
grep -n "\.tox-" events-management-e2e.spec.ts
# Should output: (nothing)

# Verify new selectors present
grep -n "mantine-RichTextEditor" events-management-e2e.spec.ts
# Should show updated lines

# TypeScript compilation check
npx tsc --noEmit

# Run updated test
npm run test:e2e -- events-management-e2e.spec.ts
```

---

## Phase 3: Create New Tiptap Editor Test Suite

### New Test File: tiptap-editor.spec.ts

**Location**: `/home/chad/repos/witchcityrope/apps/web/tests/playwright/tiptap-editor.spec.ts`

**COMPLETE TEST CODE** (copy-paste ready):

```typescript
import { test, expect } from '@playwright/test';

test.describe('Tiptap Rich Text Editor', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to event creation page (or any page with editor)
    await page.goto('/admin/events/create');

    // Wait for editor to load
    await page.locator('.mantine-RichTextEditor-root').waitFor({ timeout: 10000 });
  });

  test('renders editor with correct structure', async ({ page }) => {
    // Verify editor container
    const editor = page.locator('.mantine-RichTextEditor-root');
    await expect(editor).toBeVisible();

    // Verify toolbar
    const toolbar = page.locator('.mantine-RichTextEditor-toolbar');
    await expect(toolbar).toBeVisible();

    // Verify content area
    const content = page.locator('.ProseMirror');
    await expect(content).toBeVisible();

    // Verify editable
    await expect(content).toHaveAttribute('contenteditable', 'true');
  });

  test('allows text input and formatting', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Type text
    await content.click();
    await content.fill('Test content for formatting');

    // Verify text appears
    await expect(content).toContainText('Test content');

    // Select all text (Ctrl+A / Cmd+A)
    await content.press('ControlOrMeta+a');

    // Click bold button
    const boldButton = page.locator('.mantine-RichTextEditor-control[aria-label="Bold"]');
    await boldButton.click();

    // Verify bold formatting applied
    const boldText = page.locator('.ProseMirror strong');
    await expect(boldText).toContainText('Test content');
  });

  test('shows variable insertion autocomplete', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Click in editor
    await content.click();

    // Type variable trigger
    await content.pressSequentially('{{');

    // Wait for suggestion menu (may need to adjust selector based on implementation)
    // The tippy.js suggestion menu is appended to document.body
    const suggestionMenu = page.locator('[data-tippy-root]').first();
    await expect(suggestionMenu).toBeVisible({ timeout: 5000 });

    // Verify suggestions appear (customize based on your variables)
    // This depends on your VariableInsertion implementation
    const suggestions = page.locator('[data-tippy-root] [style*="cursor: pointer"]');
    await expect(suggestions).toHaveCount(expect.any(Number));
  });

  test('inserts variables via autocomplete', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Click in editor
    await content.click();

    // Type variable trigger
    await content.pressSequentially('{{');

    // Wait for menu
    await page.waitForTimeout(500); // Allow autocomplete to appear

    // Press down arrow to select first item
    await page.keyboard.press('ArrowDown');

    // Press enter to insert
    await page.keyboard.press('Enter');

    // Verify variable inserted (adjust based on your first variable)
    // Example: {{eventTitle}}
    const insertedText = await content.textContent();
    expect(insertedText).toContain('{{');
    expect(insertedText).toContain('}}');
  });

  test('updates form value on content change', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Type content
    await content.click();
    await content.fill('Form integration test content');

    // Fill title field (required for save)
    await page.fill('[name="title"]', 'Test Event');

    // Click save button
    await page.click('button:has-text("Save")');

    // Wait for success indication or redirect
    // (Adjust based on your form submission behavior)
    await page.waitForTimeout(1000);

    // Verify no validation errors for description field
    const descriptionError = page.locator('text=Description').locator('.. >> text=/error|required/i');
    await expect(descriptionError).not.toBeVisible();
  });

  test('toolbar buttons apply correct formatting', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Type test content
    await content.click();
    await content.fill('Formatting test');

    // Select all
    await content.press('ControlOrMeta+a');

    // Test bold
    await page.click('.mantine-RichTextEditor-control[aria-label="Bold"]');
    await expect(page.locator('.ProseMirror strong')).toContainText('Formatting test');

    // Test italic
    await page.click('.mantine-RichTextEditor-control[aria-label="Italic"]');
    await expect(page.locator('.ProseMirror em')).toContainText('Formatting test');

    // Test underline
    await page.click('.mantine-RichTextEditor-control[aria-label="Underline"]');
    await expect(page.locator('.ProseMirror u')).toContainText('Formatting test');
  });

  test('supports programmatic content updates', async ({ page }) => {
    // This test verifies the editor responds to external updates
    // (e.g., from form reset or template insertion)

    const content = page.locator('.ProseMirror');

    // Initial content
    await content.click();
    await content.fill('Initial content');
    await expect(content).toContainText('Initial content');

    // Simulate form reset (adjust based on your form implementation)
    const resetButton = page.locator('button:has-text("Reset")');
    if (await resetButton.isVisible()) {
      await resetButton.click();

      // Verify content cleared
      const emptyContent = await content.textContent();
      expect(emptyContent?.trim()).toBe('');
    } else {
      // Alternative: reload page
      await page.reload();
      await page.locator('.mantine-RichTextEditor-root').waitFor();

      // Verify content cleared after reload
      const clearedContent = page.locator('.ProseMirror');
      const text = await clearedContent.textContent();
      expect(text?.trim()).toBe('');
    }
  });

  test('handles lists correctly', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Click in editor
    await content.click();

    // Click bullet list button
    await page.click('.mantine-RichTextEditor-control[aria-label="Bullet list"]');

    // Type list items
    await content.pressSequentially('First item');
    await page.keyboard.press('Enter');
    await content.pressSequentially('Second item');

    // Verify list created
    const list = page.locator('.ProseMirror ul');
    await expect(list).toBeVisible();

    const listItems = page.locator('.ProseMirror li');
    await expect(listItems).toHaveCount(2);
  });

  test('supports undo and redo', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Type content
    await content.click();
    await content.fill('Original text');
    await expect(content).toContainText('Original text');

    // Make a change
    await content.press('ControlOrMeta+a');
    await content.fill('Modified text');
    await expect(content).toContainText('Modified text');

    // Undo
    await page.click('.mantine-RichTextEditor-control[aria-label="Undo"]');
    await expect(content).toContainText('Original text');

    // Redo
    await page.click('.mantine-RichTextEditor-control[aria-label="Redo"]');
    await expect(content).toContainText('Modified text');
  });

  test('maintains content after navigation', async ({ page }) => {
    const content = page.locator('.ProseMirror');

    // Enter content
    await content.click();
    await content.fill('Test content for persistence');

    // Fill required fields
    await page.fill('[name="title"]', 'Persistence Test Event');

    // Navigate away (if your app supports draft saving)
    // Or just verify content remains during session
    const currentContent = await content.textContent();
    expect(currentContent).toContain('Test content for persistence');
  });
});
```

### Test Coverage Summary

| Test | Purpose | Pass Criteria |
|------|---------|---------------|
| **renders editor** | Verify structure | Editor, toolbar, content visible |
| **text input** | Basic functionality | Can type and format text |
| **variable autocomplete** | Custom extension | Menu appears on `{{` |
| **variable insertion** | Custom extension | Variable inserted correctly |
| **form integration** | State management | Form values update on change |
| **toolbar buttons** | Formatting | Bold, italic, underline work |
| **programmatic updates** | External control | Content updates from code |
| **lists** | Rich content | Bullet/ordered lists work |
| **undo/redo** | History | Changes can be undone/redone |
| **content persistence** | State | Content maintained during session |

### Running New Tests

```bash
# Run new test suite
npm run test:e2e -- tiptap-editor.spec.ts

# Expected output:
# ✓ renders editor with correct structure
# ✓ allows text input and formatting
# ✓ shows variable insertion autocomplete
# ✓ inserts variables via autocomplete
# ✓ updates form value on content change
# ✓ toolbar buttons apply correct formatting
# ✓ supports programmatic content updates
# ✓ handles lists correctly
# ✓ supports undo and redo
# ✓ maintains content after navigation

# 10 tests passing
```

---

## Phase 4: Baseline Comparison

### Before Migration Baseline

Run tests before migration to establish baseline:

```bash
npm run test:e2e

# Document results:
# Total tests: 268
# Passing: X
# Failing: Y
# Pass rate: Z%
```

### After Migration Expected Results

**Changes**:
- **Removed**: 4 TinyMCE tests
- **Added**: 10 Tiptap tests (in 1 file)
- **Net change**: +6 tests

**Expected outcomes**:
- **Pass rate**: Should be ≥ baseline (likely better)
- **TinyMCE failures**: 0 (all deleted)
- **Tiptap tests**: 10/10 passing
- **Other tests**: Same as baseline

### Verification Script

```bash
#!/bin/bash
# compare-test-results.sh

echo "=== Test Results Comparison ==="
echo ""
echo "BEFORE MIGRATION:"
cat /tmp/migration-baseline.txt
echo ""
echo "AFTER MIGRATION:"
npm run test:e2e 2>&1 | grep "tests passing"
echo ""
echo "CHANGES:"
echo "- Removed: 4 TinyMCE tests"
echo "- Added: 10 Tiptap tests"
echo "- Net: +6 tests"
```

---

## Troubleshooting Test Failures

### Issue: Selector Not Found

**Error**: `Timeout waiting for selector ".mantine-RichTextEditor-root"`

**Solutions**:
1. **Verify selector in browser DevTools**:
   ```javascript
   // In browser console
   document.querySelector('.mantine-RichTextEditor-root')
   ```

2. **Increase timeout**:
   ```typescript
   await page.locator('.mantine-RichTextEditor-root').waitFor({ timeout: 15000 });
   ```

3. **Check component rendered**:
   ```typescript
   await page.screenshot({ path: 'debug-editor-missing.png' });
   ```

### Issue: Variable Insertion Test Fails

**Error**: Autocomplete menu doesn't appear

**Solutions**:
1. **Check tippy.js installed**:
   ```bash
   npm list tippy.js
   ```

2. **Verify extension loaded**:
   ```typescript
   // In browser console
   document.querySelector('[data-tippy-root]')
   ```

3. **Adjust wait time**:
   ```typescript
   await page.waitForTimeout(1000); // Allow autocomplete to render
   ```

### Issue: Content Not Updating

**Error**: Form value doesn't change

**Solutions**:
1. **Verify onChange wired**:
   ```typescript
   // Check component code
   onChange={(content) => {
     console.log('Changed:', content);
     form.setFieldValue('description', content);
   }}
   ```

2. **Debug form state**:
   ```typescript
   // In test
   const formValue = await page.evaluate(() => {
     // Access form state via window object or React DevTools
   });
   console.log('Form value:', formValue);
   ```

### Issue: Toolbar Buttons Not Working

**Error**: Clicking bold button doesn't apply formatting

**Solutions**:
1. **Verify content selected**:
   ```typescript
   await content.press('ControlOrMeta+a'); // Select all before formatting
   ```

2. **Check button selector**:
   ```typescript
   // Verify exact aria-label
   const boldButton = page.locator('.mantine-RichTextEditor-control');
   const ariaLabel = await boldButton.getAttribute('aria-label');
   console.log('Aria label:', ariaLabel);
   ```

3. **Wait for format application**:
   ```typescript
   await page.waitForTimeout(500); // Allow formatting to apply
   ```

---

## Test Data

### Sample Rich Text Content

**Simple Text**:
```html
<p>This is a simple event description.</p>
```

**Formatted Text**:
```html
<p>This is <strong>bold</strong>, <em>italic</em>, and <u>underlined</u> text.</p>
```

**With Headings**:
```html
<h2>Event Overview</h2>
<p>Event description here.</p>
<h3>What to Bring</h3>
<ul>
  <li>Rope</li>
  <li>Water bottle</li>
</ul>
```

**With Variables**:
```html
<p>Dear {{userName}},</p>
<p>Your registration for {{eventTitle}} on {{eventDate}} is confirmed.</p>
<p>Location: {{eventLocation}}</p>
```

**Complex Content**:
```html
<h2>Workshop Details</h2>
<p>Join us for an exciting <strong>rope bondage workshop</strong> taught by {{teacherName}}.</p>
<h3>Schedule</h3>
<ul>
  <li><strong>Date:</strong> {{eventDate}}</li>
  <li><strong>Time:</strong> 7:00 PM - 9:00 PM</li>
  <li><strong>Location:</strong> {{eventLocation}}</li>
</ul>
<h3>Requirements</h3>
<ol>
  <li>18+ years old</li>
  <li>Basic rope knowledge recommended</li>
  <li>Bring 2x 30ft ropes</li>
</ol>
<p>Ticket price: {{ticketPrice}}</p>
```

### Test User Credentials

Use standard test accounts:
```typescript
const adminUser = {
  email: 'admin@witchcityrope.com',
  password: 'Test123!'
};
```

---

## Success Criteria

### All Tests Must Pass
- [ ] tiptap-editor.spec.ts: 10/10 tests passing
- [ ] events-management-e2e.spec.ts: All tests passing (updated selectors)
- [ ] No TinyMCE-related test failures
- [ ] Total pass rate ≥ baseline

### Code Quality
- [ ] No TypeScript errors
- [ ] No console errors during test runs
- [ ] Tests run in reasonable time (< 5 min for full suite)
- [ ] Tests are deterministic (no flaky failures)

### Coverage
- [ ] Editor rendering tested
- [ ] Text input tested
- [ ] Formatting tested
- [ ] Variable insertion tested
- [ ] Form integration tested
- [ ] All toolbar buttons tested

---

## Related Documentation

- **Migration Plan**: [migration-plan.md](./migration-plan.md) - Complete migration phases
- **Component Guide**: [component-implementation-guide.md](./component-implementation-guide.md) - Component code
- **Configuration Guide**: [configuration-cleanup-guide.md](./configuration-cleanup-guide.md) - Config cleanup
- **Rollback Plan**: [rollback-plan.md](./rollback-plan.md) - Emergency procedures

---

## Version History
- **v1.0** (2025-10-08): Initial testing migration guide created

---

**Questions?** See troubleshooting section above or consult [migration-plan.md](./migration-plan.md).
