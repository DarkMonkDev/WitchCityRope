const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  try {
    // Navigate to login page
    console.log('Navigating to login page...');
    await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
    
    // Take screenshot of login page
    await page.screenshot({ 
      path: 'login-page-check.png', 
      fullPage: true 
    });
    
    // Check for input fields
    const emailInputs = await page.$$('input[type="email"], input[name*="email" i], input[name*="Email" i]');
    console.log(`Found ${emailInputs.length} email input fields`);
    
    const passwordInputs = await page.$$('input[type="password"]');
    console.log(`Found ${passwordInputs.length} password input fields`);
    
    // Get all input names
    const inputNames = await page.$$eval('input', inputs => 
      inputs.map(input => ({ name: input.name, type: input.type, id: input.id }))
    );
    console.log('All input fields:', inputNames);
    
    // Try different selectors and login
    if (emailInputs.length > 0 && passwordInputs.length > 0) {
      console.log('Attempting login with found fields...');
      await emailInputs[0].fill('admin@witchcityrope.com');
      await passwordInputs[0].fill('Test123!');
      
      // Find and click submit button
      const submitButton = await page.$('button[type="submit"], input[type="submit"], button:has-text("Log in"), button:has-text("Login")');
      if (submitButton) {
        await submitButton.click();
        await page.waitForTimeout(3000);
        
        console.log('Current URL after login attempt:', page.url());
        
        // Navigate to admin dashboard
        await page.goto('http://localhost:5651/admin/dashboard');
        await page.waitForTimeout(2000);
        
        // Take screenshot
        await page.screenshot({ 
          path: 'admin-dashboard-after-login.png', 
          fullPage: true 
        });
        console.log('Dashboard screenshot saved');
      }
    }
    
  } catch (error) {
    console.error('Error:', error);
  } finally {
    await browser.close();
  }
})();