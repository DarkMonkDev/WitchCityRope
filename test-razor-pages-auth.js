const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing Razor Pages Authentication ===\n');
    
    // 1. Test Direct Razor Page Login
    console.log('1. Testing Direct Razor Page Login...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    console.log('✓ Razor Page login loaded');
    
    // Fill credentials
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    console.log('✓ Credentials entered');
    
    // Submit form
    await page.click('button[type="submit"]');
    console.log('✓ Form submitted');
    
    // Wait for response
    await page.waitForTimeout(3000);
    const afterLoginUrl = page.url();
    console.log('After login URL:', afterLoginUrl);
    
    if (afterLoginUrl.includes('/dashboard') || afterLoginUrl === 'http://localhost:5651/') {
      console.log('✅ LOGIN SUCCESS - Authentication working!');
      
      // Check authentication state
      const response = await page.goto('http://localhost:5651/admin/dashboard');
      if (response.status() === 200) {
        console.log('✅ ADMIN ACCESS - Can access protected pages');
      }
    } else {
      console.log('❌ LOGIN FAILED - Still on login page or error');
      
      // Check for errors
      const errors = await page.locator('.text-danger, .validation-summary-errors').allTextContents();
      if (errors.length > 0) {
        console.log('Errors found:', errors);
      }
    }
    
    // 2. Test Blazor Component Redirect
    console.log('\n2. Testing Blazor Component Redirect to Razor Page...');
    await page.goto('http://localhost:5651/login');
    console.log('✓ Blazor login component loaded');
    
    // Fill form in Blazor component
    await page.fill('input[type="email"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');
    
    // Click login (should redirect to Razor Page)
    await page.click('button[type="submit"]');
    console.log('✓ Blazor form submitted');
    
    await page.waitForTimeout(2000);
    const blazorRedirectUrl = page.url();
    
    if (blazorRedirectUrl.includes('/Identity/Account/Login')) {
      console.log('✅ BLAZOR REDIRECT SUCCESS - Redirected to Razor Page');
      
      // Check if form is pre-filled
      const emailValue = await page.inputValue('input[name="Input.EmailOrUsername"]');
      if (emailValue === 'admin@witchcityrope.com') {
        console.log('✅ FORM PRE-FILL SUCCESS - Email transferred correctly');
      }
    } else {
      console.log('❌ BLAZOR REDIRECT FAILED - URL:', blazorRedirectUrl);
    }
    
    // 3. Test Logout
    console.log('\n3. Testing Logout...');
    await page.goto('http://localhost:5651/Identity/Account/Logout', { waitUntil: 'networkidle' });
    console.log('Current URL after logout:', page.url());
    
    // Try to access protected page
    await page.goto('http://localhost:5651/admin/dashboard');
    await page.waitForTimeout(1000);
    
    const afterLogoutUrl = page.url();
    if (afterLogoutUrl.includes('/login') || afterLogoutUrl.includes('/Identity/Account/Login')) {
      console.log('✅ LOGOUT SUCCESS - Cannot access protected pages');
    } else {
      console.log('❌ LOGOUT FAILED - Still have access');
    }
    
  } catch (error) {
    console.log('❌ Test error:', error.message);
    console.log('Stack:', error.stack);
  } finally {
    await browser.close();
  }
})();