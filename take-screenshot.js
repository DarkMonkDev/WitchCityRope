const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  // Set viewport size
  await page.setViewportSize({ width: 1920, height: 1080 });
  
  try {
    // Navigate to the homepage
    console.log('Navigating to http://localhost:5173...');
    await page.goto('http://localhost:5173', { waitUntil: 'domcontentloaded', timeout: 30000 });
    
    // Wait a bit for React to render
    await page.waitForTimeout(3000);
    
    // Log what's on the page
    const title = await page.title();
    console.log('Page title:', title);
    
    // Try to find any navigation-related elements
    const navElements = await page.$$('nav, header, [role="navigation"]');
    console.log('Found navigation elements:', navElements.length);
    
    // Take screenshot regardless
    await page.screenshot({ 
      path: 'homepage-with-fixed-navigation.png',
      fullPage: false
    });
    
    console.log('Screenshot saved as homepage-with-fixed-navigation.png');
  } catch (error) {
    console.error('Error taking screenshot:', error.message);
    
    // Try to take screenshot anyway
    try {
      await page.screenshot({ 
        path: 'homepage-with-fixed-navigation.png',
        fullPage: false
      });
      console.log('Screenshot saved despite error');
    } catch (e) {
      console.error('Failed to take screenshot:', e.message);
    }
  } finally {
    await browser.close();
  }
})();