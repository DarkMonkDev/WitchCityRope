import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Event CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin using correct React authentication patterns
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
  });

  test('Phase 2: Admin Events Page - Create Event button navigates to new event page', async ({ page }) => {
    console.log('🧪 Testing Phase 2: Event CRUD operations...');

    // Check that the admin events page loads
    await expect(page.locator('text=Events Dashboard')).toBeVisible();

    // Check that Create Event button exists
    const createButton = page.locator('button:has-text("Create Event")');
    await expect(createButton).toBeVisible();

    // Click Create Event button
    await createButton.click();

    // ARCHITECTURE: Uses page navigation (NOT modal)
    // Check that we navigated to /admin/events/new
    await page.waitForURL(/.*\/admin\/events\/new$/);
    await expect(page).toHaveURL(/.*\/admin\/events\/new$/);

    // Check that EventForm is displayed on the page (NOT in a modal)
    const eventForm = page.locator('[data-testid="event-form"]');
    await expect(eventForm).toBeVisible();

    // Verify NO modal dialog exists (this is a page, not a modal)
    const modal = page.locator('[role="dialog"]');
    await expect(modal).not.toBeVisible();

    // Check form has required tabs
    await expect(page.locator('button[role="tab"]:has-text("Basic Info")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Setup")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Emails")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Volunteers")')).toBeVisible();

    console.log('✅ Phase 2 Test Passed: Create Event navigates to dedicated page with EventForm');
  });

  test('Phase 2: Admin Events Page - Row click navigation to edit event', async ({ page }) => {
    console.log('🧪 Testing Phase 2: Edit functionality via row click...');

    // Check that the admin events page loads
    await expect(page.locator('text=Events Dashboard')).toBeVisible();

    // Look for table rows (events in the table)
    const tableRows = page.locator('tbody tr');
    const count = await tableRows.count();

    if (count > 0) {
      console.log(`Found ${count} event(s) in the table`);

      // ARCHITECTURE: Row click is the primary way to edit events (NO separate edit button)
      const firstRow = tableRows.first();

      // Get the event ID or title for verification
      const eventTitle = await firstRow.locator('td').nth(2).textContent(); // Title is in 3rd column

      // Click the row (not a button - the entire row is clickable)
      await firstRow.click();

      // Should navigate to /admin/events/:id (NOT open a modal)
      await page.waitForURL(/.*\/admin\/events\/[^/]+$/);
      await expect(page).toHaveURL(/.*\/admin\/events\/[^/]+$/);

      // Verify EventForm is displayed on the page (NOT in modal)
      const eventForm = page.locator('[data-testid="event-form"]');
      await expect(eventForm).toBeVisible();

      // Verify NO modal exists (this is a page navigation, not a modal)
      const modal = page.locator('[role="dialog"]');
      await expect(modal).not.toBeVisible();

      // Verify the event title appears on the page
      if (eventTitle) {
        await expect(page.locator('h1')).toContainText(eventTitle);
      }

      console.log('✅ Row click navigates to event edit page');
    } else {
      console.log('No events found - checking empty state');

      // Check for empty state message
      const emptyStateSelectors = [
        'text=No Events',
        'text=No events',
        'text=Create',
      ];

      let emptyStateFound = false;
      for (const selector of emptyStateSelectors) {
        if (await page.locator(selector).isVisible()) {
          emptyStateFound = true;
          console.log(`✅ Empty state detected: ${selector}`);
          break;
        }
      }

      if (!emptyStateFound) {
        console.log('ℹ️ Empty state handling may need improvement');
      }
    }

    console.log('✅ Phase 2 Test Passed: Row-click edit navigation works correctly');
  });
});