import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Simple login attempt test - uses AuthHelpers for reliable login
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

    // Login using AuthHelpers
    console.log('🔄 Logging in with AuthHelpers...');
    await AuthHelpers.loginAs(page, 'admin');

    // Take screenshot after login
    await page.screenshot({ path: 'test-results/after-login-submission.png' });

    const currentUrl = page.url();
    console.log(`📍 Current URL after login: ${currentUrl}`);
    
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
    const response = await page.request.post('http://localhost:5655/api/auth/login', {
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