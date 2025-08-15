const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  try {
    // Navigate to login page
    console.log('Navigating to login page...');
    await page.goto('http://localhost:5651/login');
    
    // Login
    console.log('Logging in...');
    await page.fill('input[name="Input.Email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for navigation
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    console.log('Login successful, on dashboard');
    
    // Wait for dashboard to load
    await page.waitForSelector('.page-title', { timeout: 10000 });
    
    // Take screenshot
    console.log('Taking screenshot...');
    await page.screenshot({ 
      path: 'admin-dashboard-current.png', 
      fullPage: true 
    });
    
    console.log('Screenshot saved as admin-dashboard-current.png');
    
  } catch (error) {
    console.error('Error:', error);
  } finally {
    await browser.close();
  }
})();