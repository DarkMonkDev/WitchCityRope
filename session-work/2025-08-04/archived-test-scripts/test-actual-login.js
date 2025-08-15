const { chromium } = require('playwright');

async function testActualLogin() {
  console.log('🔧 Testing ACTUAL login functionality...');
  
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  try {
    // Navigate to login
    console.log('📍 Going to login page...');
    await page.goto('http://localhost:5651/login');
    await page.waitForLoadState('networkidle');
    
    // Fill login form with admin credentials
    console.log('📝 Filling login form...');
    await page.fill('input[name="login-email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="login-password"]', 'Test123!');
    
    // Submit form and wait for navigation
    console.log('🚀 Submitting login form...');
    const responsePromise = page.waitForResponse(response => 
      response.url().includes('/login') && response.request().method() === 'POST'
    );
    
    await page.click('button[type="submit"]');
    const response = await responsePromise;
    
    console.log(`📊 Login POST response: ${response.status()}`);
    
    // Wait a bit for any redirects
    await page.waitForTimeout(3000);
    
    const currentUrl = page.url();
    console.log(`🌐 Current URL after login: ${currentUrl}`);
    
    // Check if we're redirected to dashboard
    if (currentUrl.includes('/dashboard') || currentUrl.includes('/admin') || currentUrl.includes('/member')) {
      console.log('✅ SUCCESS: Login redirected to authenticated area!');
    } else if (currentUrl.includes('/login')) {
      console.log('❌ FAILURE: Still on login page - authentication failed');
      
      // Check for error messages
      const errorMessages = await page.locator('.validation-message, .alert-danger, .error').allTextContents();
      if (errorMessages.length > 0) {
        console.log('🚨 Error messages found:', errorMessages);
      }
    } else {
      console.log(`⚠️  UNKNOWN: Redirected to unexpected URL: ${currentUrl}`);
    }
    
    // Take screenshot for debugging
    await page.screenshot({ path: 'login-test-result.png', fullPage: true });
    console.log('📸 Screenshot saved as login-test-result.png');
    
  } catch (error) {
    console.error('💥 Test failed with error:', error.message);
  } finally {
    await browser.close();
  }
}

testActualLogin();