import { test, expect } from '@playwright/test';

test('Admin RSVP Debug - Focused Test', async ({ page }) => {
  console.log('🔍 Starting focused admin RSVP debug test');

  // Navigate to login page
  await page.goto('http://localhost:5173/');
  await page.waitForLoadState('networkidle');

  // Click login and fill form
  await page.getByText('LOGIN').click();
  await page.waitForSelector('input[name="email"]', { timeout: 10000 });
  await page.fill('input[name="email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="password"]', 'Test123!');
  await page.getByText('SIGN IN').click();

  // Wait for redirect to dashboard
  await page.waitForURL('**/dashboard', { timeout: 15000 });
  console.log('✅ Successfully logged in as admin');

  // Take screenshot after login
  await page.screenshot({ path: 'test-results/admin-after-login.png', fullPage: true });

  // Navigate to admin events page
  await page.goto('http://localhost:5173/admin/events');
  await page.waitForLoadState('networkidle');

  console.log('✅ Navigated to admin events page');
  await page.screenshot({ path: 'test-results/admin-events-list.png', fullPage: true });

  // Look for the Rope Social event in the table and examine its capacity column
  const tableRows = await page.locator('tr').count();
  console.log(`Found ${tableRows} table rows`);

  // Find the specific Rope Social event row
  const ropeSocialRow = page.locator('tr:has-text("Rope Social & Discussion")');
  if (await ropeSocialRow.isVisible()) {
    console.log('✅ Found Rope Social & Discussion event in table');

    // Get all cell contents for this row
    const cells = await ropeSocialRow.locator('td').allTextContents();
    console.log('Row cell contents:', cells);

    // Check if any cell contains RSVP count
    const hasRSVPCount = cells.some(cell => cell.includes('2') || cell.includes('RSVP'));
    console.log(`Row contains RSVP count (2): ${hasRSVPCount}`);
  } else {
    console.log('❌ Rope Social event not found in table');
  }

  // Navigate to the specific event admin page
  await page.goto('http://localhost:5173/admin/events/5290be55-59e0-4ec9-b62b-5cc215e6e848');
  await page.waitForLoadState('networkidle');

  console.log('✅ Navigated to Rope Social admin page');
  await page.screenshot({ path: 'test-results/rope-social-admin-page.png', fullPage: true });

  // Look for tabs and content
  const allText = await page.textContent('body');
  const hasRSVPText = allText?.includes('RSVP') || allText?.includes('Participation');
  console.log(`Page contains RSVP/Participation text: ${hasRSVPText}`);

  // Look for specific tabs
  const tabs = await page.locator('[role="tab"], button:has-text("tab"), button:has-text("RSVP"), button:has-text("Ticket"), button:has-text("Participation")').allTextContents();
  console.log('Found tabs:', tabs);

  // Check if there's any data about participations
  const participationCount = await page.locator('text="admin"').count();
  console.log(`Elements containing "admin": ${participationCount}`);

  // Check page title/heading to confirm we're on the right page
  const pageHeading = await page.locator('h1, h2, h3').first().textContent();
  console.log('Page heading:', pageHeading);

  console.log('🎉 Focused admin debug test completed');
});