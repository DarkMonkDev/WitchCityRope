const puppeteer = require('puppeteer');

(async () => {
  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();
  
  // Capture console messages
  const consoleMessages = [];
  page.on('console', msg => {
    consoleMessages.push({
      type: msg.type(),
      text: msg.text()
    });
  });
  
  // Capture page errors
  page.on('pageerror', error => {
    consoleMessages.push({
      type: 'pageerror',
      text: error.message
    });
  });
  
  try {
    await page.goto('http://localhost:5173', { waitUntil: 'networkidle0', timeout: 10000 });
    
    // Check if React root has content
    const rootContent = await page.$eval('#root', el => el.innerHTML);
    console.log('Root element content:', rootContent ? rootContent.substring(0, 200) : 'EMPTY');
    
    // Print all console messages
    console.log('\nConsole messages:');
    consoleMessages.forEach(msg => {
      console.log(`[${msg.type}] ${msg.text}`);
    });
    
  } catch (error) {
    console.error('Error loading page:', error.message);
  }
  
  await browser.close();
})();
