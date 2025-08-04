const { chromium } = require('playwright');

async function testUsernameLogin() {
  console.log('🔧 Testing login with USERNAME instead of email...');
  
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  try {
    await page.goto('http://localhost:5651/login');
    await page.waitForLoadState('networkidle');
    
    // Use USERNAME not email
    console.log('📝 Filling with USERNAME: RopeAdmin');
    await page.fill('input[name="login-email"]', 'RopeAdmin');
    await page.fill('input[name="login-password"]', 'Test123!');
    
    await page.click('button[type="submit"]');
    await page.waitForTimeout(5000);
    
    const finalUrl = page.url();
    console.log(`🌐 Final URL: ${finalUrl}`);
    
    const errorMessages = await page.locator('.validation-message, .alert-danger, .error, .text-danger').allTextContents();
    if (errorMessages.length > 0) {
      console.log('🚨 Error messages:', errorMessages);
    }
    
    if (finalUrl.includes('/login')) {
      console.log('❌ STILL ON LOGIN PAGE');
    } else {
      console.log('✅ SUCCESS: Redirected away from login!');
    }
    
    await page.waitForTimeout(3000);
    
  } catch (error) {
    console.error('💥 Error:', error.message);
  } finally {
    await browser.close();
  }
}

testUsernameLogin();