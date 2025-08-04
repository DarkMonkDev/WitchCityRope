const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing Authentication State Propagation ===\n');
    
    // 1. Check initial state
    console.log('1. Checking initial auth state...');
    await page.goto('http://localhost:5651/test-auth-state');
    await page.waitForTimeout(2000);
    
    const notAuthText = await page.locator('text=You are NOT authenticated').isVisible();
    console.log('Initial state - Not authenticated:', notAuthText);
    
    // 2. Login via Razor Page
    console.log('\n2. Logging in via Razor Page...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for redirect
    await page.waitForTimeout(3000);
    console.log('After login URL:', page.url());
    
    // 3. Check auth state page
    console.log('\n3. Checking auth state after login...');
    await page.goto('http://localhost:5651/test-auth-state');
    await page.waitForTimeout(2000);
    
    const authText = await page.locator('text=You are authenticated').isVisible();
    console.log('After login - Authenticated:', authText);
    
    if (authText) {
      const userName = await page.locator('p:has-text("User:")').textContent();
      console.log(userName);
      
      const authType = await page.locator('p:has-text("Authentication Type:")').textContent();
      console.log(authType);
    }
    
    // 4. Check navigation menu
    console.log('\n4. Checking navigation menu...');
    await page.goto('http://localhost:5651/');
    await page.waitForTimeout(2000);
    
    const myDashboardLink = await page.locator('a:has-text("My Dashboard")').isVisible();
    console.log('My Dashboard link visible:', myDashboardLink);
    
    const loginButton = await page.locator('a.btn-primary:has-text("Login")').isVisible();
    console.log('Login button visible:', loginButton);
    
    // Take screenshot
    await page.screenshot({ path: 'nav-after-login.png' });
    
    // 5. Check member dashboard access
    console.log('\n5. Testing member dashboard access...');
    if (myDashboardLink) {
      await page.click('a:has-text("My Dashboard")');
      await page.waitForTimeout(2000);
      
      const dashboardUrl = page.url();
      console.log('Dashboard URL:', dashboardUrl);
      
      if (dashboardUrl.includes('/member/dashboard')) {
        console.log('✅ Successfully accessed member dashboard');
      }
    }
    
    console.log('\n=== Summary ===');
    console.log('Authentication state propagation:', authText ? '✅ WORKING' : '❌ NOT WORKING');
    console.log('Navigation update:', myDashboardLink && !loginButton ? '✅ WORKING' : '❌ NOT WORKING');
    
  } catch (error) {
    console.log('❌ Test error:', error.message);
  } finally {
    await page.waitForTimeout(3000);
    await browser.close();
  }
})();