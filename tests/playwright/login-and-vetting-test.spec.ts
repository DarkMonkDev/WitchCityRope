import { test, expect } from '@playwright/test';

test('Complete login and investigate vetting issue', async ({ page }) => {
  console.log('🔍 Testing complete login flow and vetting access...');

  // Navigate to React app
  await page.goto('http://localhost:5173');
  console.log('📍 Navigated to React app');

  // Click LOGIN button
  await page.click('text=LOGIN');
  console.log('🔐 Clicked LOGIN button');

  // Wait for login form to appear
  await page.waitForSelector('input[type="email"], input[placeholder*="email"]', { timeout: 5000 });

  // Fill login form using the visible form fields
  await page.fill('input[type="email"], input[placeholder*="email"]', 'admin@witchcityrope.com');
  console.log('📧 Filled email field');

  await page.fill('input[type="password"], input[placeholder*="password"]', 'Test123!');
  console.log('🔑 Filled password field');

  // Click the SIGN IN button
  await page.click('text=SIGN IN');
  console.log('📝 Clicked SIGN IN button');

  // Wait for login to process and check for navigation
  await page.waitForTimeout(5000);

  const currentUrl = page.url();
  console.log(`📍 URL after login attempt: ${currentUrl}`);

  // Check if we're still on login page or if we've been redirected
  if (currentUrl.includes('/login')) {
    console.log('❌ Still on login page - login may have failed');

    // Check for any error messages
    const errorText = await page.locator('body').textContent();
    if (errorText?.toLowerCase().includes('error') ||
        errorText?.toLowerCase().includes('invalid') ||
        errorText?.toLowerCase().includes('failed')) {
      console.log('🔍 Possible error messages found in page');
    }

    // Take screenshot of login state
    await page.screenshot({ path: '/tmp/login-attempt-result.png', fullPage: true });
  } else {
    console.log('✅ Successfully navigated away from login page');
  }

  // Wait for any authentication to settle
  await page.waitForTimeout(3000);

  // Now try to access admin vetting directly
  console.log('🔍 Attempting to navigate to admin vetting...');
  await page.goto('http://localhost:5173/admin/vetting');
  await page.waitForTimeout(3000);

  const vettingUrl = page.url();
  console.log(`📍 Vetting page URL: ${vettingUrl}`);

  if (vettingUrl.includes('/login')) {
    console.log('❌ Redirected back to login - authentication failed');
    console.log('🔍 Return URL:', new URL(vettingUrl).searchParams.get('returnTo'));
  } else {
    console.log('✅ Successfully accessed admin vetting page');

    // Take screenshot of vetting page
    await page.screenshot({ path: '/tmp/successful-vetting-access.png', fullPage: true });

    // Look for application list or table
    const hasTable = await page.locator('table').count() > 0;
    const hasCards = await page.locator('[class*="card"]').count() > 0;
    const hasList = await page.locator('ul li').count() > 0;

    console.log(`📋 Page structure - Table: ${hasTable}, Cards: ${hasCards}, List: ${hasList}`);

    if (hasTable) {
      const rowCount = await page.locator('table tbody tr').count();
      console.log(`📋 Found ${rowCount} table rows`);

      if (rowCount > 0) {
        console.log('🔍 Testing click on first application row...');

        // Get the first row
        const firstRow = page.locator('table tbody tr').first();

        // Check if row is clickable
        const isClickable = await firstRow.evaluate(el => {
          const style = window.getComputedStyle(el);
          return style.cursor === 'pointer' || el.onclick !== null || el.getAttribute('role') === 'button';
        });

        console.log(`📋 First row is clickable: ${isClickable}`);

        // Click the row
        await firstRow.click();
        await page.waitForTimeout(2000);

        const newUrl = page.url();
        console.log(`📍 URL after clicking row: ${newUrl}`);

        if (newUrl !== vettingUrl) {
          console.log('✅ Navigation occurred - application details page opened');
          await page.screenshot({ path: '/tmp/application-details-page.png', fullPage: true });
        } else {
          console.log('❌ No navigation occurred when clicking row');

          // Check if there are any click handlers or if we need to click something specific
          const cellCount = await firstRow.locator('td').count();
          console.log(`📋 Row has ${cellCount} cells`);

          // Try clicking different parts of the row
          if (cellCount > 0) {
            console.log('🔍 Trying to click first cell...');
            await firstRow.locator('td').first().click();
            await page.waitForTimeout(1000);

            const cellClickUrl = page.url();
            if (cellClickUrl !== vettingUrl) {
              console.log('✅ Navigation occurred after clicking cell');
            } else {
              console.log('❌ No navigation after clicking cell either');
            }
          }
        }
      }
    }
  }

  // Test direct navigation to a specific application ID
  console.log('🔍 Testing direct navigation to application details...');
  await page.goto('http://localhost:5173/admin/vetting/applications/123');
  await page.waitForTimeout(2000);

  const directUrl = page.url();
  console.log(`📍 Direct application URL: ${directUrl}`);

  if (directUrl.includes('/applications/123')) {
    console.log('✅ Route exists for application details');
    await page.screenshot({ path: '/tmp/direct-application-route.png', fullPage: true });
  } else if (directUrl.includes('/login')) {
    console.log('❌ Redirected to login - still not authenticated');
  } else {
    console.log('❌ Route may not exist or redirected elsewhere');
    await page.screenshot({ path: '/tmp/missing-route-result.png', fullPage: true });
  }

  // Check browser console for any errors
  const consoleErrors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      consoleErrors.push(msg.text());
    }
  });

  console.log(`📝 Console errors found: ${consoleErrors.length}`);
  consoleErrors.forEach((error, index) => {
    console.log(`  ${index + 1}. ${error}`);
  });
});