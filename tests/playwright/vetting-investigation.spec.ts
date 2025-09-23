import { test, expect } from '@playwright/test';

test('Investigate admin vetting application details navigation', async ({ page }) => {
  console.log('🔍 Starting admin vetting investigation...');

  // Navigate to React app
  await page.goto('http://localhost:5173');
  console.log('📍 Navigated to React app');

  // Check if page loads correctly
  const title = await page.title();
  console.log(`📋 Page title: ${title}`);

  // Look for login button and click it
  await page.click('[data-testid="login-button"]');
  console.log('🔐 Clicked login button');

  // Fill login form
  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
  await page.fill('[data-testid="password-input"]', 'Test123!');

  // Submit login
  await page.click('[data-testid="login-submit-button"]');
  console.log('📝 Submitted login form');

  // Wait for navigation to dashboard
  await page.waitForURL(/dashboard/, { timeout: 10000 });
  console.log('✅ Successfully logged in and navigated to dashboard');

  // Navigate to admin vetting area
  console.log('🔍 Looking for admin navigation...');

  // Check if admin menu exists
  const adminMenuExists = await page.locator('[data-testid="admin-menu"]').isVisible();
  console.log(`📋 Admin menu visible: ${adminMenuExists}`);

  if (adminMenuExists) {
    await page.click('[data-testid="admin-menu"]');
    console.log('📌 Clicked admin menu');
  }

  // Try to navigate to vetting page
  try {
    await page.goto('http://localhost:5173/admin/vetting');
    console.log('📍 Navigated directly to /admin/vetting');
  } catch (error) {
    console.log(`❌ Error navigating to vetting: ${error}`);
  }

  // Check if the vetting page loaded
  const currentUrl = page.url();
  console.log(`📍 Current URL: ${currentUrl}`);

  // Look for application rows or list
  const applicationRows = await page.locator('[data-testid*="application-row"]').count();
  console.log(`📋 Found ${applicationRows} application rows`);

  // If no specific test ids, look for table rows or list items
  if (applicationRows === 0) {
    const tableRows = await page.locator('tbody tr').count();
    const listItems = await page.locator('ul li').count();
    console.log(`📋 Found ${tableRows} table rows, ${listItems} list items`);

    // Try clicking the first table row if it exists
    if (tableRows > 0) {
      console.log('🔍 Clicking first table row...');
      await page.click('tbody tr:first-child');

      // Wait a moment for navigation
      await page.waitForTimeout(2000);

      const newUrl = page.url();
      console.log(`📍 URL after click: ${newUrl}`);

      // Check if URL changed to details page
      if (newUrl.includes('/applications/')) {
        console.log('✅ Successfully navigated to application details');
      } else {
        console.log('❌ Did not navigate to application details');
      }
    }
  }

  // Take screenshot for debugging
  await page.screenshot({ path: '/tmp/vetting-investigation.png', fullPage: true });
  console.log('📸 Screenshot saved for debugging');

  // Check console for any errors
  const logs = [];
  page.on('console', msg => logs.push(`${msg.type()}: ${msg.text()}`));

  console.log('📝 Browser console logs:');
  logs.forEach(log => console.log(`  ${log}`));
});