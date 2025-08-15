const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing Dashboard Access After Login ===\n');
    
    // 1. Login
    console.log('1. Logging in...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123\!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
    
    // 2. Go to home and click dashboard
    console.log('\n2. Navigating to dashboard...');
    await page.goto('http://localhost:5651/');
    await page.waitForTimeout(2000);
    
    // Click first "My Dashboard" link
    await page.locator('a:has-text("My Dashboard")').first().click();
    await page.waitForTimeout(2000);
    
    // Check URL
    const currentUrl = page.url();
    console.log('Current URL:', currentUrl);
    
    // Check for dashboard content
    const pageTitle = await page.locator('h1, h2, h3').first().textContent();
    console.log('Page title:', pageTitle);
    
    // Take screenshot
    await page.screenshot({ path: 'dashboard-access.png' });
    
    console.log('\n✅ Screenshot saved as dashboard-access.png');
    console.log('Dashboard access:', currentUrl.includes('dashboard') || currentUrl.includes('member') ? 'WORKING' : 'NOT WORKING');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();
