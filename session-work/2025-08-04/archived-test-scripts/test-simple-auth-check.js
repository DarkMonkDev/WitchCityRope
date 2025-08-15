const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Simple Authentication Check ===\n');
    
    // Wait for app to start
    console.log('Waiting for app to start...');
    await page.waitForTimeout(5000);
    
    // 1. Go to home page
    console.log('1. Checking home page...');
    await page.goto('http://localhost:5651/', { waitUntil: 'domcontentloaded' });
    
    // Check for login button
    const loginVisible = await page.locator('a.btn-primary:has-text("Login")').isVisible();
    console.log('Login button visible:', loginVisible);
    
    // 2. Login
    console.log('\n2. Performing login...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for redirect
    await page.waitForTimeout(3000);
    console.log('After login URL:', page.url());
    
    // 3. Go back to home page and check state
    console.log('\n3. Checking home page after login...');
    await page.goto('http://localhost:5651/', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    
    // Look for user indicators
    const myDashboard = await page.locator('text=My Dashboard').isVisible();
    const userMenu = await page.locator('.user-menu').count();
    const loginButton = await page.locator('a.btn-primary:has-text("Login")').isVisible();
    
    console.log('\nResults:');
    console.log('- My Dashboard link:', myDashboard);
    console.log('- User menu count:', userMenu);
    console.log('- Login button still visible:', loginButton);
    
    // Take screenshot
    await page.screenshot({ path: 'home-after-login-check.png' });
    
    console.log('\n✅ Screenshot saved as home-after-login-check.png');
    console.log('\nAuthentication state:', myDashboard || userMenu > 0 ? 'WORKING' : 'NOT UPDATING');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(3000);
    await browser.close();
  }
})();