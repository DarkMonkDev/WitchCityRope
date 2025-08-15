const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Checking Rendered HTML ===\n');
    
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
    await page.waitForTimeout(3000);
    
    // 3. Get the nav HTML
    console.log('\n3. Checking nav HTML...');
    const navHTML = await page.locator('.nav').innerHTML();
    console.log('Nav HTML length:', navHTML.length);
    
    // Check specific elements
    const hasUserMenu = navHTML.includes('user-menu');
    const hasLoginButton = navHTML.includes('Login');
    const hasDropdown = navHTML.includes('user-dropdown');
    
    console.log('\nNav contains:');
    console.log('- User menu:', hasUserMenu);
    console.log('- Login button:', hasLoginButton);  
    console.log('- Dropdown:', hasDropdown);
    
    // Extract just the auth section
    const authSection = await page.locator('.nav > div:last-child').innerHTML().catch(() => 'Not found');
    console.log('\nAuth section HTML:');
    console.log(authSection.substring(0, 200) + '...');
    
    // Check _isAuthenticated value
    const isAuthValue = await page.evaluate(() => {
      // Try to access Blazor component state (this is a hack)
      const header = document.querySelector('.header');
      return header ? header.textContent : null;
    });
    
    console.log('\n✅ Analysis complete');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();