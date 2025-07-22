const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('1. Testing Login Functionality...');
    
    // Navigate to login page
    await page.goto('http://localhost:5651/login');
    console.log('✓ Login page loaded');
    
    // Fill in credentials
    await page.fill('input[type="email"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    console.log('✓ Credentials entered');
    
    // Click login button
    await page.click('button[type="submit"]');
    console.log('✓ Login button clicked');
    
    // Wait for navigation or error
    try {
      await page.waitForURL('**/dashboard', { timeout: 10000 });
      console.log('✅ LOGIN SUCCESS - Redirected to dashboard!');
      
      // Check if we can see authenticated content
      const userMenu = await page.locator('.user-menu').count();
      if (userMenu > 0) {
        console.log('✅ User menu found - authentication state confirmed');
      }
      
      // Try to access admin page
      console.log('\n2. Testing Admin Access...');
      await page.goto('http://localhost:5651/admin/dashboard');
      await page.waitForLoadState('networkidle');
      
      const currentUrl = page.url();
      if (currentUrl.includes('/admin/dashboard')) {
        console.log('✅ ADMIN ACCESS SUCCESS - Can access admin dashboard');
      } else {
        console.log(`❌ ADMIN ACCESS FAILED - Redirected to: ${currentUrl}`);
      }
      
    } catch (error) {
      console.log('❌ LOGIN FAILED - Did not redirect to dashboard');
      console.log('Current URL:', page.url());
      
      // Check for error messages
      const errorMessage = await page.locator('.validation-error, .alert-danger').first().textContent().catch(() => null);
      if (errorMessage) {
        console.log('Error message:', errorMessage);
      }
    }
    
    // Test logout
    console.log('\n3. Testing Logout...');
    try {
      await page.goto('http://localhost:5651/Identity/Account/Logout');
      await page.waitForLoadState('networkidle');
      console.log('✓ Logout page accessed');
      
      // Try to access admin page again (should fail)
      await page.goto('http://localhost:5651/admin/dashboard');
      await page.waitForLoadState('networkidle');
      
      const afterLogoutUrl = page.url();
      if (afterLogoutUrl.includes('/login')) {
        console.log('✅ LOGOUT SUCCESS - Redirected to login page');
      } else {
        console.log(`❌ LOGOUT FAILED - Still at: ${afterLogoutUrl}`);
      }
    } catch (error) {
      console.log('❌ Error during logout test:', error.message);
    }
    
    // Test registration
    console.log('\n4. Testing Registration...');
    await page.goto('http://localhost:5651/login');
    
    // Switch to register tab
    const registerTab = await page.locator('text=Create Account').first();
    if (await registerTab.isVisible()) {
      await registerTab.click();
      console.log('✓ Switched to register tab');
      
      // Fill registration form
      const testEmail = `test${Date.now()}@example.com`;
      await page.fill('input[name="Email"]', testEmail);
      await page.fill('input[name="SceneName"]', `TestUser${Date.now()}`);
      await page.fill('input[name="Password"]', 'Test123!@#');
      await page.fill('input[name="ConfirmPassword"]', 'Test123!@#');
      
      // Check terms checkboxes
      await page.check('input[type="checkbox"]', { timeout: 1000 }).catch(() => {});
      
      // Submit registration
      await page.click('button:has-text("Create Account")');
      console.log('✓ Registration form submitted');
      
      // Check result
      await page.waitForTimeout(3000);
      const registrationUrl = page.url();
      if (registrationUrl.includes('/dashboard') || registrationUrl.includes('/register')) {
        console.log('✅ REGISTRATION INITIATED - Redirected appropriately');
      } else {
        console.log(`Registration result - Current URL: ${registrationUrl}`);
      }
    }
    
  } catch (error) {
    console.log('❌ Test error:', error.message);
  } finally {
    await browser.close();
  }
})();