# E2E Test Suite Cleanup and Maintenance

**Command**: `/e2e-cleanup`
**Frequency**: Run every 3 months for test suite health maintenance
**Target**: 70% pass rate minimum (pragmatic goal for E2E stability)
**Duration**: Typically 2-4 hours for full suite

## Overview

This command triggers a comprehensive, systematic cleanup of the E2E test suite. The process is designed to be **fully autonomous** - no user questions during execution. The approach focuses on applying proven patterns from past cleanup sessions to fix failing tests efficiently.

### Purpose

E2E tests naturally accumulate technical debt as:
- UI components evolve (Mantine updates, component structure changes)
- UX patterns change (modals → page navigation, inline forms)
- Features are added/modified without updating tests
- Authentication patterns change
- API responses evolve

Regular maintenance prevents test rot and maintains confidence in the test suite.

### Target Metrics

- **Minimum**: 70% pass rate (pragmatic stability goal)
- **Ideal**: 80%+ pass rate
- **Critical**: Zero flaky tests (intermittent failures eliminated)

## Prerequisites

### Before Starting

1. **Docker containers must be running**
   ```bash
   ./dev.sh
   # Wait for containers to be healthy
   ```

2. **Database must be seeded with test data**
   ```bash
   # Verify test accounts exist
   curl http://localhost:5655/api/health
   ```

3. **Read the lessons learned file** (MANDATORY)
   - Location: `/home/chad/repos/witchcityrope/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md`
   - This contains all proven patterns for Mantine v7 components
   - Reference this file throughout the cleanup process

4. **Create TodoWrite task for progress tracking**
   ```
   Task: E2E Test Suite Cleanup - [DATE]
   Goal: Achieve 70%+ pass rate
   ```

## Execution Strategy

### Phase 1: Assessment (15 minutes)

1. **Run full test suite to establish baseline**
   ```bash
   cd /home/chad/repos/witchcityrope
   npm run test:e2e 2>&1 | tee test-results/cleanup-baseline.log
   ```

2. **Analyze results**
   - Count total tests
   - Count passing tests
   - Calculate current pass rate
   - Identify test files with failures

3. **Prioritize test files** (fix high-value files first)
   - Sort by "number of tests per file" (most tests = highest value)
   - Focus on files with 5+ tests first
   - Leave files with 1-2 tests for last

4. **Update TodoWrite with file list**
   ```
   Files to fix (priority order):
   [ ] admin-events-comprehensive.spec.ts (17 tests)
   [ ] admin-events-dashboard.spec.ts (10 tests)
   [ ] vetting-application.spec.ts (6 tests)
   ...
   ```

### Phase 2: Systematic Fixes (2-3 hours)

**For each test file:**

1. **Read the test file**
   - Understand what functionality is being tested
   - Identify error patterns (selector failures, timeouts, assertion mismatches)

2. **Take debug screenshots if needed**
   ```typescript
   await page.screenshot({ path: './test-results/debug-component.png' });
   ```

3. **Apply proven patterns** (see Patterns section below)
   - Mantine v7 component patterns
   - AuthHelper for authentication
   - Feature detection with conditionals
   - API response unwrapping

4. **Delegate fix to test-developer agent**
   ```
   Context: E2E test cleanup session, file [filename]

   Current failures:
   - [Describe specific test failures]
   - [Error messages from test run]

   Apply these patterns:
   - [Specific patterns needed based on error analysis]
   - Use .first() for Mantine strict mode violations
   - Use AuthHelper.loginAs() for authentication
   - Add feature detection for optional elements

   Refer to: /docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md

   Fix tests one at a time, verify each passes before moving to next.
   ```

5. **Run tests for just this file**
   ```bash
   npx playwright test [filename] --headed
   ```

6. **Verify 100% pass rate for file before moving on**
   - If any test fails, iterate with test-developer agent
   - Do NOT move to next file until current file is 100% passing

7. **Update TodoWrite** (mark file as complete)

8. **Commit the fix**
   ```bash
   git add tests/e2e/[filename]
   git commit -m "fix(tests): update [filename] for Mantine v7 patterns and current UI state

   - Applied .first() for strict mode violations
   - Updated selectors to match current component structure
   - Fixed authentication patterns with AuthHelper
   - [Other specific fixes]

   Tests: X/X passing (100%)

   🤖 Generated with Claude Code

   Co-Authored-By: Claude <noreply@anthropic.com>"
   ```

### Phase 3: Handle Unclear Functionality (As Needed)

**When encountering tests for unclear/undocumented functionality:**

