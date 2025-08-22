const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

/**
 * Takes a full-page screenshot of the WitchCityRope homepage
 * 
 * Available options:
 * - url: The URL to screenshot (default: http://localhost:5651/)
 * - viewport: Object with width and height (default: { width: 1920, height: 1080 })
 * - fullPage: Boolean to capture full page or just viewport (default: true)
 * - headless: Boolean to run browser in headless mode (default: true)
 * - timeout: Navigation timeout in milliseconds (default: 30000)
 * - waitForSelector: CSS selector to wait for before taking screenshot
 * - outputPath: Custom output path for the screenshot
 * - format: Screenshot format 'png' or 'jpeg' (default: 'png')
 * - quality: JPEG quality 0-100 (only for jpeg format)
 * - clip: Object with x, y, width, height to capture specific region
 */
async function takeHomepageScreenshot(options = {}) {
  const defaults = {
    url: 'http://localhost:5651/',
    viewport: { width: 1920, height: 1080 },
    fullPage: true,
    headless: true,
    timeout: 30000,
    format: 'png'
  };

  const config = { ...defaults, ...options };

  console.log('Starting Puppeteer with configuration:', config);
  
  // Try to use Windows Chrome installation if available
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
    headless: config.headless ? 'new' : false,
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu'
    ]
  };

  if (chromePath && !config.headless) {
    console.log(`Found Chrome at: ${chromePath}`);
    launchOptions.executablePath = chromePath;
  }

  try {
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewport(config.viewport);
    console.log(`Viewport set to: ${config.viewport.width}x${config.viewport.height}`);

    // Set up console logging
    page.on('console', msg => console.log('Page console:', msg.text()));
    page.on('pageerror', error => console.error('Page error:', error.message));
    page.on('response', response => {
      if (!response.ok() && !response.url().includes('favicon')) {
        console.warn(`Failed request: ${response.status()} ${response.url()}`);
      }
    });

    console.log(`Navigating to ${config.url}...`);
    
    try {
      const response = await page.goto(config.url, {
        waitUntil: 'networkidle2',
        timeout: config.timeout
      });

      if (response) {
        console.log(`Page loaded with status: ${response.status()}`);
      }
    } catch (navError) {
      console.error('Navigation error:', navError.message);
      console.log('Will attempt to take screenshot of current page state...');
    }

    // Wait for specific selector if provided
    if (config.waitForSelector) {
      try {
        console.log(`Waiting for selector: ${config.waitForSelector}`);
        await page.waitForSelector(config.waitForSelector, { timeout: 5000 });
      } catch (e) {
        console.warn(`Selector '${config.waitForSelector}' not found within timeout`);
      }
    }

    // Additional wait for any dynamic content
    await page.waitForTimeout(2000);

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }

    // Determine output path
    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
    const defaultFilename = `wcr-homepage-${timestamp}.${config.format}`;
    const screenshotPath = config.outputPath || path.join(screenshotsDir, defaultFilename);

    // Screenshot options
    const screenshotOptions = {
      path: screenshotPath,
      fullPage: config.fullPage,
      type: config.format
    };

    if (config.quality && config.format === 'jpeg') {
      screenshotOptions.quality = config.quality;
    }

    if (config.clip) {
      screenshotOptions.clip = config.clip;
      screenshotOptions.fullPage = false; // Clip overrides fullPage
    }

    // Take screenshot
    console.log('Taking screenshot...');
    await page.screenshot(screenshotOptions);
    console.log(`Screenshot saved to: ${screenshotPath}`);

    // Get page metrics
    const metrics = await page.metrics();
    console.log('\nPage Metrics:');
    console.log(`- DOM Content Loaded: ${metrics.DOMContentLoaded}ms`);
    console.log(`- Load Event: ${metrics.Load}ms`);
    console.log(`- JS Heap Used: ${(metrics.JSHeapUsedSize / 1048576).toFixed(2)}MB`);

    // Get page information
    const pageInfo = await page.evaluate(() => {
      return {
        title: document.title,
        url: window.location.href,
        viewport: {
          width: window.innerWidth,
          height: window.innerHeight
        },
        documentSize: {
          width: document.documentElement.scrollWidth,
          height: document.documentElement.scrollHeight
        },
        hasContent: document.body.innerHTML.length > 0,
        elementCounts: {
          forms: document.querySelectorAll('form').length,
          inputs: document.querySelectorAll('input').length,
          buttons: document.querySelectorAll('button').length,
          links: document.querySelectorAll('a').length,
          images: document.querySelectorAll('img').length,
          headings: document.querySelectorAll('h1, h2, h3, h4, h5, h6').length
        }
      };
    });

    console.log('\nPage Information:');
    console.log(`- Title: ${pageInfo.title}`);
    console.log(`- URL: ${pageInfo.url}`);
    console.log(`- Viewport: ${pageInfo.viewport.width}x${pageInfo.viewport.height}`);
    console.log(`- Document Size: ${pageInfo.documentSize.width}x${pageInfo.documentSize.height}`);
    console.log(`- Has Content: ${pageInfo.hasContent}`);
    console.log('- Element Counts:', pageInfo.elementCounts);

    // Check for common issues
    const issues = await page.evaluate(() => {
      const problems = [];
      
      // Check for broken images
      const images = document.querySelectorAll('img');
      images.forEach(img => {
        if (!img.complete || img.naturalWidth === 0) {
          problems.push(`Broken image: ${img.src}`);
        }
      });

      // Check for error messages
      const errorSelectors = ['.error', '.alert-danger', '.error-message', '[class*="error"]'];
      errorSelectors.forEach(selector => {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => {
          if (el.textContent.trim() && el.offsetParent !== null) {
            problems.push(`Error message found: ${el.textContent.trim()}`);
          }
        });
      });

      return problems;
    });

    if (issues.length > 0) {
      console.log('\nPotential Issues Found:');
      issues.forEach(issue => console.log(`- ${issue}`));
    }

    await browser.close();
    console.log('\nBrowser closed successfully.');

    return {
      success: true,
      screenshotPath,
      pageInfo,
      metrics,
      issues
    };

  } catch (error) {
    console.error('Error:', error.message);
    return {
      success: false,
      error: error.message
    };
  }
}

// Command-line usage
if (require.main === module) {
  // Parse command line arguments
  const args = process.argv.slice(2);
  const options = {};

  for (let i = 0; i < args.length; i += 2) {
    const key = args[i].replace('--', '');
    const value = args[i + 1];

    switch (key) {
      case 'url':
        options.url = value;
        break;
      case 'width':
        options.viewport = options.viewport || {};
        options.viewport.width = parseInt(value);
        break;
      case 'height':
        options.viewport = options.viewport || {};
        options.viewport.height = parseInt(value);
        break;
      case 'fullPage':
        options.fullPage = value === 'true';
        break;
      case 'headless':
        options.headless = value === 'true';
        break;
      case 'format':
        options.format = value;
        break;
      case 'quality':
        options.quality = parseInt(value);
        break;
      case 'output':
        options.outputPath = value;
        break;
      case 'wait':
        options.waitForSelector = value;
        break;
    }
  }

  console.log('Running homepage screenshot with options:', options);
  takeHomepageScreenshot(options).catch(console.error);
}

module.exports = takeHomepageScreenshot;