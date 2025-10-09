import { test, expect } from '@playwright/test';

/**
 * Test to verify VettingStatus enum fix
 *
 * Context: React-developer switched from manual numeric enums to auto-generated
 * string union types from @witchcityrope/shared-types package
 *
 * This test verifies:
 * 1. Admin user can log in successfully
 * 2. Vetting status displays correctly (not showing "Denied")
 * 3. Admin dashboard is accessible
 */

test.describe('VettingStatus Enum Fix Verification', () => {
  test('should display correct vetting status for admin user', async ({ page }) => {
    // Navigate to the application
    await page.goto('http://localhost:5173');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Click LOGIN button in header (visible text)
    const loginButton = page.getByRole('button', { name: /login/i });
    await expect(loginButton).toBeVisible({ timeout: 10000 });
    await loginButton.click();

    // Wait for login form
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });

    // Fill in credentials
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');

    // Take screenshot before login
    await page.screenshot({ path: '/tmp/before-admin-login.png', fullPage: true });

    // Click sign in
    const signInButton = page.locator('button[type="submit"]').filter({ hasText: /sign in/i });
    await signInButton.click();

    // Wait for navigation to dashboard
    await page.waitForURL(/\/(dashboard|admin)/, { timeout: 15000 });

    // Take screenshot after login
    await page.screenshot({ path: '/tmp/after-admin-login.png', fullPage: true });

    // Check current URL
    const currentUrl = page.url();
    console.log('✅ Logged in successfully, current URL:', currentUrl);

    // Look for vetting status display
    const pageContent = await page.content();

    // Check if "Denied" appears (it shouldn't for admin with VettingStatus=4)
    const hasDenied = pageContent.toLowerCase().includes('denied');
    const hasApproved = pageContent.toLowerCase().includes('approved') ||
                        pageContent.toLowerCase().includes('vetted') ||
                        pageContent.toLowerCase().includes('administrator');

    console.log('🔍 Page content check:');
    console.log('  - Contains "Denied":', hasDenied);
    console.log('  - Contains positive status:', hasApproved);

    // Verify we're on dashboard
    expect(currentUrl).toMatch(/\/(dashboard|admin)/);

    // Admin user should NOT see "Denied" status
    if (hasDenied) {
      console.error('❌ ERROR: Admin user showing "Denied" vetting status!');
      await page.screenshot({ path: '/tmp/vetting-status-error.png', fullPage: true });
    } else {
      console.log('✅ SUCCESS: No "Denied" status displayed for admin user');
    }

    expect(hasDenied).toBe(false);
  });

  test('should make successful API call to get user info', async ({ page }) => {
    // Navigate to the application
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');

    // Login
    const loginButton = page.getByRole('button', { name: /login/i });
    await loginButton.click();
    await page.waitForSelector('[data-testid="email-input"]');
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');

    // Set up API response monitoring
    const apiResponses: any[] = [];
    page.on('response', async (response) => {
      if (response.url().includes('/api/')) {
        try {
          const responseData = {
            url: response.url(),
            status: response.status(),
            statusText: response.statusText(),
          };

          // Try to get JSON body for user-related endpoints
          if (response.url().includes('/user') || response.url().includes('/auth')) {
            try {
              const body = await response.json();
              responseData['body'] = body;
            } catch (e) {
              // Not JSON or already consumed
            }
          }

          apiResponses.push(responseData);
        } catch (e) {
          // Ignore errors in response monitoring
        }
      }
    });

    // Submit login
    const signInButton = page.locator('button[type="submit"]').filter({ hasText: /sign in/i });
    await signInButton.click();

    // Wait for navigation
    await page.waitForURL(/\/(dashboard|admin)/, { timeout: 15000 });

    // Give API calls time to complete
    await page.waitForTimeout(2000);

    // Log API responses
    console.log('\n📡 API Responses:');
    apiResponses.forEach(resp => {
      console.log(`  ${resp.status} ${resp.url}`);
      if (resp.body) {
        console.log('    Body:', JSON.stringify(resp.body, null, 2));
      }
    });

    // Check for successful auth API calls
    const authApiCalls = apiResponses.filter(r =>
      r.url.includes('/auth') || r.url.includes('/user')
    );

    const successfulAuthCalls = authApiCalls.filter(r => r.status === 200);

    console.log(`\n✅ Successful auth API calls: ${successfulAuthCalls.length}/${authApiCalls.length}`);

    // Verify at least one successful auth call
    expect(successfulAuthCalls.length).toBeGreaterThan(0);
  });
});
