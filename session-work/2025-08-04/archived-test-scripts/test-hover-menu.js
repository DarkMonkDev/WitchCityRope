const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing User Menu Hover ===\n');
    
    // 1. Login
    console.log('1. Logging in...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
    
    // 2. Go to home page
    console.log('\n2. Navigating to home page...');
    await page.goto('http://localhost:5651/');
    await page.waitForTimeout(2000);
    
    // 3. Hover over user menu
    console.log('\n3. Hovering over user menu...');
    await page.hover('.user-menu');
    await page.waitForTimeout(1000);
    
    // Check if dropdown is visible
    const dropdownVisible = await page.locator('.user-dropdown').evaluate(el => {
      const style = window.getComputedStyle(el);
      return style.visibility === 'visible' && style.opacity === '1';
    });
    console.log('Dropdown visible on hover:', dropdownVisible);
    
    // Take screenshot
    await page.screenshot({ path: 'user-menu-hover.png' });
    
    // Try to click a menu item
    if (dropdownVisible) {
      console.log('\n4. Trying to click My Profile...');
      await page.click('a:has-text("My Profile")');
      await page.waitForTimeout(2000);
      console.log('Current URL:', page.url());
    }
    
    console.log('\n✅ Screenshot saved as user-menu-hover.png');
    console.log('User menu functionality:', dropdownVisible ? 'WORKING' : 'NOT WORKING');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();