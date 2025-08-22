#!/usr/bin/env node

const puppeteer = require('puppeteer');

async function testBrowserTools() {
  console.log('Testing Browser Tools MCP functionality...\n');
  
  try {
    // Check if Chrome is accessible
    console.log('1. Checking Chrome accessibility...');
    const chromePath = '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe';
    const fs = require('fs');
    
    if (fs.existsSync(chromePath)) {
      console.log('✓ Chrome executable found at:', chromePath);
    } else {
      console.log('✗ Chrome executable not found');
      return;
    }
    
    // Try to connect to existing Chrome instance
    console.log('\n2. Testing connection to Chrome debugging port...');
    try {
      const browser = await puppeteer.connect({
        browserURL: 'http://localhost:9222',
        defaultViewport: null
      });
      
      console.log('✓ Connected to existing Chrome instance');
      
      // Get browser version
      const version = await browser.version();
      console.log('   Browser version:', version);
      
      // List pages
      const pages = await browser.pages();
      console.log(`   Active pages: ${pages.length}`);
      
      for (let i = 0; i < pages.length; i++) {
        const page = pages[i];
        const title = await page.title();
        const url = page.url();
        console.log(`   - Page ${i + 1}: ${title} (${url})`);
      }
      
      // Test navigation
      console.log('\n3. Testing navigation capability...');
      const page = pages[0] || await browser.newPage();
      await page.goto('https://example.com');
      console.log('✓ Navigation successful');
      
      // Test screenshot
      console.log('\n4. Testing screenshot capability...');
      const screenshot = await page.screenshot({ encoding: 'base64' });
      console.log('✓ Screenshot captured (base64 length:', screenshot.length, ')');
      
      // Don't close the browser since it's an existing instance
      console.log('\n✓ All tests passed! Browser Tools MCP should work correctly.');
      
    } catch (connectError) {
      console.log('✗ Failed to connect to Chrome:', connectError.message);
      console.log('\nTrying to launch new Chrome instance...');
      
      // Try launching new instance
      const browser = await puppeteer.launch({
        executablePath: chromePath,
        headless: false,
        args: [
          '--no-sandbox',
          '--disable-setuid-sandbox',
          '--remote-debugging-port=9222'
        ]
      });
      
      console.log('✓ Launched new Chrome instance');
      
      // Perform basic test
      const page = await browser.newPage();
      await page.goto('https://example.com');
      console.log('✓ Navigation successful');
      
      // Keep browser open for MCP usage
      console.log('\n✓ Chrome is now ready for Browser Tools MCP');
    }
    
  } catch (error) {
    console.error('\n✗ Test failed:', error.message);
    console.error('Stack trace:', error.stack);
  }
}

// Run the test
testBrowserTools();