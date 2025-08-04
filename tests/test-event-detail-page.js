const puppeteer = require('puppeteer');

async function testEventDetailPage() {
  const browser = await puppeteer.launch({
    headless: false,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  try {
    const page = await browser.newPage();
    
    // Enable console logging
    page.on('console', msg => console.log('Browser console:', msg.text()));
    page.on('pageerror', error => console.log('Page error:', error.message));

    console.log('1. Loading home page...');
    await page.goto('http://localhost:5651', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });

    // Wait for events to load
    await page.waitForSelector('.event-card', { timeout: 10000 });
    
    // Get all event cards
    const eventCards = await page.$$('.event-card');
    console.log(`Found ${eventCards.length} events on home page`);

    if (eventCards.length > 0) {
      // Get the first event's details before clicking
      const firstEventTitle = await page.evaluate(() => {
        const firstCard = document.querySelector('.event-card');
        return firstCard ? firstCard.querySelector('.event-image-title')?.textContent : null;
      });
      console.log(`First event title: ${firstEventTitle}`);

      // Click the first event
      console.log('2. Clicking on first event...');
      await eventCards[0].click();

      // Wait for navigation
      await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 30000 });

      const currentUrl = page.url();
      console.log(`3. Navigated to: ${currentUrl}`);

      // Check if we're on an event detail page
      if (currentUrl.includes('/events/')) {
        // Wait for either the event detail content or the error message
        await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });

        // Check what loaded
        const hasEventDetail = await page.$('.event-detail-container') !== null;
        const hasError = await page.$('.error-container') !== null;

        if (hasEventDetail) {
          console.log('✅ Event detail page loaded successfully!');
          
          // Get event details
          const eventTitle = await page.$eval('.event-title', el => el.textContent);
          const eventDate = await page.$eval('.event-datetime', el => el.textContent);
          const eventLocation = await page.$eval('.location-address', el => el.textContent);
          
          console.log(`Event Title: ${eventTitle}`);
          console.log(`Event Date: ${eventDate}`);
          console.log(`Event Location: ${eventLocation}`);
        } else if (hasError) {
          console.log('❌ Event Not Found error displayed');
          const errorMessage = await page.$eval('.error-container h2', el => el.textContent);
          console.log(`Error: ${errorMessage}`);
        }
      } else {
        console.log('❌ Did not navigate to event detail page');
      }
    }

    // Also test the specific event URL directly
    console.log('\n4. Testing specific event URL directly...');
    await page.goto('http://localhost:5651/events/79272089-a274-45a4-ae08-a31f5edb0965', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });

    // Wait for content to load
    await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });

    const hasEventDetail = await page.$('.event-detail-container') !== null;
    const hasError = await page.$('.error-container') !== null;

    if (hasEventDetail) {
      console.log('✅ Direct URL works - Event detail page loaded!');
      const eventTitle = await page.$eval('.event-title', el => el.textContent);
      console.log(`Event Title: ${eventTitle}`);
    } else if (hasError) {
      console.log('❌ Direct URL shows Event Not Found error');
    }

    // Take a screenshot for debugging
    await page.screenshot({ path: 'event-detail-test.png', fullPage: true });
    console.log('Screenshot saved as event-detail-test.png');

  } catch (error) {
    console.error('Test failed:', error);
  } finally {
    await browser.close();
  }
}

// Run the test
testEventDetailPage();