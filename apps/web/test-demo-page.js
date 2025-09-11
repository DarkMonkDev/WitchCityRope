// Quick test script to verify the demo page loads
const puppeteer = require('puppeteer');

async function testDemoPage() {
  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();
  
  // Listen for console messages
  page.on('console', msg => console.log('PAGE LOG:', msg.text()));
  
  // Listen for errors
  page.on('error', err => console.error('PAGE ERROR:', err));
  page.on('pageerror', err => console.error('PAGE RUNTIME ERROR:', err));
  
  try {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo', { 
      waitUntil: 'networkidle0', 
      timeout: 30000 
    });
    
    // Wait for React to render
    await page.waitForSelector('body', { timeout: 5000 });
    
    // Check if main content exists
    const hasContent = await page.evaluate(() => {
      const title = document.querySelector('h1');
      const container = document.querySelector('[data-testid], .mantine-Container-root, .container');
      return {
        hasTitle: !!title,
        titleText: title?.textContent || 'No title found',
        hasContainer: !!container,
        bodyContent: document.body.textContent.substring(0, 200)
      };
    });
    
    console.log('Page content check:', hasContent);
    
    // Screenshot for debugging
    await page.screenshot({ path: 'debug-demo-page.png', fullPage: true });
    
  } catch (error) {
    console.error('Test failed:', error);
  } finally {
    await browser.close();
  }
}

testDemoPage();