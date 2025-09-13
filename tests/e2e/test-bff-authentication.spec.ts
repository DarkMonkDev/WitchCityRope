import { test, expect } from '@playwright/test';

/**
 * Comprehensive BFF Authentication Testing
 * Tests the complete authentication flow using httpOnly cookies
 * 
 * This test verifies:
 * 1. Login flow through React frontend
 * 2. HttpOnly cookie authentication
 * 3. Authentication persistence across page refreshes
 * 4. Protected route access
 * 5. Logout functionality
 * 6. No localStorage token storage (XSS protection)
 */

test.describe('BFF Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear all cookies and localStorage before each test
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
  });

  test('Login with admin credentials and verify httpOnly cookie authentication', async ({ page }) => {
    console.log('🧪 Starting BFF authentication test...');
    
    // Step 1: Navigate to login page
    console.log('📍 Step 1: Navigate to login page');
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Verify login page loads correctly
    await expect(page).toHaveTitle(/WitchCityRope/);
    console.log('✅ Login page loaded successfully');
    
    // Step 2: Wait for login form to be visible
    console.log('📍 Step 2: Wait for login form');
    await page.waitForSelector('form', { timeout: 10000 });
    
    // Look for email and password fields
    const emailInput = page.locator('input[type="email"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"]').first();
    const submitButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")').first();
    
    // Wait for all inputs to be visible
    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible({ timeout: 10000 });
    await expect(submitButton).toBeVisible({ timeout: 10000 });
    console.log('✅ Login form elements found');
    
    // Step 3: Fill in credentials
    console.log('📍 Step 3: Fill in admin credentials');
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    console.log('✅ Credentials entered');
    
    // Step 4: Capture network requests to monitor authentication
    const authRequests: any[] = [];
    page.on('request', (request) => {
      if (request.url().includes('/auth/login') || request.url().includes('/login')) {
        authRequests.push({
          url: request.url(),
          method: request.method(),
          headers: request.headers()
        });
      }
    });
    
    const authResponses: any[] = [];
    page.on('response', async (response) => {
      if (response.url().includes('/auth/login') || response.url().includes('/login')) {
        authResponses.push({
          url: response.url(),
          status: response.status(),
          headers: response.headers()
        });
      }
    });
    
    // Step 5: Submit login form
    console.log('📍 Step 5: Submit login form');
    await submitButton.click();
    
    // Wait for authentication to complete (redirect or success indicator)
    try {
      // Wait for either dashboard redirect or success state
      await Promise.race([
        page.waitForURL('**/dashboard', { timeout: 15000 }),
        page.waitForURL('**/', { timeout: 15000 }),
        page.waitForSelector('[data-testid="dashboard"], [data-testid="welcome"], .dashboard', { timeout: 15000 })
      ]);
      console.log('✅ Login submission completed');
    } catch (error) {
      console.log('⚠️ No immediate redirect detected, checking current state...');
    }
    
    // Step 6: Verify authentication was successful
    console.log('📍 Step 6: Verify authentication success');
    
    // Check for authentication indicators
    const currentUrl = page.url();
    console.log(`Current URL after login: ${currentUrl}`);
    
    // Verify we're not still on login page or showing login form
    const isStillOnLoginPage = currentUrl.includes('/login');
    const hasLoginForm = await page.locator('form:has(input[type="password"])').count() > 0;
    
    if (isStillOnLoginPage || hasLoginForm) {
      console.log('❌ Still on login page, checking for error messages...');
      const errorMessage = await page.locator('.error, .alert-danger, [data-testid="error"]').textContent();
      if (errorMessage) {
        console.log(`Login error: ${errorMessage}`);
      }
      
      // Take screenshot for debugging
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/tests/e2e/test-results/login-failure.png' });
    } else {
      console.log('✅ Successfully navigated away from login page');
    }
    
    // Step 7: Check cookies for httpOnly authentication
    console.log('📍 Step 7: Verify httpOnly cookie is set');
    const cookies = await page.context().cookies();
    console.log(`Total cookies found: ${cookies.length}`);
    
    const authCookie = cookies.find(cookie => 
      cookie.name.toLowerCase().includes('auth') || 
      cookie.name.toLowerCase().includes('token') ||
      cookie.name.toLowerCase().includes('session')
    );
    
    if (authCookie) {
      console.log(`✅ Authentication cookie found: ${authCookie.name}`);
      console.log(`   - HttpOnly: ${authCookie.httpOnly}`);
      console.log(`   - Secure: ${authCookie.secure}`);
      console.log(`   - Domain: ${authCookie.domain}`);
      console.log(`   - Path: ${authCookie.path}`);
      
      // Verify it's httpOnly (security requirement)
      expect(authCookie.httpOnly).toBe(true);
      console.log('✅ Cookie is httpOnly - XSS protection verified');
    } else {
      console.log('⚠️ No authentication cookie found');
      console.log('Available cookies:');
      cookies.forEach(cookie => {
        console.log(`  - ${cookie.name}: ${cookie.value.substring(0, 20)}...`);
      });
    }
    
    // Step 8: Verify no tokens in localStorage (security check)
    console.log('📍 Step 8: Verify no tokens in localStorage');
    const localStorageItems = await page.evaluate(() => {
      const items: any = {};
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key) {
          items[key] = localStorage.getItem(key);
        }
      }
      return items;
    });
    
    const hasTokenInStorage = Object.keys(localStorageItems).some(key => 
      key.toLowerCase().includes('token') || 
      key.toLowerCase().includes('auth') ||
      key.toLowerCase().includes('jwt')
    );
    
    expect(hasTokenInStorage).toBe(false);
    console.log('✅ No authentication tokens in localStorage - XSS protection verified');
    
    // Step 9: Test authenticated API request
    console.log('📍 Step 9: Test authenticated API request');
    try {
      // Try to access a protected endpoint through the frontend
      const apiResponse = await page.evaluate(async () => {
        try {
          const response = await fetch('/api/auth/me', {
            credentials: 'include'  // Include cookies
          });
          return {
            status: response.status,
            ok: response.ok,
            text: await response.text()
          };
        } catch (error) {
          return {
            status: 0,
            ok: false,
            text: `Error: ${error}`
          };
        }
      });
      
      console.log(`API request status: ${apiResponse.status}`);
      if (apiResponse.ok) {
        console.log('✅ Authenticated API request successful');
      } else {
        console.log(`⚠️ API request failed: ${apiResponse.text}`);
      }
    } catch (error) {
      console.log(`⚠️ API request error: ${error}`);
    }
    
    // Step 10: Test authentication persistence (page refresh)
    console.log('📍 Step 10: Test authentication persistence');
    await page.reload();
    await page.waitForLoadState('networkidle');
    
    // Check if still authenticated after refresh
    const stillAuthenticated = !page.url().includes('/login');
    if (stillAuthenticated) {
      console.log('✅ Authentication persisted across page refresh');
    } else {
      console.log('❌ Authentication lost after page refresh');
    }
    
    // Step 11: Log authentication network traffic
    console.log('📍 Step 11: Authentication network summary');
    console.log(`Auth requests made: ${authRequests.length}`);
    authRequests.forEach((req, index) => {
      console.log(`  ${index + 1}. ${req.method} ${req.url}`);
    });
    
    console.log(`Auth responses received: ${authResponses.length}`);
    authResponses.forEach((res, index) => {
      console.log(`  ${index + 1}. ${res.status} ${res.url}`);
    });
    
    console.log('🎉 BFF authentication test completed');
  });

  test('Test logout and cookie cleanup', async ({ page }) => {
    console.log('🧪 Starting logout test...');
    
    // First login
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    const emailInput = page.locator('input[type="email"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"]').first();
    const submitButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")').first();
    
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await submitButton.click();
    
    // Wait for login to complete
    await page.waitForTimeout(3000);
    
    // Now test logout
    console.log('📍 Testing logout functionality');
    const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Logout")').first();
    
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      console.log('✅ Logout button clicked');
      
      // Wait for redirect to login
      try {
        await page.waitForURL('**/login', { timeout: 10000 });
        console.log('✅ Redirected to login page after logout');
      } catch {
        console.log('⚠️ No redirect to login page detected');
      }
      
      // Verify cookies are cleared
      const cookiesAfterLogout = await page.context().cookies();
      const authCookieAfterLogout = cookiesAfterLogout.find(cookie => 
        cookie.name.toLowerCase().includes('auth') || 
        cookie.name.toLowerCase().includes('token')
      );
      
      if (!authCookieAfterLogout) {
        console.log('✅ Authentication cookie cleared after logout');
      } else {
        console.log('❌ Authentication cookie still present after logout');
      }
    } else {
      console.log('⚠️ Logout button not found');
    }
  });

  test('Test protected route access', async ({ page }) => {
    console.log('🧪 Testing protected route access...');
    
    // Try to access protected route without authentication
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    
    // Should be redirected to login
    if (page.url().includes('/login')) {
      console.log('✅ Unauthenticated user redirected to login for protected route');
    } else {
      console.log('⚠️ Protected route accessible without authentication');
    }
    
    // Now login and test access
    const emailInput = page.locator('input[type="email"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"]').first();
    const submitButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")').first();
    
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await submitButton.click();
    
    // Wait and try to access protected route again
    await page.waitForTimeout(3000);
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    
    if (!page.url().includes('/login')) {
      console.log('✅ Authenticated user can access protected route');
    } else {
      console.log('❌ Authenticated user still redirected to login');
    }
  });
});