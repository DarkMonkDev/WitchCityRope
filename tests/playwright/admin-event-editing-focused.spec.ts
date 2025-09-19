import { test, expect } from '@playwright/test';

/**
 * Focused E2E tests for Admin Event Editing - Critical Issues Only
 *
 * This is a streamlined version focusing on the 4 most critical issues:
 * 1. Teacher selection persistence
 * 2. Draft/Publish status toggle
 * 3. Ticket count consistency
 * 4. Add button functionality
 */

test.describe('Admin Event Editing - Critical Issues', () => {

  // Simple error tracking
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    consoleErrors = [];
    jsErrors = [];

    page.on('pageerror', error => {
      jsErrors.push(error.toString());
    });

    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        // Only track non-CSS errors to reduce noise
        if (!errorText.includes('style property') && !errorText.includes('focus-visible')) {
          consoleErrors.push(errorText);
        }
      }
    });
  });

  // Helper to login and navigate to event editing
  const setupEventEdit = async (page) => {
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForURL('**/dashboard', { timeout: 10000 });

    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');

    const eventRows = page.locator('[data-testid="event-row"]');
    const rowCount = await eventRows.count();

    if (rowCount === 0) {
      throw new Error('No events found for testing');
    }

    await eventRows.first().click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
  };

  test('ISSUE 1: Teacher selection should persist after save and refresh', async ({ page }) => {
    await setupEventEdit(page);

    console.log('🧪 Testing teacher selection persistence...');

    // Go to setup tab
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(500);

    // Check if teacher select exists and try to select
    const teacherSelect = page.locator('[data-testid="teacher-select"]').first();

    if (await teacherSelect.count() > 0) {
      console.log('✅ Teacher select found');

      // Get current value
      const originalValue = await teacherSelect.inputValue().catch(() => '');
      console.log('🔍 Original teacher value:', originalValue);

      // Try to open dropdown and select different option if possible
      await teacherSelect.click();
      await page.waitForTimeout(500);

      const teacherOptions = page.locator('[role="option"]').first();
      if (await teacherOptions.count() > 0) {
        await teacherOptions.click();
        console.log('✅ Teacher option selected');

        // Save changes
        const saveButton = page.locator('[data-testid="save-button"]');
        if (await saveButton.count() > 0) {
          await saveButton.click();
          await page.waitForTimeout(2000);
          console.log('✅ Save button clicked');

          // Refresh page
          await page.reload();
          await page.waitForLoadState('networkidle');
          await page.click('[data-testid="setup-tab"]');
          await page.waitForTimeout(500);

          // Check if selection persisted
          const newValue = await page.locator('[data-testid="teacher-select"]').first().inputValue().catch(() => '');
          console.log('🔍 Teacher value after refresh:', newValue);

          if (newValue === originalValue) {
            console.log('🚨 ISSUE CONFIRMED: Teacher selection did not persist');
          } else {
            console.log('✅ Teacher selection persisted correctly');
          }
        }
      }
    } else {
      console.log('⚠️ Teacher select not found - cannot test persistence');
    }
  });

  test('ISSUE 2: Draft/Publish status toggle should work immediately', async ({ page }) => {
    await setupEventEdit(page);

    console.log('🧪 Testing status toggle...');

    // Find status control
    const statusControl = page.locator('.mantine-SegmentedControl-root').first();

    if (await statusControl.count() > 0) {
      // Get current status
      const currentActiveLabel = await page.locator('.mantine-SegmentedControl-control[data-active="true"] .mantine-SegmentedControl-label').textContent();
      console.log('🔍 Current status:', currentActiveLabel);

      // Click opposite status
      const targetStatus = currentActiveLabel?.includes('DRAFT') ? 'PUBLISHED' : 'DRAFT';
      const targetButton = page.locator(`.mantine-SegmentedControl-label:text("${targetStatus}")`);

      if (await targetButton.count() > 0) {
        await targetButton.click();
        console.log(`🧪 Clicked ${targetStatus} button`);

        // Look for confirmation modal
        await page.waitForTimeout(1000);
        const modal = page.locator('.mantine-Modal-root').first();

        if (await modal.isVisible().catch(() => false)) {
          console.log('✅ Confirmation modal appeared');

          // Confirm change
          const confirmButton = page.locator('button').filter({ hasText: /publish|unpublish/i }).first();
          if (await confirmButton.count() > 0) {
            await confirmButton.click();
            await page.waitForTimeout(1000);

            // Check if status changed
            const newActiveLabel = await page.locator('.mantine-SegmentedControl-control[data-active="true"] .mantine-SegmentedControl-label').textContent();
            console.log('🔍 Status after change:', newActiveLabel);

            if (newActiveLabel === currentActiveLabel) {
              console.log('🚨 ISSUE CONFIRMED: Status did not change after modal confirmation');
            } else {
              console.log('✅ Status changed successfully');
            }
          }
        } else {
          console.log('⚠️ No confirmation modal appeared');
        }
      }
    } else {
      console.log('⚠️ Status control not found');
    }
  });

  test('ISSUE 3: Ticket counts should be consistent across tabs', async ({ page }) => {
    await setupEventEdit(page);

    console.log('🧪 Testing ticket count consistency...');

    // Check Setup tab
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(500);

    const setupTabText = await page.locator('body').textContent();
    const setupTicketMatch = setupTabText?.match(/(\d+)\s+tickets?\s+sold/i);
    const setupCount = setupTicketMatch ? parseInt(setupTicketMatch[1]) : 0;
    console.log('🔍 Setup tab ticket count:', setupCount);

    // Check RSVP/Tickets tab
    await page.click('[data-testid="rsvp-tickets-tab"]');
    await page.waitForTimeout(500);

    // Count actual ticket rows
    const ticketRows = page.locator('[data-testid="ticket-row"]');
    const actualTicketCount = await ticketRows.count();
    console.log('🔍 RSVP tab actual ticket rows:', actualTicketCount);

    // Check for discrepancy
    if (setupCount !== actualTicketCount) {
      console.log(`🚨 ISSUE CONFIRMED: Ticket count mismatch - Setup: ${setupCount}, Actual: ${actualTicketCount}`);

      if (setupCount === 10 && actualTicketCount === 0) {
        console.log('🚨 SPECIFIC ISSUE: Hardcoded "10 tickets sold" detected');
      }
    } else {
      console.log('✅ Ticket counts are consistent');
    }
  });

  test('ISSUE 4: Add buttons should work on first attempt', async ({ page }) => {
    await setupEventEdit(page);

    console.log('🧪 Testing add button functionality...');

    // Test add volunteer position
    await page.click('[data-testid="tab-volunteers"]');
    await page.waitForTimeout(500);

    const addVolunteerButton = page.locator('button').filter({ hasText: /add.*position/i }).first();

    if (await addVolunteerButton.count() > 0) {
      const urlBefore = page.url();
      await addVolunteerButton.click();
      await page.waitForTimeout(1000);

      const urlAfter = page.url();

      if (urlAfter !== urlBefore) {
        console.log('🚨 ISSUE CONFIRMED: Add volunteer button caused page refresh');
      } else {
        console.log('✅ Add volunteer button worked without refresh');
      }
    }

    // Test add session
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(500);

    const addSessionButton = page.locator('button').filter({ hasText: /add.*session/i }).first();

    if (await addSessionButton.count() > 0) {
      const urlBefore = page.url();
      await addSessionButton.click();
      await page.waitForTimeout(1000);

      const urlAfter = page.url();

      if (urlAfter !== urlBefore) {
        console.log('🚨 ISSUE CONFIRMED: Add session button caused page refresh');
      } else {
        console.log('✅ Add session button worked without refresh');
      }
    }
  });

  test('Summary: Check for critical JavaScript errors', async ({ page }) => {
    await setupEventEdit(page);

    console.log('🧪 Testing all tabs for critical errors...');

    const tabs = ['[data-testid="tab-basic-info"]', '[data-testid="setup-tab"]', '[data-testid="rsvp-tickets-tab"]', '[data-testid="tab-volunteers"]'];

    for (const tab of tabs) {
      await page.click(tab);
      await page.waitForTimeout(500);
    }

    console.log(`🔍 JavaScript errors found: ${jsErrors.length}`);
    console.log(`🔍 Console errors found: ${consoleErrors.length}`);

    if (jsErrors.length > 0) {
      console.log('🚨 CRITICAL JavaScript errors:', jsErrors);
    }

    if (consoleErrors.length > 0) {
      console.log('⚠️ Console errors:', consoleErrors);
    }

    if (jsErrors.length === 0 && consoleErrors.length === 0) {
      console.log('✅ No critical errors found');
    }
  });
});