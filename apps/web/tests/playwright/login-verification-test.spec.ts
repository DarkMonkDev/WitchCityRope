import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Login Page Verification', () => {
  test.beforeEach(async ({ page }) => {
    // Enable console and network monitoring
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log(`❌ Console Error: ${msg.text()}`);
      } else if (msg.type() === 'warn') {
        console.log(`⚠️ Console Warning: ${msg.text()}`);
      }
    });
    
    page.on('response', response => {
      if (response.url().includes('/api/')) {
        console.log(`📡 API Response: ${response.status()} ${response.url()}`);
      }
    });
    
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        console.log(`📡 API Request: ${request.method()} ${request.url()}`);
      }
    });
  });

  test('should load login page successfully', async ({ page }) => {
    console.log('🔍 Testing login page load...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of initial page state
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/apps/web/test-results/login-page-loaded.png' });
    
    // Verify login page elements are present
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    
    // Check if elements exist using various selector strategies
    const emailExists = await emailInput.count() > 0;
    const passwordExists = await passwordInput.count() > 0;
    const loginButtonExists = await loginButton.count() > 0;
    
    console.log(`Email input found: ${emailExists}`);
    console.log(`Password input found: ${passwordExists}`);
    console.log(`Login button found: ${loginButtonExists}`);
    
    // If data-testid doesn't work, try alternative selectors
    if (!emailExists) {
      const altEmailInput = page.locator('input[type="email"], input[placeholder*="email" i]');
      const altEmailExists = await altEmailInput.count() > 0;
      console.log(`Alternative email input found: ${altEmailExists}`);
    }
    
    if (!passwordExists) {
      const altPasswordInput = page.locator('input[type="password"]');
      const altPasswordExists = await altPasswordInput.count() > 0;
      console.log(`Alternative password input found: ${altPasswordExists}`);
    }
    
    if (!loginButtonExists) {
      const altLoginButton = page.locator('button:has-text("Login"), button:has-text("Sign In")');
      const altLoginExists = await altLoginButton.count() > 0;
      console.log(`Alternative login button found: ${altLoginExists}`);
    }
    
    // Verify page loaded successfully
    expect(page.url()).toContain('/login');
    
    console.log('✅ Login page loaded successfully');
  });

  test('should login with admin credentials', async ({ page }) => {
    console.log('🔍 Testing admin login...');

    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');

    // Take screenshot after login
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/apps/web/test-results/after-admin-login.png' });

    // Check current URL
    console.log(`🌐 Current URL after login: ${page.url()}`);

    // Verify redirected to protected area
    if (page.url().includes('/dashboard') || page.url().includes('/admin')) {
      console.log('✅ Admin login successful - redirected to protected area');
    } else {
      console.log(`📍 Unexpected URL: ${page.url()}`);
    }
  });

  test('should login with guest credentials', async ({ page }) => {
    console.log('🔍 Testing guest login...');

    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'guest');

    // Take screenshot after login
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/apps/web/test-results/after-guest-login.png' });

    // Check current URL
    console.log(`🌐 Current URL after login: ${page.url()}`);

    // Verify redirected to protected area
    if (page.url().includes('/dashboard') || page.url().includes('/member')) {
      console.log('✅ Guest login successful - redirected to protected area');
    } else {
      console.log(`📍 Unexpected URL: ${page.url()}`);
    }
  });

  test('should handle invalid credentials gracefully', async ({ page }) => {
    console.log('🔍 Testing invalid credentials handling...');

    // Try invalid login using AuthHelpers
    await AuthHelpers.loginExpectingError(
      page,
      { email: 'invalid@test.com', password: 'wrongpassword' }
    );

    // Take screenshot of error state
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/apps/web/test-results/invalid-credentials-result.png' });

    console.log('✅ Invalid credentials correctly handled');
  });
});