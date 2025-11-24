import { test, expect } from '@playwright/test';

test('Visual check of admin events page with type column', async ({ page }) => {
  await page.goto('http://localhost:5173/login');

  // Fill the form using more direct selectors
  const emailInput = page.locator('input').first(); // First input field
  const passwordInput = page.locator('input').nth(1); // Second input field

  await emailInput.fill('admin@witchcityrope.com');
  await passwordInput.fill('Test123!');

  // Click the login button (likely the only button visible)
  await page.getByRole('button').click();

  // Wait for navigation and go to admin events
  await page.waitForTimeout(3000); // Give time for login
  await page.goto('http://localhost:5173/admin/events');

  // Wait for page to load
  await page.waitForTimeout(3000);

  // Take a screenshot of the admin events page
  await page.screenshot({
    path: 'test-results/admin-events-page-verification.png',
    fullPage: true
  });

  // Look for Type column header
  const pageContent = await page.content();

  console.log('Page loaded, looking for Type column...');

  // Check if "Type" appears in a table header
  if (pageContent.includes('>Type<')) {
    console.log('✅ Type column header found in table!');
  } else {
    console.log('❌ Type column header not found');
  }

  // Check for badge elements (which would indicate event types)
  const badges = page.locator('.mantine-Badge-root');
  const badgeCount = await badges.count();

  console.log(`Found ${badgeCount} badges on the page (these should be event type badges)`);

  if (badgeCount > 0) {
    // Get text from first few badges
    for (let i = 0; i < Math.min(3, badgeCount); i++) {
      const badgeText = await badges.nth(i).textContent();
      console.log(`Badge ${i + 1}: "${badgeText}"`);
    }
  }

  console.log('Visual verification test completed!');
});