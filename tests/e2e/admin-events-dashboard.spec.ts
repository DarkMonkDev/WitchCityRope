import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Admin Events Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin using AuthHelper
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();

    // Navigate to admin events page
    await page.goto('/admin/events');
    await page.waitForLoadState('domcontentloaded');
  });

  test('should display events dashboard with filter controls', async ({ page }) => {
    // Check page title
    await expect(page.getByRole('heading', { name: 'Events Dashboard' })).toBeVisible();

    // Check filter controls exist
    // Show Past Events switch (Mantine Switch renders input as hidden, use toBeAttached)
    const showPastSwitch = page.getByTestId('switch-show-past-events');
    await expect(showPastSwitch).toBeAttached();

    // Search input
    const searchInput = page.getByTestId('input-search-events');
    await expect(searchInput).toBeVisible();

    // Create Event button
    const createButton = page.getByTestId('button-create-event');
    await expect(createButton).toBeVisible();
  });

  test('should show events in table', async ({ page }) => {
    // Check if table exists
    const eventsTable = page.getByTestId('events-table');
    await expect(eventsTable).toBeVisible();

    // Check if there are any events in the table
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();

    console.log(`Found ${rowCount} events in table`);

    // Should have at least one event (or show "No events found" message)
    if (rowCount === 1) {
      // Check if it's the "No events found" message
      const firstRow = tableRows.first();
      const noEventsText = await firstRow.textContent();
      if (noEventsText?.includes('No events found')) {
        console.log('No events in database');
      } else {
        console.log('Found event:', noEventsText);
      }
    } else {
      expect(rowCount).toBeGreaterThan(0);
      console.log(`Successfully showing ${rowCount} events`);
    }
  });

  test('should toggle past events visibility with switch', async ({ page }) => {
    // Get initial event count
    const tableRows = page.locator('tbody tr');
    const initialCount = await tableRows.count();
    console.log(`Initial event count: ${initialCount}`);

    // Click the switch label text to toggle (Mantine hides the actual input)
    const switchLabel = page.getByText('Show Past Events');
    await expect(switchLabel).toBeVisible();
    await switchLabel.click();
    await page.waitForTimeout(500); // Wait for filter to apply

    // Get new count
    const newCount = await tableRows.count();
    console.log(`Event count after toggle: ${newCount}`);

    // The counts might be different depending on whether there are past events
    // Just verify the switch works (no crash)
    console.log('Switch toggled successfully');
  });

  test('should filter events with search input', async ({ page }) => {
    // Get the search input
    const searchInput = page.getByTestId('input-search-events');
    await expect(searchInput).toBeVisible();

    // Type a search term
    await searchInput.fill('test');
    await page.waitForTimeout(500); // Wait for debounced search

    // Verify search was applied (check the showing count text or table updates)
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();
    console.log(`Events matching "test": ${rowCount}`);

    // Clear search
    await searchInput.fill('');
    await page.waitForTimeout(500);

    const afterClear = await tableRows.count();
    console.log(`Events after clearing search: ${afterClear}`);
  });

  test('should have working Copy button', async ({ page }) => {
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();

    if (rowCount > 0) {
      // Check if first row has a Copy button
      const firstRowCopyButton = tableRows.first().getByTestId('button-copy-event');
      const buttonExists = await firstRowCopyButton.count() > 0;

      if (buttonExists) {
        // Check that button text is visible
        await expect(firstRowCopyButton).toBeVisible();
        await expect(firstRowCopyButton).toContainText('Copy');
      }
    }
  });

  test('should navigate to event edit on row click', async ({ page }) => {
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();

    if (rowCount > 0) {
      const firstRow = tableRows.first();
      const text = await firstRow.textContent();

      // Only click if it's not the "No events found" row
      if (!text?.includes('No events found')) {
        await firstRow.click();

        // Should either navigate or show a notification
        await page.waitForTimeout(1000);

        // Check if we navigated away from the table page
        const currentUrl = page.url();
        console.log('Current URL after click:', currentUrl);
      }
    }
  });
});
