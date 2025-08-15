const { chromium } = require('playwright');

async function testBothLoginMethods() {
  console.log('🔧 Testing BOTH email and username login...');
  
  const browser = await chromium.launch({ headless: false });
  
  // Test 1: Login with EMAIL
  console.log('\n=== TEST 1: Login with EMAIL ===');
  const page1 = await browser.newPage();
  try {
    await page1.goto('http://localhost:5651/login');
    await page1.waitForLoadState('networkidle');
    
    await page1.fill('input[name="login-email"]', 'admin@witchcityrope.com');
    await page1.fill('input[name="login-password"]', 'Test123!');
    await page1.click('button[type="submit"]');
    await page1.waitForTimeout(5000);
    
    const emailUrl = page1.url();
    const emailErrors = await page1.locator('.validation-message, .alert-danger, .error, .text-danger').allTextContents();
    
    console.log(`📧 Email login URL: ${emailUrl}`);
    if (emailErrors.length > 0) {
      console.log(`📧 Email login errors: ${emailErrors}`);
    }
    
    if (emailUrl.includes('/login')) {
      console.log('❌ Email login FAILED');
    } else {
      console.log('✅ Email login SUCCESS!');
    }
  } catch (error) {
    console.error('💥 Email login error:', error.message);
  } finally {
    await page1.close();
  }
  
  // Test 2: Login with USERNAME
  console.log('\n=== TEST 2: Login with USERNAME ===');
  const page2 = await browser.newPage();
  try {
    await page2.goto('http://localhost:5651/login');
    await page2.waitForLoadState('networkidle');
    
    await page2.fill('input[name="login-email"]', 'RopeAdmin');
    await page2.fill('input[name="login-password"]', 'Test123!');
    await page2.click('button[type="submit"]');
    await page2.waitForTimeout(5000);
    
    const usernameUrl = page2.url();
    const usernameErrors = await page2.locator('.validation-message, .alert-danger, .error, .text-danger').allTextContents();
    
    console.log(`👤 Username login URL: ${usernameUrl}`);
    if (usernameErrors.length > 0) {
      console.log(`👤 Username login errors: ${usernameErrors}`);
    }
    
    if (usernameUrl.includes('/login')) {
      console.log('❌ Username login FAILED');
    } else {
      console.log('✅ Username login SUCCESS!');
    }
  } catch (error) {
    console.error('💥 Username login error:', error.message);
  } finally {
    await page2.close();
  }
  
  await browser.close();
}

testBothLoginMethods();