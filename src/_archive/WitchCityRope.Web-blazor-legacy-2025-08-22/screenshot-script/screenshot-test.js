const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testScreenshot() {
  console.log('Testing Puppeteer screenshot capability...\n');

  // Check for chromium availability
  const { execSync } = require('child_process');
  try {
    const chromiumPath = execSync('which chromium-browser || which chromium || which google-chrome').toString().trim();
    console.log('Found browser at:', chromiumPath);
  } catch (e) {
    console.log('No system browser found, will use bundled Chromium');
  }

  const options = {
    headless: 'new',
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-accelerated-2d-canvas',
      '--no-first-run',
      '--no-zygote',
      '--single-process',
      '--disable-gpu'
    ]
  };

  try {
    console.log('Launching browser...');
    const browser = await puppeteer.launch(options);
    
    console.log('Creating new page...');
    const page = await browser.newPage();
    
    console.log('Setting viewport to 1920x1080...');
    await page.setViewport({ width: 1920, height: 1080 });

    const url = 'http://localhost:5651/';
    console.log(`Navigating to ${url}...`);
    
    try {
      await page.goto(url, {
        waitUntil: 'networkidle2',
        timeout: 30000
      });
      console.log('Page loaded successfully!');
    } catch (e) {
      console.log('Navigation timeout or error:', e.message);
    }

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }

    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
    
    // Take viewport screenshot
    const viewportPath = path.join(screenshotsDir, `homepage-viewport-${timestamp}.png`);
    await page.screenshot({
      path: viewportPath,
      fullPage: false
    });
    console.log(`\nViewport screenshot saved: ${viewportPath}`);

    // Take full page screenshot
    const fullPagePath = path.join(screenshotsDir, `homepage-fullpage-${timestamp}.png`);
    await page.screenshot({
      path: fullPagePath,
      fullPage: true
    });
    console.log(`Full page screenshot saved: ${fullPagePath}`);

    // Get page dimensions
    const dimensions = await page.evaluate(() => {
      return {
        documentHeight: document.documentElement.scrollHeight,
        documentWidth: document.documentElement.scrollWidth,
        viewportHeight: window.innerHeight,
        viewportWidth: window.innerWidth
      };
    });

    console.log('\nPage dimensions:');
    console.log(`- Document: ${dimensions.documentWidth}x${dimensions.documentHeight}`);
    console.log(`- Viewport: ${dimensions.viewportWidth}x${dimensions.viewportHeight}`);

    await browser.close();
    console.log('\nScreenshots completed successfully!');

  } catch (error) {
    console.error('\nError:', error.message);
    console.error('\nFull error:', error);
  }
}

testScreenshot();