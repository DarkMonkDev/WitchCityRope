const { chromium } = require('playwright');

async function testLoginSelectors() {
  console.log('🔍 Testing Playwright selectors for login form...');
  
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  try {
    // Navigate to login page
    console.log('📍 Navigating to login page...');
    await page.goto('http://localhost:5651/login');
    
    console.log('🔍 Current URL:', page.url());
    
    // Wait for page load
    await page.waitForTimeout(3000);
    
    // Test the working selectors from simple-login-test.js
    console.log('✅ Testing WORKING selectors:');
    const workingEmailSelector = 'input[name="login-email"]';
    const workingPasswordSelector = 'input[name="login-password"]';
    const workingSubmitSelector = 'button[type="submit"]';
    
    try {
      await page.waitForSelector(workingEmailSelector, { timeout: 5000 });
      console.log('  ✅ Found: ' + workingEmailSelector);
    } catch (e) {
      console.log('  ❌ NOT FOUND: ' + workingEmailSelector);
    }
    
    try {
      await page.waitForSelector(workingPasswordSelector, { timeout: 5000 });
      console.log('  ✅ Found: ' + workingPasswordSelector);
    } catch (e) {
      console.log('  ❌ NOT FOUND: ' + workingPasswordSelector);
    }
    
    try {
      await page.waitForSelector(workingSubmitSelector, { timeout: 5000 });
      console.log('  ✅ Found: ' + workingSubmitSelector);
    } catch (e) {
      console.log('  ❌ NOT FOUND: ' + workingSubmitSelector);
    }
    
    // Test the Playwright LoginPage selectors
    console.log('\\n🧪 Testing PLAYWRIGHT LoginPage selectors:');
    const playwrightEmailSelector = 'input[type="email"], input#login-email, input[placeholder*="email"]';
    const playwrightPasswordSelector = 'input[type="password"], input#login-password, input[placeholder*="password"]';
    const playwrightSubmitSelector = 'button[type="submit"], .btn-primary-full, button:has-text("Sign In")';
    
    try {
      const emailElement = await page.locator(playwrightEmailSelector).first();
      const emailCount = await page.locator(playwrightEmailSelector).count();
      console.log(`  📊 Email selector found ${emailCount} elements`);
      if (emailCount > 0) {
        const emailName = await emailElement.getAttribute('name');
        const emailType = await emailElement.getAttribute('type');
        console.log(`    - First element: name="${emailName}", type="${emailType}"`);
      }
    } catch (e) {
      console.log('  ❌ Email selector error:', e.message);
    }
    
    try {
      const passwordElement = await page.locator(playwrightPasswordSelector).first();
      const passwordCount = await page.locator(playwrightPasswordSelector).count();
      console.log(`  📊 Password selector found ${passwordCount} elements`);
      if (passwordCount > 0) {
        const passwordName = await passwordElement.getAttribute('name');
        const passwordType = await passwordElement.getAttribute('type');
        console.log(`    - First element: name="${passwordName}", type="${passwordType}"`);
      }
    } catch (e) {
      console.log('  ❌ Password selector error:', e.message);
    }
    
    try {
      const submitElement = await page.locator(playwrightSubmitSelector).first();
      const submitCount = await page.locator(playwrightSubmitSelector).count();
      console.log(`  📊 Submit selector found ${submitCount} elements`);
      if (submitCount > 0) {
        const submitType = await submitElement.getAttribute('type');
        const submitText = await submitElement.textContent();
        console.log(`    - First element: type="${submitType}", text="${submitText}"`);
      }
    } catch (e) {
      console.log('  ❌ Submit selector error:', e.message);
    }
    
    // Try the login with working selectors
    console.log('\\n🚀 Testing login with WORKING selectors...');
    
    await page.fill(workingEmailSelector, 'admin@witchcityrope.com');
    await page.fill(workingPasswordSelector, 'Test123!');
    
    console.log('📸 Taking screenshot before submission...');
    await page.screenshot({ path: 'playwright-selector-test-before.png', fullPage: true });
    
    // Submit and check result
    const [response] = await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click(workingSubmitSelector)
    ]);
    
    await page.waitForTimeout(2000);
    
    console.log('🔗 URL after submission:', page.url());
    console.log('📸 Taking screenshot after submission...');
    await page.screenshot({ path: 'playwright-selector-test-after.png', fullPage: true });
    
    if (page.url().includes('login')) {
      console.log('❌ Still on login page - login failed');
    } else {
      console.log('✅ Login successful - redirected!');
    }
    
  } catch (error) {
    console.log('❌ Test failed:', error.message);
    await page.screenshot({ path: 'playwright-selector-test-error.png', fullPage: true });
  } finally {
    await browser.close();
    console.log('🏁 Selector test completed');
  }
}

testLoginSelectors().catch(console.error);