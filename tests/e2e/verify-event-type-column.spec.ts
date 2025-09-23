import { test, expect } from '@playwright/test';

test('Login and check event type column', async ({ page }) => {
  // Go to login page
  await page.goto('http://localhost:5173/login');

  // Fill login form - using more specific selectors
  await page.getByLabel('Email Address').fill('admin@witchcityrope.com');
  await page.getByLabel('Password').fill('Test123!');

  // Submit the form
  await page.getByRole('button', { name: 'Sign In' }).click();

  // Wait for dashboard page
  await page.waitForURL('**/dashboard', { timeout: 10000 });

  // Navigate to admin events
  await page.goto('http://localhost:5173/admin/events');

  // Wait for page to load
  await page.waitForLoadState('networkidle');

  // Take screenshot to verify the table structure
  await page.screenshot({ path: 'test-results/admin-events-with-type-column.png', fullPage: true });

  // Check for Type column header
  const typeHeader = page.locator('th', { hasText: 'Type' });
  await expect(typeHeader).toBeVisible();

  console.log('✅ Type column header found');

  // Check if any events are visible
  const eventRows = page.locator('[data-testid="event-row"]');
  const rowCount = await eventRows.count();

  console.log(`Found ${rowCount} event rows`);

  if (rowCount > 0) {
    // Check for badges in the Type column (should be second column)
    const firstTypeBadge = page.locator('[data-testid="event-row"]:first-child td:nth-child(2) .mantine-Badge-root');

    if (await firstTypeBadge.isVisible()) {
      const badgeText = await firstTypeBadge.textContent();
      console.log(`✅ Found event type badge: "${badgeText}"`);
    } else {
      console.log('ℹ️ No type badge found in first row');
    }
  }

  console.log('Test completed successfully!');
});