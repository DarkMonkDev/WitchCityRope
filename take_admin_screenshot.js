const { chromium } = require('playwright');

(async () => {
  // Launch browser
  const browser = await chromium.launch({
    headless: true // Run in headless mode
  });
  
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 } // Set a good viewport size for screenshots
  });
  
  const page = await context.newPage();
  
  try {
    // Navigate to login page
    console.log('Navigating to login page...');
    await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
    
    // Fill in login credentials
    console.log('Filling login form...');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    
    // Submit login form
    console.log('Submitting login...');
    await page.click('button[type="submit"]');
    
    // Wait for navigation to complete after login
    await page.waitForLoadState('networkidle');
    
    // Navigate to admin dashboard
    console.log('Navigating to admin dashboard...');
    await page.goto('http://localhost:5651/admin/dashboard', { waitUntil: 'networkidle' });
    
    // Alternative: try just /admin if /admin/dashboard doesn't work
    if (page.url() !== 'http://localhost:5651/admin/dashboard') {
      console.log('Trying /admin instead...');
      await page.goto('http://localhost:5651/admin', { waitUntil: 'networkidle' });
    }
    
    // Wait a bit for any dynamic content to load
    await page.waitForTimeout(2000);
    
    // Take full page screenshot
    console.log('Taking screenshot...');
    await page.screenshot({ 
      path: 'admin-dashboard-current.png',
      fullPage: true // Capture the entire page including scrollable content
    });
    
    console.log('Screenshot saved as admin-dashboard-current.png');
    
  } catch (error) {
    console.error('Error occurred:', error);
  } finally {
    // Close browser
    await browser.close();
  }
})();