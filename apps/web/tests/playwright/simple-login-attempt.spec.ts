import { test, expect } from '@playwright/test';

/**
 * Simple login attempt test - bypasses problematic helpers
 * to see what actually happens during login
 */
test.describe('Simple Login Attempt', () => {
  test('should attempt login and capture results', async ({ page }) => {
    // Enable console monitoring
    page.on('console', msg => {
      console.log(`Console: ${msg.text()}`);
    });

    // Monitor network requests
    page.on('request', request => {
      if (request.url().includes('/api/auth')) {
        console.log(`🌐 Auth Request: ${request.method()} ${request.url()}`);
      }
    });

    page.on('response', response => {
      if (response.url().includes('/api/auth')) {
        console.log(`🌐 Auth Response: ${response.status()} ${response.url()}`);
      }
    });

    // Clear cookies first
    await page.context().clearCookies();
    
    // Navigate to login page
    await page.goto('http://localhost:5174/login');
    await page.waitForLoadState('domcontentloaded');
    
    // Fill form with admin credentials
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    
    // Take screenshot before submission
    await page.screenshot({ path: 'test-results/before-login-submission.png' });
    
    console.log('🔄 Submitting login form...');
    
    // Click login button and wait for any navigation
    const navigationPromise = page.waitForURL('**', { timeout: 10000 }).catch(() => null);
    await page.locator('[data-testid="login-button"]').click();
    
    // Wait a moment for any response
    await page.waitForTimeout(2000);
    
    const currentUrl = page.url();
    console.log(`📍 Current URL after login attempt: ${currentUrl}`);
    
    // Take screenshot after submission
    await page.screenshot({ path: 'test-results/after-login-submission.png' });
    
    // Check for error messages
    const errorElement = page.locator('[data-testid="login-error"]');
    const hasError = await errorElement.count() > 0;
    
    if (hasError) {
      const errorText = await errorElement.textContent();
      console.log(`❌ Login error: ${errorText}`);
    } else {
      console.log('✅ No error message visible');
    }
    
    // Check if we're still on login page or redirected
    if (currentUrl.includes('/login')) {
      console.log('📍 Still on login page');
      
      // Look for any loading indicators
      const loadingElement = page.locator('text=loading', { timeout: 1000 }).or(
        page.locator('[data-testid*="loading"]')
      );
      const isLoading = await loadingElement.count() > 0;
      console.log(`⏳ Loading indicator present: ${isLoading}`);
    } else {
      console.log(`📍 Redirected to: ${currentUrl}`);
    }
    
    // Get page content to understand what's shown
    const pageTitle = await page.locator('h1').textContent().catch(() => 'No h1 found');
    console.log(`📄 Page title: ${pageTitle}`);
  });

  test('should test API directly', async ({ page }) => {
    // Test the login API directly
    const response = await page.request.post('http://localhost:5653/api/auth/login', {
      data: {
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      }
    });

    console.log(`🔌 Direct API login response: ${response.status()}`);
    
    if (response.ok()) {
      const data = await response.json();
      console.log('✅ API login successful');
      console.log(`📝 Response keys: ${Object.keys(data)}`);
    } else {
      const errorText = await response.text();
      console.log(`❌ API login failed: ${errorText}`);
    }
  });
});