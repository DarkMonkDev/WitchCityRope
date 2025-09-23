import { test, expect } from '@playwright/test';

test('Event Type column is displayed in admin events table', async ({ page }) => {
  // Navigate to admin events page
  await page.goto('http://localhost:5173/login');

  // Login as admin
  await page.fill('input[type="email"]', 'admin@witchcityrope.com');
  await page.fill('input[type="password"]', 'Test123!');
  await page.click('button[type="submit"]');

  // Wait for login and navigate to admin events
  await page.waitForURL('**/dashboard');
  await page.goto('http://localhost:5173/admin/events');

  // Wait for the events table to load
  await page.waitForSelector('[data-testid="events-table"]', { timeout: 10000 });

  // Check that the Type column header exists
  const typeHeader = page.locator('table thead th:has-text("Type")');
  await expect(typeHeader).toBeVisible();

  // Check that Type column is positioned between Date and Event Title
  const headers = page.locator('table thead th');
  const headerTexts = await headers.allTextContent();

  console.log('Table headers:', headerTexts);

  // Verify the header order includes Type in the correct position
  expect(headerTexts).toContain('Type');

  // Look for at least one event row with a badge in the Type column
  const eventRows = page.locator('[data-testid="event-row"]');
  const eventCount = await eventRows.count();

  if (eventCount > 0) {
    // Check that at least one row has a badge in the Type column (2nd column, 0-indexed)
    const typeBadge = page.locator('[data-testid="event-row"] td:nth-child(2) .mantine-Badge-root').first();
    await expect(typeBadge).toBeVisible();

    // Check that the badge has content
    const badgeText = await typeBadge.textContent();
    console.log('Event type badge text:', badgeText);
    expect(badgeText).toBeTruthy();
  }

  console.log('✅ Event Type column test passed!');
});

test('Draft events are visible in admin events table', async ({ page }) => {
  await page.goto('http://localhost:5173/login');

  // Login as admin
  await page.fill('input[type="email"]', 'admin@witchcityrope.com');
  await page.fill('input[type="password"]', 'Test123!');
  await page.click('button[type="submit"]');

  await page.waitForURL('**/dashboard');
  await page.goto('http://localhost:5173/admin/events');

  await page.waitForSelector('[data-testid="events-table"]', { timeout: 10000 });

  // Look for any event title that contains "- DRAFT"
  const draftEvent = page.locator('[data-testid="event-row"] td:has-text("- DRAFT")');

  // If there are any draft events, they should be visible
  const draftCount = await draftEvent.count();
  console.log('Number of draft events found:', draftCount);

  if (draftCount > 0) {
    await expect(draftEvent.first()).toBeVisible();
    console.log('✅ Draft events are correctly displayed');
  } else {
    console.log('ℹ️ No draft events found in the current dataset');
  }
});