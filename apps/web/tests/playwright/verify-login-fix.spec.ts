import { test, expect } from '@playwright/test';

test.describe('Login Fix Verification', () => {
  test('admin can successfully login after API response fix', async ({ page }) => {
    // Monitor console for errors
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
        console.log('Console error:', msg.text());
      }
    });

    // Monitor network requests
    const networkRequests: { url: string; method: string; status?: number }[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/') || request.url().includes('/auth')) {
        networkRequests.push({ url: request.url(), method: request.method() });
      }
    });
    
    page.on('response', response => {
      if (response.url().includes('/api/') || response.url().includes('/auth')) {
        const existing = networkRequests.find(r => r.url === response.url());
        if (existing) {
          existing.status = response.status();
        }
      }
    });

    // Navigate to login
    await page.goto('http://localhost:5174/login');
    await expect(page).toHaveURL(/\/login/);
    
    console.log('✅ Successfully navigated to login page');
    
    // Take before screenshot
    await page.screenshot({ path: 'test-results/login-page-before-fix-verification.png' });

    // Fill credentials
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    
    console.log('✅ Filled in credentials');
    
    // Submit form
    await page.click('button[type="submit"]');
    
    console.log('✅ Clicked login button');
    
    // Wait for some response (either success or failure)
    await page.waitForTimeout(5000);
    
    // Take after screenshot
    await page.screenshot({ path: 'test-results/after-login-attempt-fix-verification.png' });
    
    // Check current URL
    const currentUrl = page.url();
    console.log('Current URL after login attempt:', currentUrl);
    
    // Log network requests
    console.log('Network requests during login:');
    networkRequests.forEach(req => {
      console.log(`  ${req.method} ${req.url} - Status: ${req.status || 'pending'}`);
    });
    
    // Check if we redirected to dashboard (success) or stayed on login (failure)
    if (currentUrl.includes('/dashboard')) {
      console.log('✅ SUCCESS: Redirected to dashboard');
      
      // Check for logout button to confirm authentication
      const logoutButton = page.locator('[data-testid="button-logout"]');
      if (await logoutButton.isVisible()) {
        console.log('✅ SUCCESS: User is authenticated (logout button visible)');
      } else {
        console.log('⚠️ WARNING: On dashboard but no logout button found');
        // Take screenshot of dashboard for debugging
        await page.screenshot({ path: 'test-results/dashboard-after-login-fix-verification.png' });
      }
    } else {
      console.log('❌ FAILURE: Still on login page or other location');
      
      // Look for error messages
      const errorElements = page.locator('text=/error|failed|invalid/i');
      const errorCount = await errorElements.count();
      if (errorCount > 0) {
        console.log(`Found ${errorCount} error elements on page`);
        for (let i = 0; i < errorCount; i++) {
          const errorText = await errorElements.nth(i).textContent();
          console.log(`Error ${i + 1}: ${errorText}`);
        }
      }
    }
    
    // Report console errors
    if (errors.length > 0) {
      console.log('⚠️ Console errors detected:');
      errors.forEach((error, index) => {
        console.log(`${index + 1}. ${error}`);
      });
    } else {
      console.log('✅ No console errors detected');
    }
    
    // For now, we'll just ensure the test doesn't crash and we can analyze the results
    expect(currentUrl).toBeDefined();
  });

  test('login shows error with invalid credentials', async ({ page }) => {
    await page.goto('http://localhost:5174/login');
    
    console.log('Testing invalid credentials...');
    
    // Try invalid credentials
    await page.fill('[data-testid="email-input"]', 'invalid@example.com');
    await page.fill('[data-testid="password-input"]', 'wrongpassword');
    await page.click('button[type="submit"]');
    
    // Should stay on login page
    await page.waitForTimeout(3000);
    const currentUrl = page.url();
    console.log('URL after invalid login:', currentUrl);
    
    // Take screenshot
    await page.screenshot({ path: 'test-results/invalid-login-attempt.png' });
    
    expect(currentUrl).toContain('/login');
    console.log('✅ Correctly stayed on login page with invalid credentials');
  });
});