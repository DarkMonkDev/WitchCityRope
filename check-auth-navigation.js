const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Checking Authentication Navigation State ===\n');
    
    // 1. Go to home page
    console.log('1. Navigating to home page...');
    await page.goto('http://localhost:5651/', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    
    // Check initial state in nav
    const loginLinkCount = await page.locator('nav a:has-text("Login")').count();
    const dashboardLinkCount = await page.locator('nav a:has-text("My Dashboard")').count();
    console.log(`Nav login links: ${loginLinkCount}`);
    console.log(`Nav dashboard links: ${dashboardLinkCount}`);
    
    // 2. Login
    console.log('\n2. Logging in...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
    
    // 3. Check nav after login
    console.log('\n3. Checking nav after login...');
    await page.goto('http://localhost:5651/', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    
    const loginLinkCountAfter = await page.locator('nav a:has-text("Login")').count();
    const dashboardLinkCountAfter = await page.locator('nav a:has-text("My Dashboard")').count();
    console.log(`Nav login links after login: ${loginLinkCountAfter}`);
    console.log(`Nav dashboard links after login: ${dashboardLinkCountAfter}`);
    
    // Take screenshot
    await page.screenshot({ path: 'nav-auth-state.png' });
    
    console.log('\n✅ Screenshot saved as nav-auth-state.png');
    console.log('\nAuth state in nav: ' + (dashboardLinkCountAfter > 0 ? 'WORKING' : 'NOT UPDATING'));
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();