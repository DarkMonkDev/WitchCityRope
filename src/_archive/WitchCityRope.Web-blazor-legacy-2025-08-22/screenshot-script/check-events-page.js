const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function checkEventsPage() {
  console.log('Starting Puppeteer to check events page...');
  
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
    headless: 'new', // Use new headless mode
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu',
      '--disable-web-security',
      '--disable-features=IsolateOrigins,site-per-process'
    ]
  };

  // Don't use Windows Chrome in WSL, let Puppeteer use its bundled Chromium
  // if (chromePath) {
  //   console.log(`Found Chrome at: ${chromePath}`);
  //   launchOptions.executablePath = chromePath;
  // }

  try {
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewport({
      width: 1920,
      height: 1080
    });

    console.log('Navigating to http://localhost:5651/events...');
    
    try {
      const response = await page.goto('http://localhost:5651/events', {
        waitUntil: 'networkidle2',
        timeout: 30000
      });

      if (response) {
        console.log(`Page loaded with status: ${response.status()}`);
      }
    } catch (navError) {
      console.error('Navigation error:', navError.message);
    }

    // Wait a bit for any dynamic content
    await page.waitForTimeout(3000);

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }

    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
    const screenshotPath = path.join(screenshotsDir, `events-page-${timestamp}.png`);

    // Take screenshot
    console.log('Taking screenshot...');
    await page.screenshot({
      path: screenshotPath,
      fullPage: true
    });

    console.log(`Screenshot saved to: ${screenshotPath}`);

    // Check for specific elements
    console.log('\nChecking for page elements...');
    
    const analysis = await page.evaluate(() => {
      const results = {
        title: document.title,
        url: window.location.href,
        hasContent: document.body.innerHTML.length > 0,
        elements: {
          eventCards: [],
          searchIcon: null,
          layoutIssues: []
        }
      };

      // Check for event cards
      const eventCards = document.querySelectorAll('.event-card, [class*="event"], [class*="card"]');
      if (eventCards.length > 0) {
        eventCards.forEach((card, index) => {
          const cardInfo = {
            index: index + 1,
            classes: card.className,
            hasImage: !!card.querySelector('img'),
            text: card.textContent.trim().substring(0, 100) + '...'
          };
          results.elements.eventCards.push(cardInfo);
        });
      }

      // Check for search icon/functionality
      const searchElements = document.querySelectorAll('[class*="search"], [id*="search"], input[type="search"], button[aria-label*="search"], svg[class*="search"]');
      if (searchElements.length > 0) {
        results.elements.searchIcon = {
          found: true,
          count: searchElements.length,
          types: Array.from(searchElements).map(el => el.tagName.toLowerCase())
        };
      } else {
        results.elements.searchIcon = { found: false };
      }

      // Check for potential layout issues
      const allElements = document.querySelectorAll('*');
      allElements.forEach(el => {
        const rect = el.getBoundingClientRect();
        const styles = window.getComputedStyle(el);
        
        // Check for elements that might be cut off
        if (rect.right > window.innerWidth || rect.bottom > window.innerHeight) {
          results.elements.layoutIssues.push({
            element: el.tagName + (el.className ? '.' + el.className : ''),
            issue: 'Element extends beyond viewport',
            position: { right: rect.right, bottom: rect.bottom }
          });
        }
        
        // Check for overlapping elements
        if (styles.position === 'absolute' && (rect.width === 0 || rect.height === 0)) {
          results.elements.layoutIssues.push({
            element: el.tagName + (el.className ? '.' + el.className : ''),
            issue: 'Absolute positioned element with zero dimensions'
          });
        }
      });

      return results;
    });

    // Generate report
    console.log('\n=== EVENTS PAGE ANALYSIS REPORT ===');
    console.log(`Page Title: ${analysis.title}`);
    console.log(`URL: ${analysis.url}`);
    console.log(`Has Content: ${analysis.hasContent}`);
    
    console.log('\n--- Event Cards ---');
    if (analysis.elements.eventCards.length > 0) {
      console.log(`Found ${analysis.elements.eventCards.length} event card(s):`);
      analysis.elements.eventCards.forEach(card => {
        console.log(`  Card ${card.index}:`);
        console.log(`    Classes: ${card.classes}`);
        console.log(`    Has Image: ${card.hasImage}`);
        console.log(`    Text Preview: ${card.text}`);
      });
    } else {
      console.log('No event cards found. This could indicate:');
      console.log('  - Mock data is not loading');
      console.log('  - Different class names are being used');
      console.log('  - Page structure is different than expected');
    }
    
    console.log('\n--- Search Functionality ---');
    if (analysis.elements.searchIcon.found) {
      console.log(`Search elements found: ${analysis.elements.searchIcon.count}`);
      console.log(`Element types: ${analysis.elements.searchIcon.types.join(', ')}`);
    } else {
      console.log('No search icon/functionality detected');
    }
    
    console.log('\n--- Layout Issues ---');
    if (analysis.elements.layoutIssues.length > 0) {
      console.log(`Found ${analysis.elements.layoutIssues.length} potential layout issue(s):`);
      analysis.elements.layoutIssues.slice(0, 5).forEach(issue => {
        console.log(`  - ${issue.element}: ${issue.issue}`);
      });
    } else {
      console.log('No obvious layout issues detected');
    }

    // Save the analysis report
    const reportPath = path.join(screenshotsDir, `events-page-analysis-${timestamp}.json`);
    fs.writeFileSync(reportPath, JSON.stringify(analysis, null, 2));
    console.log(`\nDetailed analysis saved to: ${reportPath}`);

    // Keep browser open for 5 seconds so you can see the page
    // console.log('\nKeeping browser open for 5 seconds...');
    // await page.waitForTimeout(5000);

    await browser.close();
    console.log('\nBrowser closed.');

  } catch (error) {
    console.error('Error:', error.message);
  }
}

checkEventsPage().catch(console.error);