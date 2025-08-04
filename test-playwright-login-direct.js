const { chromium } = require('playwright');

async function testLoginDirect() {
  console.log('🚀 Testing Playwright login with EXACT working approach...');
  
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  try {
    // Navigate to login page
    console.log('📍 Navigating to login page...');
    await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
    
    console.log('🔍 Current URL:', page.url());
    
    // Wait for form to be interactive
    console.log('⏳ Waiting for form to be interactive...');
    await page.waitForTimeout(3000);
    
    // Find form inputs using the EXACT selectors from working simple-login-test.js
    const emailSelector = 'input[name="login-email"]';
    const passwordSelector = 'input[name="login-password"]';
    const submitSelector = 'button[type="submit"]';
    
    console.log('🔍 Looking for email field...');
    await page.waitForSelector(emailSelector, { timeout: 10000 });
    
    console.log('🔍 Looking for password field...');
    await page.waitForSelector(passwordSelector, { timeout: 5000 });
    
    console.log('📝 Filling in login form...');
    await page.fill(emailSelector, 'admin@witchcityrope.com');
    await page.fill(passwordSelector, 'Test123!');
    
    // Take screenshot before submission
    await page.screenshot({ path: 'playwright-direct-before.png', fullPage: true });
    console.log('📸 Screenshot saved: playwright-direct-before.png');
    
    console.log('🔄 Submitting form...');
    
    // Use EXACT approach from working simple-login-test.js
    const [response] = await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click(submitSelector + ':has-text("Sign In")') // Be specific about which submit button
    ]);
    
    // Wait a moment for any processing
    await page.waitForTimeout(2000);
    
    console.log('🔗 URL after submission:', page.url());
    
    // Take screenshot after submission
    await page.screenshot({ path: 'playwright-direct-after.png', fullPage: true });
    console.log('📸 Screenshot saved: playwright-direct-after.png');
    
    // Check the page content
    const bodyText = await page.textContent('body');
    
    // Look for success/error indicators
    if (page.url().includes('dashboard') || bodyText.includes('Dashboard') || bodyText.includes('Welcome')) {
      console.log('✅ Login appears successful - redirected to dashboard!');
    } else if (page.url().includes('login')) {
      console.log('⚠️ Still on login page - checking for error messages...');
      
      // Look for error messages
      const errorElements = await page.$$eval('*', (elements) => {
        const errors = [];
        for (const el of elements) {
          const text = el.innerText || el.textContent || '';
          if (text && (
            text.toLowerCase().includes('error') ||
            text.toLowerCase().includes('invalid') ||
            text.toLowerCase().includes('incorrect') ||
            text.toLowerCase().includes('failed') ||
            text.toLowerCase().includes('wrong')
          )) {
            errors.push(text.trim());
          }
        }
        return errors.filter((text, index, arr) => arr.indexOf(text) === index).slice(0, 5);
      });
      
      if (errorElements.length > 0) {
        console.log('📋 Error messages found:', errorElements);
      } else {
        console.log('❓ No clear error messages found');
        console.log('📋 Current page URL:', page.url());
        console.log('📋 Page title:', await page.title());
      }
    } else {
      console.log('❓ Unexpected page - URL:', page.url());
    }
    
  } catch (error) {
    console.log('❌ Test failed:', error.message);
    await page.screenshot({ path: 'playwright-direct-error.png', fullPage: true });
  } finally {
    await browser.close();
    console.log('🏁 Direct test completed');
  }
}

testLoginDirect().catch(console.error);