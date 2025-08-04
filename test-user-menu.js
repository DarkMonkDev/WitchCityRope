const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing User Menu Display ===\n');
    
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
    
    // Check for user menu elements
    const userMenuButton = await page.locator('.user-menu-btn').count();
    const userMenuDropdown = await page.locator('.user-dropdown').count();
    const userSceneName = await page.locator('.user-menu-btn span').textContent().catch(() => null);
    
    console.log('\nUser menu elements:');
    console.log('- User menu button found:', userMenuButton > 0);
    console.log('- User dropdown found:', userMenuDropdown > 0);
    console.log('- Scene name displayed:', userSceneName || 'None');
    
    // Check what's in the nav
    const navHTML = await page.locator('.nav').innerHTML();
    console.log('\nNav contains user menu:', navHTML.includes('user-menu'));
    console.log('Nav contains login button:', navHTML.includes('Login'));
    
    // Take screenshot
    await page.screenshot({ path: 'user-menu-display.png' });
    
    // Try clicking user menu if it exists
    if (userMenuButton > 0) {
      console.log('\n3. Trying to click user menu...');
      await page.click('.user-menu-btn');
      await page.waitForTimeout(1000);
      
      const dropdownVisible = await page.locator('.user-dropdown.show').count();
      console.log('Dropdown visible after click:', dropdownVisible > 0);
      
      await page.screenshot({ path: 'user-menu-clicked.png' });
    }
    
    console.log('\n✅ Screenshots saved');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();