// Debug script to check React mounting issue
const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ 
    headless: false,  // Show the browser so I can see what's happening
    devtools: true    // Open dev tools
  });
  
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Collect all console messages
  const consoleMessages = [];
  page.on('console', msg => {
    console.log(`[CONSOLE ${msg.type()}]: ${msg.text()}`);
    consoleMessages.push(`[${msg.type()}]: ${msg.text()}`);
  });
  
  // Collect all errors
  const errors = [];
  page.on('pageerror', err => {
    console.log(`[PAGE ERROR]: ${err.message}`);
    errors.push(err.message);
  });
  
  // Monitor network requests
  page.on('response', response => {
    if (response.status() >= 400) {
      console.log(`[NETWORK ERROR]: ${response.url()} - ${response.status()}`);
    }
  });
  
  try {
    console.log('🔍 Opening http://localhost:5173...');
    await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });
    
    // Wait for potential React mounting
    await page.waitForTimeout(3000);
    
    // Check if root element has content
    const rootElement = await page.locator('#root');
    const rootContent = await rootElement.innerHTML();
    const rootTextContent = await rootElement.textContent();
    
    console.log('\n=== RESULTS ===');
    console.log(`Root element HTML length: ${rootContent.length}`);
    console.log(`Root element text length: ${rootTextContent?.length || 0}`);
    console.log(`Root element content: ${rootContent.slice(0, 200)}...`);
    
    console.log('\n=== CONSOLE MESSAGES ===');
    consoleMessages.forEach(msg => console.log(msg));
    
    console.log('\n=== ERRORS ===');
    errors.forEach(err => console.log(`ERROR: ${err}`));
    
    // Check if any expected console messages appear
    const hasInitLog = consoleMessages.some(msg => msg.includes('Starting React app initialization'));
    const hasRootLog = consoleMessages.some(msg => msg.includes('Creating React root'));
    const hasCompleteLog = consoleMessages.some(msg => msg.includes('React app render complete'));
    
    console.log('\n=== DEBUG STATUS ===');
    console.log(`✓ Init log found: ${hasInitLog}`);
    console.log(`✓ Root log found: ${hasRootLog}`);
    console.log(`✓ Complete log found: ${hasCompleteLog}`);
    
    // Take screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/debug-screenshot.png', fullPage: true });
    console.log('Screenshot saved to debug-screenshot.png');
    
    // Keep browser open for manual inspection
    console.log('\nBrowser will stay open for manual inspection. Press Ctrl+C to close.');
    await page.waitForTimeout(300000); // Wait 5 minutes
    
  } catch (error) {
    console.error('Error during debugging:', error);
  } finally {
    await browser.close();
  }
})();