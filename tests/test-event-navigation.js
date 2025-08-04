const puppeteer = require('puppeteer');

async function testEventNavigation() {
  const browser = await puppeteer.launch({
    headless: false,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  try {
    const page = await browser.newPage();
    
    console.log('1. Loading home page...');
    await page.goto('http://localhost:5651', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });

    // Wait for events to load
    await page.waitForSelector('.event-card', { timeout: 10000 });
    
    // Get event info before clicking
    const eventInfo = await page.evaluate(() => {
      const firstCard = document.querySelector('.event-card');
      if (!firstCard) return null;
      
      // Find the onclick handler to get the event ID
      const onclickAttr = firstCard.getAttribute('onclick');
      const match = onclickAttr ? onclickAttr.match(/NavigateToEvent\('([^']+)'\)/) : null;
      
      return {
        title: firstCard.querySelector('.event-image-title')?.textContent,
        hasOnClick: !!onclickAttr,
        eventId: match ? match[1] : null
      };
    });

    console.log('Event info:', eventInfo);

    // Click using Blazor's onclick
    console.log('2. Clicking event card...');
    await page.evaluate(() => {
      const firstCard = document.querySelector('.event-card');
      if (firstCard) firstCard.click();
    });

    // Wait a bit for Blazor navigation
    await page.waitForTimeout(2000);

    const newUrl = page.url();
    console.log(`3. Current URL: ${newUrl}`);

    if (newUrl.includes('/events/')) {
      // Wait for content
      await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });
      
      const result = await page.evaluate(() => {
        const error = document.querySelector('.error-container h2');
        if (error) return { success: false, error: error.textContent };
        
        const title = document.querySelector('.event-title');
        if (title) return { success: true, title: title.textContent };
        
        return { success: false, error: 'Unknown state' };
      });

      if (result.success) {
        console.log(`✅ Navigation successful! Viewing: ${result.title}`);
      } else {
        console.log(`❌ Navigation failed: ${result.error}`);
      }
    } else {
      console.log('❌ Did not navigate to event page');
    }

  } catch (error) {
    console.error('Test error:', error.message);
  } finally {
    await browser.close();
  }
}

testEventNavigation();