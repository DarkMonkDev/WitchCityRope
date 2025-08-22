const puppeteer = require('puppeteer');

async function takeScreenshot() {
  console.log('Starting Puppeteer...');
  
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  
  try {
    const page = await browser.newPage();
    
    // Set viewport to a common desktop size
    await page.setViewport({ width: 1920, height: 1080 });
    
    console.log('Navigating to http://localhost:5651/auth/login...');
    
    // Navigate to the login page
    await page.goto('http://localhost:5651/auth/login', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });
    
    // Wait a bit for any animations or lazy-loaded content
    await page.waitForTimeout(2000);
    
    // Take a full page screenshot
    const timestamp = new Date().toISOString().replace(/[:]/g, '-').split('.')[0];
    const screenshotPath = `./login-page-screenshot-${timestamp}.png`;
    
    await page.screenshot({
      path: screenshotPath,
      fullPage: true
    });
    
    console.log(`Screenshot saved to: ${screenshotPath}`);
    
    // Also take a viewport-only screenshot
    const viewportPath = `./login-page-viewport-${timestamp}.png`;
    await page.screenshot({
      path: viewportPath,
      fullPage: false
    });
    
    console.log(`Viewport screenshot saved to: ${viewportPath}`);
    
    // Log any console messages from the page
    page.on('console', msg => console.log('Page console:', msg.text()));
    
    // Check for any errors in the console
    page.on('pageerror', error => console.error('Page error:', error.message));
    
    // Get page title and URL for verification
    const title = await page.title();
    const url = page.url();
    
    console.log(`Page title: ${title}`);
    console.log(`Page URL: ${url}`);
    
    // Check if there are any visible error messages on the page
    const errorMessages = await page.evaluate(() => {
      const errors = [];
      // Look for common error indicators
      const errorElements = document.querySelectorAll('.error, .alert-danger, .error-message, [class*="error"]');
      errorElements.forEach(el => {
        if (el.textContent.trim()) {
          errors.push(el.textContent.trim());
        }
      });
      return errors;
    });
    
    if (errorMessages.length > 0) {
      console.log('Found error messages on page:', errorMessages);
    }
    
    // Get basic page structure info
    const pageInfo = await page.evaluate(() => {
      return {
        forms: document.querySelectorAll('form').length,
        inputs: document.querySelectorAll('input').length,
        buttons: document.querySelectorAll('button, input[type="submit"]').length,
        links: document.querySelectorAll('a').length,
        images: document.querySelectorAll('img').length
      };
    });
    
    console.log('Page structure:', pageInfo);
    
  } catch (error) {
    console.error('Error taking screenshot:', error);
  } finally {
    await browser.close();
    console.log('Browser closed.');
  }
}

takeScreenshot().catch(console.error);