import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * DEBUG TEST: Investigate authentication cookie persistence
 *
 * This test is designed to debug the 401 Unauthorized errors we're seeing
 * in E2E tests after successful login.
 *
 * Investigation: Are cookies being set? Are they persisting? Are they being sent?
 */

test.describe('Auth Cookie Debugging', () => {
  test('investigate cookie persistence after login', async ({ page }) => {
    console.log('=== STARTING COOKIE INVESTIGATION ===');

    // Step 1: Check cookies before login
    let cookies = await page.context().cookies();
    console.log(`[BEFORE LOGIN] Cookie count: ${cookies.length}`);
    console.log('[BEFORE LOGIN] Cookies:', JSON.stringify(cookies, null, 2));

    // Step 2: Perform login
    console.log('\n[LOGIN] Calling AuthHelpers.loginAs(page, "admin")...');
    await AuthHelpers.loginAs(page, 'admin');

    // Step 3: Check cookies immediately after login
    cookies = await page.context().cookies();
    console.log(`\n[AFTER LOGIN] Cookie count: ${cookies.length}`);
    console.log('[AFTER LOGIN] Cookies:', JSON.stringify(cookies, null, 2));

    // Look for auth-token specifically
    const authCookie = cookies.find(c => c.name === 'auth-token');
    if (authCookie) {
      console.log('\n✅ auth-token cookie FOUND!');
      console.log('  - Domain:', authCookie.domain);
      console.log('  - Path:', authCookie.path);
      console.log('  - Secure:', authCookie.secure);
      console.log('  - HttpOnly:', authCookie.httpOnly);
      console.log('  - SameSite:', authCookie.sameSite);
      console.log('  - Expires:', new Date(authCookie.expires * 1000).toISOString());
    } else {
      console.log('\n❌ auth-token cookie NOT FOUND!');
    }

    // Step 4: Navigate to events page
    console.log('\n[NAVIGATION] Navigating to /events...');
    await page.goto('/events');
    await page.waitForLoadState('networkidle');

    // Step 5: Check cookies after navigation
    cookies = await page.context().cookies();
    console.log(`\n[AFTER NAVIGATION] Cookie count: ${cookies.length}`);
    console.log('[AFTER NAVIGATION] Cookies:', JSON.stringify(cookies, null, 2));

    const authCookieAfterNav = cookies.find(c => c.name === 'auth-token');
    if (authCookieAfterNav) {
      console.log('\n✅ auth-token cookie STILL PRESENT after navigation!');
    } else {
      console.log('\n❌ auth-token cookie LOST after navigation!');
    }

    // Step 6: Check if we're authenticated by looking at the page
    const loginLink = page.locator('a:has-text("Login")');
    const loginLinkVisible = await loginLink.isVisible().catch(() => false);

    if (loginLinkVisible) {
      console.log('\n❌ Page shows "Login" link - USER NOT AUTHENTICATED');
    } else {
      console.log('\n✅ Page does not show "Login" link - might be authenticated');
    }

    // Step 7: Try making an API call to check if cookies are sent
    console.log('\n[API TEST] Making API call to /api/auth/user...');
    const response = await page.request.get('http://localhost:5655/api/auth/user');
    console.log('  - Status:', response.status());
    console.log('  - Status Text:', response.statusText());

    if (response.status() === 200) {
      const userData = await response.json();
      console.log('  - ✅ API call SUCCESS! User data:', JSON.stringify(userData, null, 2));
    } else {
      console.log('  - ❌ API call FAILED!');
      const errorText = await response.text();
      console.log('  - Error:', errorText.substring(0, 200));
    }

    console.log('\n=== INVESTIGATION COMPLETE ===');

    // This test always passes - it's just for debugging
    expect(true).toBe(true);
  });
});
