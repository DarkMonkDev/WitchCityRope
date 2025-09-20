import { test, expect } from '@playwright/test';

test('Admin Display Check - Working Approach', async ({ page }) => {
  console.log('🔍 Checking admin display issues with working login approach');

  // Navigate to login page directly (like the working test)
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  // Use data-testid selectors (like the working test)
  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
  await page.fill('[data-testid="password-input"]', 'Test123!');
  await page.click('[data-testid="login-button"]');
  await page.waitForTimeout(3000);

  console.log('✅ Logged in as admin');
  console.log('Current URL:', page.url());

  // Navigate to admin events page
  await page.goto('http://localhost:5173/admin/events');
  await page.waitForTimeout(2000);

  if (page.url().includes('login')) {
    throw new Error('Authentication failed - redirected to login page');
  }

  console.log('✅ Successfully accessed admin events page');
  await page.screenshot({ path: 'test-results/admin-events-capacity-check.png', fullPage: true });

  // Check for the Rope Social event in the table
  const ropeSocialElement = page.locator('text="Rope Social & Discussion"');
  if (await ropeSocialElement.isVisible()) {
    console.log('✅ Found Rope Social & Discussion event');

    // Find the parent row
    const row = ropeSocialElement.locator('..').locator('..');
    const rowText = await row.textContent();
    console.log('Row content:', rowText);

    // Check if the row contains RSVP count
    if (rowText?.includes('2')) {
      console.log('✅ Row contains RSVP count (2)');
    } else {
      console.log('❌ Row does not contain RSVP count (2)');
    }
  } else {
    console.log('❌ Rope Social event not found in admin events list');
  }

  // Navigate to the specific event admin page
  await page.goto('http://localhost:5173/admin/events/5290be55-59e0-4ec9-b62b-5cc215e6e848');
  await page.waitForTimeout(2000);

  console.log('✅ Navigated to Rope Social admin page');
  await page.screenshot({ path: 'test-results/rope-social-admin-rsvp-check.png', fullPage: true });

  // Check for RSVP/Tickets tab
  const rsvpTabSelectors = [
    'text="RSVPs and Tickets"',
    'text="Tickets"',
    'text="RSVP"',
    'text="Participations"'
  ];

  let tabFound = false;
  for (const selector of rsvpTabSelectors) {
    const tab = page.locator(selector);
    if (await tab.isVisible()) {
      console.log(`✅ Found tab: ${selector}`);
      await tab.click();
      await page.waitForTimeout(1000);
      tabFound = true;
      break;
    }
  }

  if (!tabFound) {
    console.log('❌ No RSVP/Tickets tab found');
    const allText = await page.textContent('body');
    const hasRSVPData = allText?.includes('admin@witchcityrope.com') || allText?.includes('2 participants');
    console.log(`Page contains RSVP data: ${hasRSVPData}`);
  }

  // Take final screenshot
  await page.screenshot({ path: 'test-results/final-rsvp-tab-state.png', fullPage: true });

  console.log('🎉 Admin display check completed');
});