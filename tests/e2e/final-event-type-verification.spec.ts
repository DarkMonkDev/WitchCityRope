import { test, expect } from '@playwright/test';

test('Final verification of Event Type column implementation', async ({ page }) => {
  console.log('Starting final verification test...');

  await page.goto('http://localhost:5173/login');

  // Fill login form using specific selectors
  await page.locator('input').first().fill('admin@witchcityrope.com');
  await page.locator('input').nth(1).fill('Test123!');

  // Click the login button using test ID
  await page.getByTestId('login-button').click();

  // Wait for login and navigation
  await page.waitForTimeout(3000);

  // Navigate to admin events page
  await page.goto('http://localhost:5173/admin/events');

  // Wait for page to fully load
  await page.waitForTimeout(5000);

  // Take screenshot for visual verification
  await page.screenshot({
    path: 'test-results/final-admin-events-verification.png',
    fullPage: true
  });

  console.log('Screenshot saved. Checking for Type column...');

  // Check page content for verification
  const pageContent = await page.content();

  // 1. Verify Type column header exists
  const hasTypeHeader = pageContent.includes('>Type<') || pageContent.includes('Type</th>');
  console.log(`Type column header found: ${hasTypeHeader ? '✅' : '❌'}`);

  // 2. Check for event type badges
  const badges = await page.locator('.mantine-Badge-root').count();
  console.log(`Event type badges found: ${badges} badges`);

  // 3. Check table structure has 6 columns (Date, Type, Title, Time, Capacity, Actions)
  const headerCells = await page.locator('table thead th').count();
  console.log(`Table header columns: ${headerCells} columns (expected: 6)`);

  // 4. Check if any draft events are visible
  const draftEvents = pageContent.includes('- DRAFT');
  console.log(`Draft events visible: ${draftEvents ? '✅' : 'ℹ️ No draft events found'}`);

  console.log('\n=== VERIFICATION SUMMARY ===');
  console.log(`✅ Type column header: ${hasTypeHeader}`);
  console.log(`✅ Event type badges: ${badges > 0}`);
  console.log(`✅ Table structure: ${headerCells === 6} (${headerCells} columns)`);
  console.log(`✅ Draft events support: ${draftEvents ? 'Confirmed' : 'No drafts to test'}`);

  // Test passes if we have the Type header and correct number of columns
  expect(hasTypeHeader).toBe(true);
  expect(headerCells).toBe(6);

  console.log('\n🎉 Event Type column implementation verified successfully!');
});