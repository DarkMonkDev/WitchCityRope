const puppeteer = require('puppeteer');

async function testEventDetail() {
  const browser = await puppeteer.launch({
    headless: false,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  try {
    const page = await browser.newPage();
    
    console.log('Testing event detail page...');
    await page.goto('http://localhost:5651/events/79272089-a274-45a4-ae08-a31f5edb0965', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });

    // Wait for either success or error
    await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });

    // Check what loaded
    const pageContent = await page.evaluate(() => {
      const errorContainer = document.querySelector('.error-container');
      if (errorContainer) {
        return {
          success: false,
          error: errorContainer.querySelector('h2')?.textContent || 'Unknown error'
        };
      }

      const eventTitle = document.querySelector('.event-title')?.textContent;
      const eventDetailContainer = document.querySelector('.event-detail-container');
      
      if (eventDetailContainer && eventTitle) {
        // Get all text content from the detail container to see what's there
        const allText = eventDetailContainer.textContent;
        return {
          success: true,
          title: eventTitle,
          hasContent: allText.length > 100,
          contentPreview: allText.substring(0, 200).replace(/\s+/g, ' ').trim()
        };
      }

      return {
        success: false,
        error: 'No event detail found'
      };
    });

    if (pageContent.success) {
      console.log('✅ SUCCESS: Event detail page loaded!');
      console.log(`Title: ${pageContent.title}`);
      console.log(`Has content: ${pageContent.hasContent}`);
      console.log(`Content preview: ${pageContent.contentPreview}...`);
    } else {
      console.log('❌ FAILED: ' + pageContent.error);
    }

    // Take screenshot
    await page.screenshot({ path: 'event-detail-result.png', fullPage: true });
    console.log('\nScreenshot saved as event-detail-result.png');

  } catch (error) {
    console.error('Test error:', error.message);
  } finally {
    await browser.close();
  }
}

testEventDetail();