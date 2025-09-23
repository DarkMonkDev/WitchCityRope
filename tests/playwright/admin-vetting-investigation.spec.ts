import { test, expect } from '@playwright/test';

test('Admin vetting application details investigation', async ({ page }) => {
  console.log('🔍 Starting admin vetting investigation...');

  // Navigate to React app
  await page.goto('http://localhost:5173');
  console.log('📍 Navigated to React app');

  // Wait for page to load
  await page.waitForTimeout(2000);

  // Look for LOGIN button in navigation (visible in screenshot)
  await page.click('text=LOGIN');
  console.log('🔐 Clicked LOGIN button');

  // Wait for login modal/form to appear
  await page.waitForTimeout(1000);

  // Fill login form - look for actual form fields
  const emailField = page.locator('input[type="email"]').first();
  const passwordField = page.locator('input[type="password"]').first();

  if (await emailField.isVisible()) {
    await emailField.fill('admin@witchcityrope.com');
    console.log('📧 Filled email field');
  } else {
    console.log('❌ Email field not found');
  }

  if (await passwordField.isVisible()) {
    await passwordField.fill('Test123!');
    console.log('🔑 Filled password field');
  } else {
    console.log('❌ Password field not found');
  }

  // Look for submit button
  const submitButton = page.locator('button[type="submit"]').first();
  if (await submitButton.isVisible()) {
    await submitButton.click();
    console.log('📝 Clicked submit button');
  } else {
    // Try other possible submit buttons
    const signInButton = page.locator('text=Sign In');
    if (await signInButton.isVisible()) {
      await signInButton.click();
      console.log('📝 Clicked Sign In button');
    } else {
      console.log('❌ No submit button found');
    }
  }

  // Wait for login to process
  await page.waitForTimeout(3000);

  // Check current URL after login
  const currentUrl = page.url();
  console.log(`📍 Current URL after login: ${currentUrl}`);

  // Try to navigate directly to admin vetting
  console.log('🔍 Attempting to navigate to /admin/vetting...');
  await page.goto('http://localhost:5173/admin/vetting');
  await page.waitForTimeout(3000);

  // Check if we're on the vetting page
  const vettingUrl = page.url();
  console.log(`📍 Vetting page URL: ${vettingUrl}`);

  // Take screenshot of vetting page
  await page.screenshot({ path: '/tmp/admin-vetting-page.png', fullPage: true });
  console.log('📸 Screenshot of vetting page saved');

  // Check page content
  const pageText = await page.locator('body').textContent();
  console.log('📋 Page contains "vetting":', pageText?.toLowerCase().includes('vetting') || false);
  console.log('📋 Page contains "application":', pageText?.toLowerCase().includes('application') || false);

  // Look for any table rows or clickable elements
  const tableRows = await page.locator('table tbody tr').count();
  const listItems = await page.locator('ul li').count();
  const cards = await page.locator('[class*="card"]').count();

  console.log(`📋 Found: ${tableRows} table rows, ${listItems} list items, ${cards} cards`);

  // Check for any clickable elements that might be applications
  const clickableElements = await page.locator('tr[role="button"], tr[style*="cursor"], .clickable, [onClick]').count();
  console.log(`📋 Found ${clickableElements} potentially clickable elements`);

  // If we find table rows, try clicking the first one
  if (tableRows > 0) {
    console.log('🔍 Clicking first table row...');
    const firstRow = page.locator('table tbody tr').first();

    // Check if row has click handler
    const onClick = await firstRow.getAttribute('onClick');
    const style = await firstRow.getAttribute('style');
    const role = await firstRow.getAttribute('role');

    console.log(`📋 First row attributes - onClick: ${onClick}, style: ${style}, role: ${role}`);

    await firstRow.click();
    await page.waitForTimeout(2000);

    const newUrl = page.url();
    console.log(`📍 URL after clicking row: ${newUrl}`);

    if (newUrl.includes('/applications/') || newUrl !== vettingUrl) {
      console.log('✅ Navigation occurred after clicking row');
    } else {
      console.log('❌ No navigation after clicking row');
    }
  }

  // Check for any route parameters in current URL
  if (vettingUrl.includes('/applications/')) {
    console.log('✅ Successfully navigated to application details');
    // Take screenshot of details page
    await page.screenshot({ path: '/tmp/application-details.png', fullPage: true });
  } else {
    console.log('❌ Did not navigate to application details');
  }

  // Check console for any JavaScript errors
  const errors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });

  console.log('📝 JavaScript errors found:', errors.length);
  errors.forEach(error => console.log(`  ❌ ${error}`));

  // Check for missing routes by testing direct navigation
  console.log('🔍 Testing direct navigation to application details...');
  await page.goto('http://localhost:5173/admin/vetting/applications/123');
  await page.waitForTimeout(2000);

  const detailsUrl = page.url();
  const detailsText = await page.locator('body').textContent();

  console.log(`📍 Details page URL: ${detailsUrl}`);
  console.log('📋 Details page shows "404" or "not found":',
    detailsText?.toLowerCase().includes('404') ||
    detailsText?.toLowerCase().includes('not found') || false);

  await page.screenshot({ path: '/tmp/direct-details-navigation.png', fullPage: true });
  console.log('📸 Screenshot of direct details navigation saved');
});