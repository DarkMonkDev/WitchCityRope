const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  try {
    // Navigate to login page
    console.log('Navigating to login page...');
    await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
    
    // Login
    const emailInput = await page.$('input[type="email"], input[name*="email" i], input[name*="Email" i]');
    const passwordInput = await page.$('input[type="password"]');
    
    if (emailInput && passwordInput) {
      await emailInput.fill('admin@witchcityrope.com');
      await passwordInput.fill('Test123!');
      
      const submitButton = await page.$('button[type="submit"], input[type="submit"]');
      if (submitButton) {
        await submitButton.click();
        await page.waitForTimeout(3000);
        
        // Navigate to events page
        console.log('Navigating to events page...');
        await page.goto('http://localhost:5651/admin/events');
        await page.waitForTimeout(2000);
        
        // Take screenshot
        await page.screenshot({ 
          path: 'admin-events-page.png', 
          fullPage: true 
        });
        console.log('Events page screenshot saved');
      }
    }
    
  } catch (error) {
    console.error('Error:', error);
  } finally {
    await browser.close();
  }
})();