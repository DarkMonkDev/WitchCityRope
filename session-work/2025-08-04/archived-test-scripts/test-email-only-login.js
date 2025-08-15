const { chromium } = require('playwright');

async function testEmailOnlyLogin() {
  console.log('🔧 Testing EMAIL login only...');
  
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  try {
    await page.goto('http://localhost:5651/login');
    await page.waitForLoadState('networkidle');
    
    console.log('📧 Filling with admin email...');
    await page.fill('input[name="login-email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="login-password"]', 'Test123!');
    
    console.log('🚀 Submitting...');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(5000);
    
    const finalUrl = page.url();
    const errorMessages = await page.locator('.validation-message, .alert-danger, .error, .text-danger').allTextContents();
    const pageTitle = await page.title();
    
    console.log(`🌐 Final URL: ${finalUrl}`);
    console.log(`📄 Page title: ${pageTitle}`);
    if (errorMessages.length > 0) {
      console.log('🚨 Error messages:', errorMessages);
    } else {
      console.log('✅ No error messages found');
    }
    
    // Check for success indicators
    const hasAdminText = await page.locator('text=Admin').count();
    const hasDashboardText = await page.locator('text=Dashboard').count();
    console.log(`🔍 Admin text count: ${hasAdminText}`);
    console.log(`🔍 Dashboard text count: ${hasDashboardText}`);
    
    if (finalUrl.includes('/login')) {
      console.log('❌ STILL ON LOGIN PAGE - Authentication failed');
    } else {
      console.log('✅ SUCCESS: Redirected away from login!');
    }
    
    await page.screenshot({ path: 'email-login-test.png', fullPage: true });
    await page.waitForTimeout(3000);
    
  } catch (error) {
    console.error('💥 Error:', error.message);
  } finally {
    await browser.close();
  }
}

testEmailOnlyLogin();