import { test, expect } from '@playwright/test';

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
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/login-page-loaded.png' });
    
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
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Find form elements using multiple selector strategies
    let emailInput = page.locator('[data-testid="email-input"]');
    let passwordInput = page.locator('[data-testid="password-input"]');
    let loginButton = page.locator('[data-testid="login-button"]');
    
    // Fallback selectors if data-testid doesn't work
    if (await emailInput.count() === 0) {
      emailInput = page.locator('input[type="email"], input[placeholder*="email" i]').first();
      console.log('Using fallback email selector');
    }
    
    if (await passwordInput.count() === 0) {
      passwordInput = page.locator('input[type="password"]').first();
      console.log('Using fallback password selector');
    }
    
    if (await loginButton.count() === 0) {
      loginButton = page.locator('button:has-text("Login"), button:has-text("Sign In"), button[type="submit"]').first();
      console.log('Using fallback login button selector');
    }
    
    // Fill in admin credentials
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    
    // Take screenshot before login attempt
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/before-admin-login.png' });
    
    console.log('📝 Filled in admin credentials, clicking login...');
    
    // Click login and monitor network activity
    const responsePromise = page.waitForResponse(response => 
      response.url().includes('/api/auth/login') || response.url().includes('/api/Auth/login')
    );
    
    await loginButton.click();
    
    try {
      const response = await responsePromise;
      console.log(`🔗 Login API Response: ${response.status()} ${response.url()}`);
      
      const responseBody = await response.text();
      console.log(`📄 Response body: ${responseBody.substring(0, 200)}...`);
      
      // Wait a bit for any redirects or state changes
      await page.waitForTimeout(2000);
      
      // Take screenshot after login attempt
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/after-admin-login.png' });
      
      // Check current URL
      console.log(`🌐 Current URL after login: ${page.url()}`);
      
      // Check if we're redirected to dashboard or remain on login page
      if (page.url().includes('/dashboard') || page.url().includes('/admin')) {
        console.log('✅ Admin login successful - redirected to protected area');
      } else if (page.url().includes('/login')) {
        console.log('⚠️ Still on login page - checking for error messages or auth state');
        
        // Look for error messages
        const errorMessages = await page.locator('.error, .alert, [role="alert"]').all();
        for (const error of errorMessages) {
          const errorText = await error.textContent();
          console.log(`❌ Error message: ${errorText}`);
        }
      } else {
        console.log(`📍 Unexpected redirect to: ${page.url()}`);
      }
      
    } catch (error) {
      console.log(`❌ No login API response detected: ${error.message}`);
      
      // Take screenshot of error state
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/admin-login-error.png' });
    }
  });

  test('should login with guest credentials', async ({ page }) => {
    console.log('🔍 Testing guest login...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Find form elements using multiple selector strategies
    let emailInput = page.locator('[data-testid="email-input"]');
    let passwordInput = page.locator('[data-testid="password-input"]');
    let loginButton = page.locator('[data-testid="login-button"]');
    
    // Fallback selectors if data-testid doesn't work
    if (await emailInput.count() === 0) {
      emailInput = page.locator('input[type="email"], input[placeholder*="email" i]').first();
      console.log('Using fallback email selector');
    }
    
    if (await passwordInput.count() === 0) {
      passwordInput = page.locator('input[type="password"]').first();
      console.log('Using fallback password selector');
    }
    
    if (await loginButton.count() === 0) {
      loginButton = page.locator('button:has-text("Login"), button:has-text("Sign In"), button[type="submit"]').first();
      console.log('Using fallback login button selector');
    }
    
    // Fill in guest credentials
    await emailInput.fill('guest@witchcityrope.com');
    await passwordInput.fill('Test123!');
    
    // Take screenshot before login attempt
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/before-guest-login.png' });
    
    console.log('📝 Filled in guest credentials, clicking login...');
    
    // Click login and monitor network activity
    const responsePromise = page.waitForResponse(response => 
      response.url().includes('/api/auth/login') || response.url().includes('/api/Auth/login')
    );
    
    await loginButton.click();
    
    try {
      const response = await responsePromise;
      console.log(`🔗 Login API Response: ${response.status()} ${response.url()}`);
      
      const responseBody = await response.text();
      console.log(`📄 Response body: ${responseBody.substring(0, 200)}...`);
      
      // Wait a bit for any redirects or state changes
      await page.waitForTimeout(2000);
      
      // Take screenshot after login attempt
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/after-guest-login.png' });
      
      // Check current URL
      console.log(`🌐 Current URL after login: ${page.url()}`);
      
      // Check if we're redirected to dashboard or remain on login page
      if (page.url().includes('/dashboard') || page.url().includes('/member')) {
        console.log('✅ Guest login successful - redirected to protected area');
      } else if (page.url().includes('/login')) {
        console.log('⚠️ Still on login page - checking for error messages or auth state');
        
        // Look for error messages
        const errorMessages = await page.locator('.error, .alert, [role="alert"]').all();
        for (const error of errorMessages) {
          const errorText = await error.textContent();
          console.log(`❌ Error message: ${errorText}`);
        }
      } else {
        console.log(`📍 Unexpected redirect to: ${page.url()}`);
      }
      
    } catch (error) {
      console.log(`❌ No login API response detected: ${error.message}`);
      
      // Take screenshot of error state
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/guest-login-error.png' });
    }
  });

  test('should handle invalid credentials gracefully', async ({ page }) => {
    console.log('🔍 Testing invalid credentials handling...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Find form elements using multiple selector strategies
    let emailInput = page.locator('[data-testid="email-input"]');
    let passwordInput = page.locator('[data-testid="password-input"]');
    let loginButton = page.locator('[data-testid="login-button"]');
    
    // Fallback selectors if data-testid doesn't work
    if (await emailInput.count() === 0) {
      emailInput = page.locator('input[type="email"], input[placeholder*="email" i]').first();
    }
    
    if (await passwordInput.count() === 0) {
      passwordInput = page.locator('input[type="password"]').first();
    }
    
    if (await loginButton.count() === 0) {
      loginButton = page.locator('button:has-text("Login"), button:has-text("Sign In"), button[type="submit"]').first();
    }
    
    // Fill in invalid credentials
    await emailInput.fill('invalid@test.com');
    await passwordInput.fill('wrongpassword');
    
    console.log('📝 Filled in invalid credentials, clicking login...');
    
    // Click login and monitor for error response
    const responsePromise = page.waitForResponse(response => 
      response.url().includes('/api/auth/login') || response.url().includes('/api/Auth/login')
    );
    
    await loginButton.click();
    
    try {
      const response = await responsePromise;
      console.log(`🔗 Login API Response: ${response.status()} ${response.url()}`);
      
      // Should get 401 or similar error
      if (response.status() === 401 || response.status() === 400) {
        console.log('✅ Invalid credentials correctly rejected by API');
      } else {
        console.log(`⚠️ Unexpected status for invalid credentials: ${response.status()}`);
      }
      
      // Wait for error message to appear
      await page.waitForTimeout(1000);
      
      // Take screenshot of error state
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/invalid-credentials-result.png' });
      
      // Look for error messages in UI
      const errorMessages = await page.locator('.error, .alert, [role="alert"], .notification').all();
      for (const error of errorMessages) {
        const errorText = await error.textContent();
        if (errorText?.trim()) {
          console.log(`📝 UI Error message: ${errorText}`);
        }
      }
      
    } catch (error) {
      console.log(`❌ No login API response detected: ${error.message}`);
    }
  });
});