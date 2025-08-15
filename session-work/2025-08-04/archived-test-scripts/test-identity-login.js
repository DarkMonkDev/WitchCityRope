const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  try {
    console.log('1. Navigating to Identity login page...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    
    console.log('2. Filling login form...');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    
    console.log('3. Submitting form...');
    await page.click('button[type="submit"]');
    
    console.log('4. Waiting for navigation...');
    await page.waitForLoadState('networkidle');
    
    const currentUrl = page.url();
    console.log('5. Current URL:', currentUrl);
    
    // Check if we're redirected away from login
    if (!currentUrl.includes('/Login')) {
      console.log('✅ Login successful! Redirected to:', currentUrl);
    } else {
      console.log('❌ Still on login page');
      // Check for error messages
      const errorMessages = await page.locator('.text-danger').allTextContents();
      if (errorMessages.length > 0) {
        console.log('Error messages:', errorMessages);
      }
    }
    
    // Check for user info in nav
    const userInfo = await page.locator('.user-menu').textContent().catch(() => null);
    if (userInfo) {
      console.log('✅ User menu found:', userInfo);
    }
    
  } catch (error) {
    console.error('Error:', error.message);
  }
  
  // Keep browser open for inspection
  console.log('\n✅ Test complete. Press Ctrl+C to close browser.');
  await new Promise(() => {});
})();