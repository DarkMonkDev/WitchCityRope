import { test, expect } from '@playwright/test';

test('Login and check event type column', async ({ page }) => {
  // Monitor console errors
  const consoleErrors: string[] = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      consoleErrors.push(msg.text());
    }
  });

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

  // Hard assertion: verify at least one event row exists
  await expect(eventRows.first()).toBeVisible({ timeout: 5000 });
  expect(rowCount).toBeGreaterThan(0);

  // Hard assertion: Check for badges in the Type column (should be second column)
  const firstTypeBadge = page.locator('[data-testid="event-row"]:first-child td:nth-child(2) .mantine-Badge-root');
  await expect(firstTypeBadge).toBeVisible({ timeout: 5000 });

  const badgeText = await firstTypeBadge.textContent();
  expect(badgeText).toBeTruthy();
  console.log(`✅ Found event type badge: "${badgeText}"`);

  // Verify no console errors occurred
  expect(consoleErrors).toEqual([]);

  console.log('Test completed successfully!');
});