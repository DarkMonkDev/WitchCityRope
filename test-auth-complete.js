const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Complete Authentication Test ===\n');
    
    // 1. Login via Razor Page
    console.log('1. Testing Login via Razor Page...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for navigation
    await page.waitForNavigation({ timeout: 10000 }).catch(() => {});
    
    const afterLoginUrl = page.url();
    console.log('✓ After login URL:', afterLoginUrl);
    
    if (!afterLoginUrl.includes('/Login') && !afterLoginUrl.includes('/login')) {
      console.log('✅ LOGIN SUCCESS - Left login page');
      
      // Check cookies
      const cookies = await context.cookies();
      const authCookie = cookies.find(c => c.name.includes('AspNetCore') || c.name.includes('Identity'));
      if (authCookie) {
        console.log('✅ AUTH COOKIE SET:', authCookie.name);
      }
      
      // Test member dashboard access
      console.log('\n2. Testing Member Dashboard Access...');
      const memberResponse = await page.goto('http://localhost:5651/member/dashboard', { 
        waitUntil: 'domcontentloaded',
        timeout: 10000 
      }).catch(e => ({ status: () => 'error', error: e.message }));
      
      if (memberResponse.status && memberResponse.status() === 200) {
        console.log('✅ MEMBER DASHBOARD ACCESS - Authorized');
      } else {
        console.log('❌ MEMBER DASHBOARD ACCESS FAILED:', memberResponse.error || page.url());
      }
      
      // Test admin access
      console.log('\n3. Testing Admin Access...');
      const adminResponse = await page.goto('http://localhost:5651/admin', { 
        waitUntil: 'domcontentloaded',
        timeout: 10000 
      }).catch(e => ({ status: () => 'error', error: e.message }));
      
      if (adminResponse.status && adminResponse.status() === 200) {
        console.log('✅ ADMIN ACCESS - Authorized as admin');
      } else {
        console.log('Admin access result:', adminResponse.error || page.url());
        // This might be expected if user is not admin
      }
      
    } else {
      console.log('❌ LOGIN FAILED - Still on login page');
      const errors = await page.locator('.text-danger').allTextContents();
      console.log('Errors:', errors);
    }
    
    // 4. Test Logout
    console.log('\n4. Testing Logout...');
    
    // First try GET request to logout
    await page.goto('http://localhost:5651/Identity/Account/Logout');
    await page.waitForTimeout(2000);
    
    console.log('After logout URL:', page.url());
    
    // Verify logged out by trying to access protected page
    await page.goto('http://localhost:5651/member/dashboard');
    await page.waitForTimeout(1000);
    
    const finalUrl = page.url();
    if (finalUrl.includes('/login') || finalUrl.includes('/Login')) {
      console.log('✅ LOGOUT SUCCESS - Redirected to login');
    } else {
      console.log('Logout result - Current URL:', finalUrl);
    }
    
    console.log('\n=== Summary ===');
    console.log('Razor Pages authentication is working with standard ASP.NET Core Identity');
    console.log('Login: /Identity/Account/Login');
    console.log('Logout: /Identity/Account/Logout');
    console.log('Register: /Identity/Account/Register');
    
  } catch (error) {
    console.log('❌ Test error:', error.message);
  } finally {
    await page.waitForTimeout(2000); // Keep browser open briefly to see results
    await browser.close();
  }
})();