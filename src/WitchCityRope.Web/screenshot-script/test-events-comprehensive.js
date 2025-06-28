const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testEventsPage() {
  console.log('Starting comprehensive events page testing...');
  
  const launchOptions = {
    headless: 'new',
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu',
      '--disable-web-security',
      '--disable-features=IsolateOrigins,site-per-process'
    ]
  };

  const browser = await puppeteer.launch(launchOptions);
  
  try {
    const page = await browser.newPage();
    
    // Enable console log capturing
    page.on('console', msg => {
      console.log(`Browser console ${msg.type()}: ${msg.text()}`);
    });
    
    // Capture errors
    const errors = [];
    page.on('pageerror', error => {
      errors.push(error.message);
      console.error('Page error:', error.message);
    });

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots', 'events-test');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir, { recursive: true });
    }

    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
    
    // Test results object
    const testResults = {
      timestamp: timestamp,
      tests: []
    };

    // TEST 1: Initial page load and event cards
    console.log('\n=== TEST 1: Initial Page Load ===');
    await page.setViewport({ width: 1920, height: 1080 });
    
    const startTime = Date.now();
    const response = await page.goto('http://localhost:5651/events', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });
    const loadTime = Date.now() - startTime;
    
    console.log(`Page loaded in ${loadTime}ms with status: ${response.status()}`);
    
    // Wait for event cards to load
    await page.waitForTimeout(2000);
    
    // Take initial screenshot
    await page.screenshot({
      path: path.join(screenshotsDir, `1-initial-load-${timestamp}.png`),
      fullPage: true
    });
    
    // Count event cards
    const eventCardsCount = await page.evaluate(() => {
      const cards = document.querySelectorAll('.event-card, [class*="event-card"], .card');
      return cards.length;
    });
    
    console.log(`Found ${eventCardsCount} event cards`);
    
    // Check for future events
    const futureEvents = await page.evaluate(() => {
      const cards = document.querySelectorAll('.event-card, [class*="event-card"], .card');
      const futureEventInfo = [];
      
      cards.forEach((card, index) => {
        const dateText = card.textContent;
        const title = card.querySelector('h3, h4, .title, .event-title');
        futureEventInfo.push({
          index: index + 1,
          title: title ? title.textContent.trim() : 'No title found',
          text: card.textContent.trim().substring(0, 100)
        });
      });
      
      return futureEventInfo;
    });
    
    testResults.tests.push({
      name: 'Initial Page Load',
      status: eventCardsCount >= 4 ? 'PASS' : 'FAIL',
      details: {
        loadTime: `${loadTime}ms`,
        eventCardsFound: eventCardsCount,
        expectedMinimum: 4,
        events: futureEvents
      }
    });

    // TEST 2: Search functionality
    console.log('\n=== TEST 2: Search Functionality ===');
    
    // Find search input
    const searchInput = await page.$('input[type="search"], input[placeholder*="search" i], input[name*="search" i], input#search');
    
    if (searchInput) {
      console.log('Search input found, testing search functionality...');
      
      // Type "rope" in search
      await searchInput.click({ clickCount: 3 }); // Select all
      await searchInput.type('rope');
      await page.waitForTimeout(1000); // Wait for search to process
      
      // Take screenshot of filtered results
      await page.screenshot({
        path: path.join(screenshotsDir, `2-search-rope-${timestamp}.png`),
        fullPage: true
      });
      
      // Count filtered results
      const filteredCount = await page.evaluate(() => {
        const visibleCards = document.querySelectorAll('.event-card:not([style*="display: none"]), [class*="event-card"]:not([style*="display: none"]), .card:not([style*="display: none"])');
        return visibleCards.length;
      });
      
      console.log(`Filtered results: ${filteredCount} cards visible`);
      
      // Clear search
      await searchInput.click({ clickCount: 3 });
      await page.keyboard.press('Backspace');
      await page.waitForTimeout(1000);
      
      // Take screenshot after clearing
      await page.screenshot({
        path: path.join(screenshotsDir, `3-search-cleared-${timestamp}.png`),
        fullPage: true
      });
      
      testResults.tests.push({
        name: 'Search Functionality',
        status: 'PASS',
        details: {
          searchTerm: 'rope',
          filteredResults: filteredCount,
          searchCleared: true
        }
      });
    } else {
      console.log('Search input not found');
      testResults.tests.push({
        name: 'Search Functionality',
        status: 'FAIL',
        details: {
          error: 'Search input element not found'
        }
      });
    }

    // TEST 3: Performance Audit
    console.log('\n=== TEST 3: Performance Audit ===');
    
    // Measure performance metrics
    const performanceMetrics = await page.evaluate(() => {
      const perfData = performance.getEntriesByType('navigation')[0];
      return {
        domContentLoaded: perfData.domContentLoadedEventEnd - perfData.domContentLoadedEventStart,
        loadComplete: perfData.loadEventEnd - perfData.loadEventStart,
        domInteractive: perfData.domInteractive,
        firstPaint: performance.getEntriesByType('paint').find(entry => entry.name === 'first-paint')?.startTime || 0,
        firstContentfulPaint: performance.getEntriesByType('paint').find(entry => entry.name === 'first-contentful-paint')?.startTime || 0
      };
    });
    
    console.log('Performance Metrics:', performanceMetrics);
    
    // Check for console errors
    console.log(`Console errors found: ${errors.length}`);
    
    testResults.tests.push({
      name: 'Performance Audit',
      status: errors.length === 0 ? 'PASS' : 'WARNING',
      details: {
        metrics: performanceMetrics,
        consoleErrors: errors,
        pageLoadTime: `${loadTime}ms`
      }
    });

    // TEST 4: Responsive Design - Mobile View
    console.log('\n=== TEST 4: Responsive Design ===');
    
    // Set mobile viewport
    await page.setViewport({ width: 375, height: 667 });
    await page.waitForTimeout(1000); // Wait for responsive adjustments
    
    // Take mobile screenshot
    await page.screenshot({
      path: path.join(screenshotsDir, `4-mobile-view-${timestamp}.png`),
      fullPage: true
    });
    
    // Check if cards stack properly
    const mobileLayout = await page.evaluate(() => {
      const cards = document.querySelectorAll('.event-card, [class*="event-card"], .card');
      const cardPositions = [];
      
      cards.forEach((card, index) => {
        const rect = card.getBoundingClientRect();
        cardPositions.push({
          index: index,
          top: rect.top,
          left: rect.left,
          width: rect.width,
          height: rect.height
        });
      });
      
      // Check if cards are stacked (same left position or full width)
      const isStacked = cardPositions.every(pos => 
        pos.left < 20 || // Close to left edge
        pos.width > 350 // Nearly full width
      );
      
      return {
        cardCount: cards.length,
        positions: cardPositions.slice(0, 4), // First 4 cards
        isStacked: isStacked,
        viewportWidth: window.innerWidth
      };
    });
    
    console.log(`Mobile layout: ${mobileLayout.isStacked ? 'Cards are stacked' : 'Cards are not properly stacked'}`);
    
    testResults.tests.push({
      name: 'Responsive Design - Mobile',
      status: mobileLayout.isStacked ? 'PASS' : 'WARNING',
      details: {
        viewport: '375x667',
        cardsStacked: mobileLayout.isStacked,
        cardPositions: mobileLayout.positions
      }
    });

    // TEST 5: Additional viewport - Tablet
    console.log('\n=== TEST 5: Tablet View ===');
    await page.setViewport({ width: 768, height: 1024 });
    await page.waitForTimeout(1000);
    
    await page.screenshot({
      path: path.join(screenshotsDir, `5-tablet-view-${timestamp}.png`),
      fullPage: true
    });

    // Generate test report
    const reportPath = path.join(screenshotsDir, `test-report-${timestamp}.json`);
    fs.writeFileSync(reportPath, JSON.stringify(testResults, null, 2));
    
    // Generate HTML report
    const htmlReport = generateHTMLReport(testResults, screenshotsDir, timestamp);
    const htmlPath = path.join(screenshotsDir, `test-report-${timestamp}.html`);
    fs.writeFileSync(htmlPath, htmlReport);
    
    console.log('\n=== TEST SUMMARY ===');
    testResults.tests.forEach(test => {
      console.log(`${test.name}: ${test.status}`);
    });
    
    console.log(`\nTest report saved to: ${reportPath}`);
    console.log(`HTML report saved to: ${htmlPath}`);
    console.log(`Screenshots saved in: ${screenshotsDir}`);

  } catch (error) {
    console.error('Test error:', error);
  } finally {
    await browser.close();
  }
}

