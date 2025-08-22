const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function takeScreenshot() {
  console.log('Starting Puppeteer with Windows Chrome...');
  
  // Try to use Windows Chrome installation
  const possibleChromePaths = [
    '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Users/chad/AppData/Local/Google/Chrome/Application/chrome.exe'
  ];

  let chromePath = null;
  for (const path of possibleChromePaths) {
    if (fs.existsSync(path)) {
      chromePath = path;
      break;
    }
  }

  const launchOptions = {
    headless: false, // Run in non-headless mode to see what's happening
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu'
    ]
  };

  if (chromePath) {
    console.log(`Found Chrome at: ${chromePath}`);
    launchOptions.executablePath = chromePath;
  }

  try {
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewport({
      width: 1920,
      height: 1080
    });

    console.log('Navigating to http://localhost:5651/...');
    
    try {
      const response = await page.goto('http://localhost:5651/', {
        waitUntil: 'networkidle2',
        timeout: 30000
      });

      if (response) {
        console.log(`Page loaded with status: ${response.status()}`);
      }
    } catch (navError) {
      console.error('Navigation error:', navError.message);
      
      // Take a screenshot of whatever loaded
      console.log('Taking screenshot of current page state...');
    }

    // Wait a bit for any dynamic content
    await page.waitForTimeout(2000);

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }

    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
    const screenshotPath = path.join(screenshotsDir, `wcr-homepage-${timestamp}.png`);

    // Take screenshot
    console.log('Taking screenshot...');
    await page.screenshot({
      path: screenshotPath,
      fullPage: true
    });

    console.log(`Screenshot saved to: ${screenshotPath}`);

    // Get page info
    const pageInfo = await page.evaluate(() => {
      return {
        title: document.title,
        url: window.location.href,
        hasContent: document.body.innerHTML.length > 0
      };
    });

    console.log('\nPage information:');
    console.log(`- Title: ${pageInfo.title}`);
    console.log(`- URL: ${pageInfo.url}`);
    console.log(`- Has content: ${pageInfo.hasContent}`);

    await browser.close();
    console.log('\nBrowser closed.');

  } catch (error) {
    console.error('Error:', error.message);
  }
}

takeScreenshot().catch(console.error);