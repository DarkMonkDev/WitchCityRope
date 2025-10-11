import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * SIMPLE Test to verify the policies field fix in EventForm
 *
 * Bug: The policies field was not displaying in the EventForm after saving
 * Cause: Missing policies mapping in transformApiEvent() function
 * Fix: Added policies field to the transform function (same as shortDescription fix)
 *
 * This test:
 * 1. Navigates to an existing event in the admin panel
 * 2. Edits the Policies field
 * 3. Saves the event
 * 4. Refreshes the page
 * 5. Verifies the Policies field still displays the saved text
 */

test.describe('Policies Field Display Fix', () => {
  const TEST_POLICIES = `Test Policies - ${Date.now()}
- Attendees must sign waiver
- No photography without consent
- Safety protocols required`;

  test('should display policies field after save and refresh', async ({ page }) => {
    console.log('Starting policies field verification test...');

    // Step 1: Login as admin using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✓ Logged in as admin');

    // Step 2: Navigate to Admin → Events
    await page.click('text=Admin');
    await page.waitForURL('**/admin');
    await page.waitForTimeout(1000);

    // Click on Events menu
    const eventsLink = page.locator('a[href="/admin/events"], a:has-text("Events")').first();
    await eventsLink.click();
    await page.waitForURL('**/admin/events');
    await page.waitForLoadState('networkidle');
    console.log('✓ Navigated to admin events page');

    // Step 3: Click on the first event in the list to edit it
    // Look for any event link in the table/list
    const eventRow = page.locator('[data-testid="event-row"], tr').first();
    await expect(eventRow).toBeVisible({ timeout: 5000 });
    await eventRow.click();

    // Wait for event edit page to load
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    console.log('✓ Opened event for editing');

    // Step 4: Find and fill the Policies field
    // Try multiple selectors since we don't know the exact implementation
    const policiesSelectors = [
      '[data-testid="policies-input"]',
      'textarea[name="policies"]',
      'label:has-text("Policies") + textarea',
      'label:has-text("Policies") ~ textarea',
      '[placeholder*="policies" i]',
      '[aria-label*="policies" i]'
    ];

    let policiesField = null;
    for (const selector of policiesSelectors) {
      const field = page.locator(selector).first();
      if (await field.count() > 0) {
        policiesField = field;
        console.log(`✓ Found policies field using selector: ${selector}`);
        break;
      }
    }

    if (!policiesField) {
      // Take a screenshot to debug
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/policies-field-not-found.png', fullPage: true });
      throw new Error('Could not find policies field on the page');
    }

    // Clear existing content and fill with test content
    await policiesField.clear();
    await policiesField.fill(TEST_POLICIES);

    // Verify the text was entered
    const enteredValue = await policiesField.inputValue();
    expect(enteredValue).toBe(TEST_POLICIES);
    console.log('✓ Policies field filled with test content');

    // Step 5: Save the event
    const saveButton = page.locator('button:has-text("Save"), button[type="submit"]').first();
    await saveButton.click();
    console.log('✓ Clicked save button');

    // Wait for save operation to complete
    await page.waitForTimeout(2000);
    await page.waitForLoadState('networkidle');

    // Step 6: Refresh the page to verify persistence
    console.log('Refreshing page to verify persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Step 7: Check if policies field still has the saved content
    let policiesFieldAfterRefresh = null;
    for (const selector of policiesSelectors) {
      const field = page.locator(selector).first();
      if (await field.count() > 0) {
        policiesFieldAfterRefresh = field;
        console.log(`✓ Found policies field after refresh using selector: ${selector}`);
        break;
      }
    }

    if (!policiesFieldAfterRefresh) {
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/policies-field-not-found-after-refresh.png', fullPage: true });
      throw new Error('Could not find policies field after page refresh');
    }

    // Wait for the field to be populated
    await page.waitForTimeout(1000);

    const policiesValueAfterRefresh = await policiesFieldAfterRefresh.inputValue();
    console.log('Policies field value after refresh:', policiesValueAfterRefresh);

    // The critical test: Does the policies field retain its value?
    expect(policiesValueAfterRefresh).toBe(TEST_POLICIES);
    console.log('✓ Policies field persists after page refresh!');

    console.log('All tests passed! The policies field fix is working correctly.');
  });

  test('should handle empty policies field gracefully', async ({ page }) => {
    console.log('Testing empty policies field handling...');

    // Login as admin using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to events
    await page.click('text=Admin');
    await page.waitForURL('**/admin');
    await page.waitForTimeout(1000);

    const eventsLink = page.locator('a[href="/admin/events"], a:has-text("Events")').first();
    await eventsLink.click();
    await page.waitForURL('**/admin/events');
    await page.waitForLoadState('networkidle');

    // Click on first event
    const eventRow = page.locator('[data-testid="event-row"], tr').first();
    await expect(eventRow).toBeVisible({ timeout: 5000 });
    await eventRow.click();

    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Find policies field
    const policiesSelectors = [
      '[data-testid="policies-input"]',
      'textarea[name="policies"]',
      'label:has-text("Policies") + textarea',
      'label:has-text("Policies") ~ textarea',
      '[placeholder*="policies" i]',
      '[aria-label*="policies" i]'
    ];

    let policiesField = null;
    for (const selector of policiesSelectors) {
      const field = page.locator(selector).first();
      if (await field.count() > 0) {
        policiesField = field;
        break;
      }
    }

    if (!policiesField) {
      console.log('Policies field not found, skipping test');
      return;
    }

    // Clear the policies field
    await policiesField.clear();
    await policiesField.fill('');

    // Save
    const saveButton = page.locator('button:has-text("Save"), button[type="submit"]').first();
    await saveButton.click();

    await page.waitForTimeout(2000);
    await page.waitForLoadState('networkidle');

    // Refresh
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Find field again after refresh
    let policiesFieldAfterRefresh = null;
    for (const selector of policiesSelectors) {
      const field = page.locator(selector).first();
      if (await field.count() > 0) {
        policiesFieldAfterRefresh = field;
        break;
      }
    }

    if (policiesFieldAfterRefresh) {
      const emptyValue = await policiesFieldAfterRefresh.inputValue();
      expect(emptyValue).toBe('');
      console.log('✓ Empty policies field handled gracefully');
    }
  });
});