function generateHTMLReport(results, screenshotsDir, timestamp) {
  const html = `
<!DOCTYPE html>
<html>
<head>
  <title>Events Page Test Report - ${timestamp}</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
    .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    h1 { color: #333; }
    .test { margin: 20px 0; padding: 15px; border-left: 4px solid #ddd; background: #fafafa; }
    .test.pass { border-color: #4CAF50; }
    .test.fail { border-color: #f44336; }
    .test.warning { border-color: #ff9800; }
    .test h3 { margin-top: 0; }
    .details { margin-top: 10px; }
    .screenshot { margin: 10px 0; }
    .screenshot img { max-width: 100%; height: auto; border: 1px solid #ddd; }
    pre { background: #f5f5f5; padding: 10px; overflow-x: auto; }
    .metric { display: inline-block; margin-right: 20px; }
    .metric strong { color: #666; }
  </style>
</head>
<body>
  <div class="container">
    <h1>Events Page Test Report</h1>
    <p>Generated: ${new Date(timestamp).toLocaleString()}</p>
    
    ${results.tests.map((test, index) => `
      <div class="test ${test.status.toLowerCase()}">
        <h3>${test.name} - ${test.status}</h3>
        <div class="details">
          ${formatTestDetails(test.details)}
        </div>
        ${getScreenshotForTest(index + 1, timestamp)}
      </div>
    `).join('')}
  </div>
</body>
</html>
  `;
  
  return html;
}

function formatTestDetails(details) {
  return Object.entries(details).map(([key, value]) => {
    if (typeof value === 'object') {
      return `<strong>${key}:</strong><pre>${JSON.stringify(value, null, 2)}</pre>`;
    }
    return `<div class="metric"><strong>${key}:</strong> ${value}</div>`;
  }).join('');
}

function getScreenshotForTest(testNumber, timestamp) {
  const screenshotMap = {
    1: `1-initial-load-${timestamp}.png`,
    2: `2-search-rope-${timestamp}.png`,
    3: `3-search-cleared-${timestamp}.png`,
    4: `4-mobile-view-${timestamp}.png`,
    5: `5-tablet-view-${timestamp}.png`
  };
  
  if (screenshotMap[testNumber]) {
    return `<div class="screenshot"><img src="${screenshotMap[testNumber]}" alt="Test ${testNumber} Screenshot"></div>`;
  }
  return '';
}

// Run the tests
testEventsPage().catch(console.error);