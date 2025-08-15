const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  try {
    console.log('=== Testing Login Functionality ===\n');
    
    // 1. Navigate to login page
    console.log('1. Navigating to Identity login page...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.waitForSelector('input[name="Input.EmailOrUsername"]', { timeout: 10000 });
    
    // 2. Take screenshot of styled login page
    await page.screenshot({ path: 'identity-login-styled.png', fullPage: true });
    console.log('   ✅ Screenshot saved as identity-login-styled.png');
    
    // 3. Fill login form
    console.log('\n2. Filling login form...');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    console.log('   ✅ Form filled');
    
    // 4. Submit form
    console.log('\n3. Submitting form...');
    await page.click('button[type="submit"]:has-text("Sign In")');
    
    // 5. Wait for navigation
    console.log('\n4. Waiting for navigation...');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    const currentUrl = page.url();
    console.log('   Current URL:', currentUrl);
    
    // 6. Check if login was successful
    if (!currentUrl.includes('/Login')) {
      console.log('   ✅ Successfully logged in!');
      
      // Check for user menu
      const userMenu = await page.locator('.user-menu').count();
      if (userMenu > 0) {
        const userName = await page.locator('.user-menu').textContent();
        console.log('   ✅ User menu found:', userName.trim());
      }
      
      // Take screenshot of logged in state
      await page.screenshot({ path: 'after-login.png', fullPage: true });
      console.log('   ✅ Screenshot saved as after-login.png');
    } else {
      console.log('   ❌ Still on login page');
      
      // Check for validation errors
      const errors = await page.locator('.text-danger').allTextContents();
      if (errors.length > 0) {
        console.log('   Errors found:', errors);
      }
    }
    
    console.log('\n✅ Test complete!');
    
  } catch (error) {
    console.error('❌ Error:', error.message);
  }
  
  // Keep browser open for inspection
  console.log('\nPress Ctrl+C to close browser.');
  await new Promise(() => {});
})();