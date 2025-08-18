import { test, expect } from '@playwright/test';

/**
 * DEBUG test to understand why FormDesignA component isn't rendering
 */

test('DEBUG: Form Design A rendering investigation', async ({ page }) => {
  console.log('🔍 DEBUG: Starting investigation...');

  // Enable console logging
  page.on('console', msg => {
    console.log(`Browser Console [${msg.type()}]:`, msg.text());
  });

  // Enable network monitoring
  page.on('request', request => {
    console.log(`📤 Request: ${request.method()} ${request.url()}`);
  });

  page.on('response', response => {
    console.log(`📥 Response: ${response.status()} ${response.url()}`);
  });

  page.on('requestfailed', request => {
    console.log(`❌ Request Failed: ${request.method()} ${request.url()} - ${request.failure()?.errorText}`);
  });

  // Navigate to homepage first to check if React is working
  console.log('🏠 Testing homepage first...');
  await page.goto('/', { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  
  const homePageText = await page.locator('body').innerText();
  console.log('Homepage body text:', homePageText.substring(0, 200));
  
  // Take screenshot of homepage
  await page.screenshot({ path: 'test-results/debug-homepage.png', fullPage: true });

  // Now navigate to form design page
  console.log('🎨 Testing form design page...');
  await page.goto('/form-designs/a', { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  
  // Wait a bit more for potential React hydration
  await page.waitForTimeout(5000);
  
  const formPageText = await page.locator('body').innerText();
  console.log('Form design page body text:', formPageText.substring(0, 200));
  
  // Check for specific React elements
  const reactRoot = await page.locator('#root').innerHTML();
  console.log('React root content (first 300 chars):', reactRoot.substring(0, 300));
  
  // Take screenshot
  await page.screenshot({ path: 'test-results/debug-form-design-a.png', fullPage: true });
  
  // Check for JavaScript errors in the page
  const jsErrors = await page.evaluate(() => {
    return window.console && window.console.error ? 'Console available' : 'No console';
  });
  console.log('JavaScript console status:', jsErrors);
  
  // Check if React is loaded
  const reactLoaded = await page.evaluate(() => {
    return typeof window.React !== 'undefined' || typeof window.__REACT_DEVTOOLS_GLOBAL_HOOK__ !== 'undefined';
  });
  console.log('React detected:', reactLoaded);
  
  // Check if the route handler is working by looking at browser URL
  const currentUrl = page.url();
  console.log('Current URL:', currentUrl);
  
  // Check network tab for any 404s or errors
  console.log('✅ Investigation complete - check console output above');
});