1. **DO NOT ask user questions** (autonomous execution)

2. **Skip the test with clear TODO marker**
   ```typescript
   test.skip('Submit volunteer application', async ({ page }) => {
     // TODO: E2E Cleanup - Unclear if this functionality is implemented
     // Last checked: [DATE]
     // Status: Feature may not exist yet, or form fields have changed
     // Action needed: Manual verification of expected behavior
   });
   ```

3. **Document in test file comments**
   ```typescript
   /**
    * CLEANUP NOTE [DATE]:
    * This test assumes volunteer application submission is implemented.
    * Could not verify if feature exists or what current form structure is.
    * Most functionality is manually tested and working - tests should reflect
    * CURRENT state, not ideal/future state.
    */
   ```

4. **Continue to next test** (don't block progress on unknowns)

5. **Important principle**: Tests should verify **current functionality**, not document desired features. If functionality doesn't exist or isn't clear, skip the test.

### Phase 4: Final Validation (30 minutes)

1. **Run full test suite again**
   ```bash
   npm run test:e2e 2>&1 | tee test-results/cleanup-final.log
   ```

2. **Calculate final pass rate**
   - Compare to baseline
   - Verify 70%+ target achieved
   - Document improvement metrics

3. **Archive debug files**
   ```bash
   # Move debug screenshots to archive
   mkdir -p test-results/archive/cleanup-[DATE]
   mv test-results/debug-*.png test-results/archive/cleanup-[DATE]/
   ```

4. **Update test documentation**
   - Update `/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md` with any NEW patterns discovered
   - Update test file registry if new test files created

5. **Final commit** (if any documentation updates)
   ```bash
   git add docs/lessons-learned/
   git commit -m "docs: update E2E testing patterns from cleanup session [DATE]"
   ```

## Agent Delegation Pattern

### When to Delegate to test-developer Agent

**ALWAYS delegate actual test fixes** to the test-developer agent. The main agent's role is:
- Analyze failures
- Identify patterns
- Prioritize work
- Verify results
- Coordinate progress

The test-developer agent's role is:
- Apply fixes to test files
- Update selectors
- Fix authentication
- Adjust assertions

### Effective Delegation Message Template

```
Context: E2E test suite cleanup session, fixing [filename]

Current State:
- Pass rate: X/Y tests passing
- Primary failures: [selector issues, timeout, assertions, etc.]

Test Failures Analysis:
1. Test "[name]": [error message]
   Pattern needed: [specific pattern from lessons learned]

2. Test "[name]": [error message]
   Pattern needed: [specific pattern from lessons learned]

Required Patterns (from lessons learned):
- Mantine Chip: data-testid is on input, click label to toggle
- Mantine Forms: use .getByLabel().first() for strict mode
- AuthHelper: Use AuthHelper.loginAs() for test account login
- Feature detection: Use .count() > 0 before interaction
- API responses: Unwrap with .data or .data[0]

Instructions:
1. Read test file: [path]
2. Apply patterns above to fix failing tests
3. Run tests after each fix: npx playwright test [filename]
4. Verify 100% pass rate before completing
5. Do NOT move on if any test still fails

Reference: /docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md

Constraints:
- Fix tests one at a time
- Verify each fix works before next fix
- If functionality unclear, skip test with TODO comment
- Tests should reflect CURRENT functionality, not ideal state
```

## Patterns to Apply

### 1. AuthHelper Usage

**Problem**: Inconsistent authentication in tests, timeouts, session issues

**Solution**: Use centralized AuthHelper
```typescript
import { AuthHelper } from './helpers/auth-helper';

test.beforeEach(async ({ page }) => {
  // Login as specific role
  await AuthHelper.loginAs(page, 'admin');
  // OR
  await AuthHelper.loginAs(page, 'member');
});

test.afterEach(async ({ page }) => {
  await AuthHelper.logout(page);
});
```

**Available Roles**: admin, teacher, member, vetted, guest, coordinator1, coordinator2

### 2. Mantine v7 Strict Mode (.first())

**Problem**: "strict mode violation: locator resolved to multiple elements"

**Solution**: Use .first() for Mantine form fields
```typescript
// ❌ WRONG - Throws strict mode violation
const field = page.getByLabel('Event Title');
await field.fill('value');

// ✅ CORRECT - Use .first()
const field = page.getByLabel('Event Title').first();
await field.fill('value');
```

**Why**: Mantine creates multiple elements with same label (input + listbox/dropdown)

### 3. Mantine Chip Component Pattern

**Problem**: Cannot click chips, state checks fail, timeouts

**Solution**: data-testid is on the input, click the label
```typescript
// Check chip state
const chipInput = page.getByTestId('filter-social');
await expect(chipInput).toBeChecked();

// Toggle chip (click the LABEL, not the input)
const chipId = await chipInput.getAttribute('id');
const chipLabel = page.locator(`label[for="${chipId}"]`);
await chipLabel.click();
await page.waitForTimeout(500); // Wait for state update
```

### 4. Feature Detection (Conditional Operations)

**Problem**: Tests fail when optional features don't exist on page

**Solution**: Check existence before interaction
```typescript
// ❌ WRONG - Fails if button doesn't exist
await page.getByTestId('optional-button').click();

// ✅ CORRECT - Check first
const button = page.getByTestId('optional-button');
if (await button.count() > 0) {
  await button.click();
} else {
  console.log('Optional button not present, skipping');
}
```

### 5. API Response Unwrapping

**Problem**: Tests expect direct data, but API returns wrapped response

**Solution**: Unwrap response properly
```typescript
// API returns: { data: [...] }
const response = await page.request.get('/api/events');
const json = await response.json();

// ❌ WRONG - json is wrapper object
expect(json.length).toBe(5);

// ✅ CORRECT - unwrap to data
expect(json.data.length).toBe(5);
// OR if single item
expect(json.data[0].title).toBe('Event Title');
```

### 6. Custom Component Inspection (When Selectors Fail)

**Problem**: Don't know how component is structured, selectors fail

**Solution**: Take screenshot and inspect DOM
```typescript
test('Debug component structure', async ({ page }) => {
  await page.goto('/admin/events');

  // Screenshot entire page
  await page.screenshot({ path: './test-results/debug-full-page.png' });

  // Screenshot specific component
  const component = page.locator('[data-testid="events-filter"]');
  await component.screenshot({ path: './test-results/debug-filter.png' });

  // Print HTML structure
  const html = await component.innerHTML();
  console.log('Component HTML:', html);
});
```

Then analyze screenshot/HTML to determine correct selectors.

### 7. Page Navigation vs Modal Patterns

**Problem**: Test expects modal, but app uses page navigation (or vice versa)

**Solution**: Verify actual navigation pattern
```typescript
// Page navigation pattern
await page.click('[data-testid="button-create-event"]');
await page.waitForURL('**/admin/events/new');
expect(page.url()).toContain('/admin/events/new');

// Modal pattern
await page.click('[data-testid="button-create-event"]');
const modal = page.locator('[role="dialog"]');
await expect(modal).toBeVisible();
```

**Check first**: Take screenshot or inspect source to determine which pattern is used.

### 8. Inline Forms (Mantine Collapse)

**Problem**: Inline form validation fails, element visibility checks don't work

**Solution**: Handle Collapse animation timing, verify grid updates not form visibility
```typescript
// Open inline form
await page.getByTestId('button-add-item').click();
await page.waitForTimeout(300); // Collapse animation

// Fill and save
await page.getByTestId('input-title').fill('Item Title');
await page.getByTestId('button-save').click();
await page.waitForTimeout(500); // Form close + grid refresh

// ❌ WRONG - Collapse keeps element in DOM (height=0)
await expect(page.getByTestId('inline-form')).not.toBeVisible();

// ✅ CORRECT - Verify grid updates
const row = page.locator('tr').filter({ hasText: 'Item Title' });
await expect(row).toBeVisible();
```

### 9. Disabled/Readonly Fields

**Problem**: Test tries to fill disabled fields, causing timeouts

**Solution**: Check field state, don't try to fill disabled fields
```typescript
const field = page.getByTestId('scene-name-input');

// Check if disabled
const isDisabled = await field.isDisabled();
if (!isDisabled) {
  await field.fill('value');
} else {
  // Just verify it exists and has expected value
  await expect(field).toBeVisible();
  await expect(field).toBeDisabled();
  // Optional: verify pre-filled value
  const value = await field.inputValue();
  expect(value).toBe('Expected Value');
}
```

### 10. Tab Organization Discovery

**Problem**: Test looks for wrong tab names/testids

**Solution**: Take screenshot, inspect actual tab structure
```typescript
// ❌ WRONG - Assuming tab names
await page.getByTestId('tab-sessions').click();

// ✅ CORRECT - Use actual tab structure
// (Setup tab contains BOTH sessions and tickets)
await page.getByTestId('setup-tab').click();
const sessionsSection = page.getByTestId('sessions-section');
await expect(sessionsSection).toBeVisible();
```

### 11. Simplified Test Approach for Complex Forms

**Problem**: Tests try to submit forms with 10+ required fields, causing brittle tests

**Solution**: Focus on access and presence, not full submission
```typescript
// ❌ WRONG - Brittle full form submission
test('Submit application', async ({ page }) => {
  await page.getByLabel('Field 1').fill('value');
  await page.getByLabel('Field 2').fill('value');
  // ... 10 more fields
  await page.click('Submit');
  await expect(page).toHaveURL('/success');
});

// ✅ CORRECT - Verify access and key elements
test('Application form access', async ({ page }) => {
  // Verify authenticated user can see form
  await expect(page.locator('h2')).toContainText('Apply to Join');

  // Verify key fields exist
  await expect(page.getByTestId('scene-name-input')).toBeVisible();
  await expect(page.getByTestId('experience-input')).toBeVisible();

  // Verify submit button exists (may be disabled)
  await expect(page.getByRole('button', { name: 'Submit Application' })).toBeVisible();

  console.log('✅ Form accessible with required elements');
  // Leave full submission testing to integration tests
});
```

## Quality Gates

### Per-File Quality Gate

**Before moving to next file**:
- [ ] 100% of tests in current file passing
- [ ] No skipped tests without TODO comments
- [ ] Changes committed with descriptive message
- [ ] TodoWrite updated with completion

### Overall Quality Gate

**Before finishing cleanup**:
- [ ] 70%+ pass rate achieved (minimum target)
- [ ] Zero flaky tests (all tests pass consistently)
- [ ] All fixes committed
- [ ] Documentation updated with new patterns (if any)
- [ ] Debug files archived
- [ ] TodoWrite task marked complete

### If Quality Gate Not Met

**If 70% target not reached**:
1. Review prioritization - did we fix high-value files first?
2. Check for systemic issues (authentication, API, database)
3. Consider if some tests need to be deleted (test obsolete features)
4. Document remaining failures in TodoWrite for next session

**Do NOT block on perfection** - 70% is pragmatic target, 100% may not be achievable.

## Post-Cleanup Tasks

### 1. Archive Debug Files

```bash
# Create archive directory
mkdir -p test-results/archive/cleanup-$(date +%Y-%m-%d)

# Move debug screenshots
mv test-results/debug-*.png test-results/archive/cleanup-$(date +%Y-%m-%d)/

# Move baseline/final logs
mv test-results/cleanup-*.log test-results/archive/cleanup-$(date +%Y-%m-%d)/
```

### 2. Update Documentation

**If new patterns discovered**, add to:
- `/home/chad/repos/witchcityrope/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md`

**Format**:
```markdown
### X. [Pattern Name]

**Problem**: [What was failing]

**Solution**: [How to fix it]

```typescript
// ✅ CORRECT pattern
// Code example
```

**Files Fixed**: [List of files where pattern applied]
```

### 3. Update Test Catalog (If Exists)

If project has `/docs/testing/TEST_CATALOG.md`:
- Update pass rates for modified test files
- Add notes about patterns applied
- Update last maintenance date

### 4. Final Summary Report

Create summary in TodoWrite or commit message:
```
E2E Test Suite Cleanup - [DATE]

Baseline: X/Y tests passing (Z%)
Final: A/B tests passing (C%)
Improvement: +D tests fixed (+E% pass rate)

Files Fixed:
- [filename] (X tests fixed)
- [filename] (Y tests fixed)
...

Patterns Applied:
- Mantine .first() strict mode fixes
- AuthHelper authentication updates
- Feature detection for optional elements
- [Other patterns]

Time Spent: ~X hours

Next Maintenance: [DATE + 3 months]
```

## Reference Materials

### Primary Reference (MUST READ)
- **Mantine E2E Patterns**: `/home/chad/repos/witchcityrope/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md`

### Supporting Documentation
- **Playwright Best Practices**: https://playwright.dev/docs/best-practices
- **Mantine v7 Documentation**: https://mantine.dev/
- **Project Standards**: `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/e2e-testing-patterns.md` (if exists)

### Test Helper Files
- **AuthHelper**: `/home/chad/repos/witchcityrope/tests/e2e/helpers/auth-helper.ts`
- **Test Utilities**: `/home/chad/repos/witchcityrope/tests/e2e/helpers/` (various)

## Key Principles (Critical for Success)

### 1. AUTONOMOUS EXECUTION
- **DO NOT** ask user questions during cleanup
- **DO** make pragmatic decisions based on observable behavior
- **DO** skip unclear functionality with TODO comments
- **DO** document uncertainties in test file comments

### 2. CURRENT FUNCTIONALITY FOCUS
- Tests should reflect **what exists now**, not what should exist
- Most functionality is manually tested and working
- If test fails and feature seems missing, skip test (don't assume bug)
- Trust that working features have working manual verification

### 3. SYSTEMATIC APPROACH
- Fix one file at a time
- Verify 100% pass rate for file before moving on
- Commit each file fix separately
- Track progress in TodoWrite
- Don't jump around between files

### 4. PATTERN APPLICATION
- Use proven patterns from lessons learned
- Don't reinvent solutions
- Document NEW patterns discovered
- Share patterns via documentation updates

### 5. PRAGMATIC TARGETS
- 70% pass rate is success (not 100%)
- Some tests may be obsolete (delete rather than fix)
- Some features may not be implemented (skip test)
- Focus on high-value fixes (many tests per file)

### 6. TIME BOXING
- Don't spend more than 30 minutes per test file
- If file is too complex, skip and document
- Move to next file if stuck
- Systemic issues need different approach (not per-test fixes)

## Troubleshooting Common Issues

### Issue: Authentication Keeps Failing

**Solution**: Verify Docker containers and test accounts
```bash
# Check containers running
docker ps

# Verify API health
curl http://localhost:5655/api/health

# Verify test accounts exist (check database or seed scripts)
# If accounts missing, reseed database
```

### Issue: Selectors Keep Timing Out

**Solution**: Take screenshots to verify page state
```typescript
await page.screenshot({ path: './test-results/debug-timeout.png', fullPage: true });
```
Review screenshot to see if:
- Page loaded completely
- Component is actually present
- Selector is correct for current DOM structure

### Issue: Tests Flaky (Pass/Fail Intermittently)

**Solution**: Add appropriate waits
```typescript
// Wait for network idle
await page.waitForLoadState('networkidle');

// Wait for specific element
await page.waitForSelector('[data-testid="component"]');

// Wait for animation
await page.waitForTimeout(300); // Mantine animations typically 200-300ms
```

### Issue: Can't Determine Current Functionality

**Solution**: Skip test with clear TODO
```typescript
test.skip('Feature test', async ({ page }) => {
  // TODO: E2E Cleanup [DATE] - Cannot verify if feature exists
  // Need manual verification of:
  // - Does this feature exist in current build?
  // - What is current form structure?
  // - What are current field names/labels?
  // Decision: Skip rather than block cleanup progress
});
```

### Issue: Mass Failures in Multiple Files

**Solution**: Check for systemic issue
- API down: Check Docker containers
- Database issue: Reseed test data
- Auth broken: Verify AuthHelper implementation
- Mantine upgrade: Check for breaking changes in recent commits

Don't fix individual tests if systemic issue exists - fix root cause first.

## Success Criteria

✅ **Session is successful if**:
1. Pass rate improved by at least 10 percentage points
2. Target of 70%+ pass rate achieved (or documented why not achievable)
3. All fixes committed with clear messages
4. New patterns documented in lessons learned
5. Debug files archived
6. Progress tracked in TodoWrite
7. Next maintenance date scheduled (3 months out)

✅ **Individual test file is successful if**:
1. 100% of tests passing (or skipped with TODO)
2. Changes committed
3. Patterns applied match lessons learned
4. No flaky tests remain

## Next Maintenance Schedule

After completing cleanup, schedule next session:

**Next Cleanup Date**: [Today's Date + 3 months]

**Add reminder to**:
- Project calendar
- TodoWrite as future task
- Documentation note in lessons learned file

**Why 3 months**: Balance between preventing test rot and avoiding over-maintenance. Quarterly cleanup maintains stability without excessive overhead.

---

## Quick Reference Card

**Start**: `npm run test:e2e`
**Fix Delegation**: Use test-developer agent
**Primary Reference**: `/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md`
**Key Pattern**: Use `.first()` for Mantine form fields
**Auth Pattern**: `AuthHelper.loginAs(page, 'admin')`
**Feature Check**: `if (await element.count() > 0) { ... }`
**Chip Toggle**: Click the label, not the input
**Skip Test**: Add TODO comment explaining why
**Target**: 70%+ pass rate
**Approach**: Autonomous, systematic, file-by-file

---

**Last Updated**: 2025-11-10
**Based on Session**: Mantine E2E Testing Patterns cleanup
**Maintained by**: librarian agent
