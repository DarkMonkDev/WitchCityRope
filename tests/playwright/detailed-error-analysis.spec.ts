import { test, expect } from '@playwright/test';

test('Detailed Error Analysis - Dashboard and Navigation Bugs', async ({ page }) => {
  console.log('🚨 CRITICAL: Starting comprehensive error analysis...');

  // Collect console errors
  const consoleErrors: string[] = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      consoleErrors.push(`❌ Console Error: ${msg.text()}`);
    }
  });

  // Collect page errors
  const pageErrors: string[] = [];
  page.on('pageerror', err => {
    pageErrors.push(`❌ Page Error: ${err.message}`);
  });

  // Collect failed network requests
  const failedRequests: string[] = [];
  page.on('response', response => {
    if (response.status() >= 400) {
      failedRequests.push(`❌ API Error: ${response.status()} ${response.url()}`);
    }
  });

  // Navigate to the app
  console.log('🔍 Navigating to app...');
  await page.goto('http://localhost:5173');

  // Wait for any initial errors to surface
  await page.waitForTimeout(5000);

  // Check basic app state
  const title = await page.title();
  const rootContent = await page.locator('#root').innerHTML();

  console.log(`📄 Page title: ${title}`);
  console.log(`🔍 Root content length: ${rootContent.length} characters`);

  // Check if we have the loading spinner
  const loadingSpinner = await page.locator('text=Loading events').isVisible();
  console.log(`⏳ Events loading spinner visible: ${loadingSpinner}`);

  if (loadingSpinner) {
    console.log('⚠️  Events are stuck loading - API issue detected');
  }

  // Try to click Login button
  console.log('🔑 Testing Login button click...');
  const loginButton = page.locator('text=LOGIN').first();
  const loginExists = await loginButton.isVisible();
  console.log(`🔍 LOGIN button visible: ${loginExists}`);

  if (loginExists) {
    console.log('👆 Clicking LOGIN button...');
    await loginButton.click();

    // Wait for navigation/modal
    await page.waitForTimeout(3000);

    // Check what happened after login click
    const currentUrl = page.url();
    console.log(`🔍 URL after login click: ${currentUrl}`);

    // Check for login form elements
    const emailInput = await page.locator('input[type="email"]').isVisible();
    const passwordInput = await page.locator('input[type="password"]').isVisible();
    const submitButton = await page.locator('button:has-text("Sign In")').isVisible();

    console.log(`🔍 Email input visible: ${emailInput}`);
    console.log(`🔍 Password input visible: ${passwordInput}`);
    console.log(`🔍 Submit button visible: ${submitButton}`);

    if (!emailInput) {
      console.log('❌ CRITICAL: Login form not rendering after LOGIN button click');
      await page.screenshot({ path: 'login-form-missing.png', fullPage: true });
    }
  }

  // Test direct navigation to dashboard (simulate direct URL access)
  console.log('🔍 Testing direct dashboard navigation...');
  await page.goto('http://localhost:5173/dashboard');
  await page.waitForTimeout(3000);

  const dashboardUrl = page.url();
  const dashboardContent = await page.locator('h1').textContent();
  console.log(`🔍 Dashboard URL: ${dashboardUrl}`);
  console.log(`🔍 Dashboard heading: ${dashboardContent || 'None found'}`);

  await page.screenshot({ path: 'direct-dashboard-navigation.png', fullPage: true });

  // Test admin events navigation
  console.log('🔍 Testing direct admin events navigation...');
  await page.goto('http://localhost:5173/admin/events');
  await page.waitForTimeout(3000);

  const adminEventsUrl = page.url();
  const adminEventsContent = await page.locator('h1').textContent();
  console.log(`🔍 Admin Events URL: ${adminEventsUrl}`);
  console.log(`🔍 Admin Events heading: ${adminEventsContent || 'None found'}`);

  await page.screenshot({ path: 'direct-admin-events-navigation.png', fullPage: true });

  // Wait a bit more for any async errors
  await page.waitForTimeout(5000);

  // Report all collected errors
  console.log('\n🚨 ERROR SUMMARY:');
  console.log(`❌ Console Errors: ${consoleErrors.length}`);
  consoleErrors.forEach(error => console.log(error));

  console.log(`❌ Page Errors: ${pageErrors.length}`);
  pageErrors.forEach(error => console.log(error));

  console.log(`❌ Failed API Requests: ${failedRequests.length}`);
  failedRequests.forEach(request => console.log(request));

  // Test API health directly
  console.log('\n🔍 Testing API health...');
  const apiHealthResponse = await page.request.get('http://localhost:5655/health').catch(err => {
    console.log(`❌ API Health Request Failed: ${err.message}`);
    return null;
  });

  if (apiHealthResponse) {
    console.log(`🔍 API Health Status: ${apiHealthResponse.status()}`);
    if (apiHealthResponse.ok()) {
      const healthData = await apiHealthResponse.text();
      console.log(`✅ API Health Response: ${healthData}`);
    }
  } else {
    console.log('❌ CRITICAL: Cannot connect to API at http://localhost:5655');
  }

  // Final summary
  console.log('\n📊 INVESTIGATION SUMMARY:');
  console.log(`- React App Loading: ${rootContent.length > 0 ? '✅ SUCCESS' : '❌ FAILED'}`);
  console.log(`- Events Loading: ${loadingSpinner ? '❌ STUCK' : '✅ OK'}`);
  console.log(`- Login Button: ${loginExists ? '✅ VISIBLE' : '❌ MISSING'}`);
  console.log(`- API Connectivity: ${apiHealthResponse?.ok() ? '✅ OK' : '❌ FAILED'}`);
  console.log(`- Console Errors: ${consoleErrors.length}`);
  console.log(`- API Errors: ${failedRequests.length}`);
});