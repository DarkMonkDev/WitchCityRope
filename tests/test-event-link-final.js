const puppeteer = require('puppeteer');

async function testEventLinks() {
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
    await page.waitForSelector('a.event-card', { timeout: 10000 });
    
    // Get all event links
    const eventLinks = await page.evaluate(() => {
      const links = document.querySelectorAll('a.event-card');
      return Array.from(links).map(link => ({
        href: link.href,
        title: link.querySelector('.event-image-title')?.textContent
      }));
    });

    console.log(`\nFound ${eventLinks.length} event links:`);
    eventLinks.forEach((link, i) => {
      console.log(`${i + 1}. ${link.title} -> ${link.href}`);
    });

    if (eventLinks.length > 0) {
      console.log(`\n2. Clicking first event: ${eventLinks[0].title}`);
      
      // Click the first event link
      await page.click('a.event-card:first-child');
      
      // Wait for navigation
      await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 10000 });
      
      const newUrl = page.url();
      console.log(`\n3. Navigated to: ${newUrl}`);
      
      // Check result
      await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });
      
      const pageStatus = await page.evaluate(() => {
        const error = document.querySelector('.error-container h2');
        if (error) return `❌ ERROR: ${error.textContent}`;
        
        const title = document.querySelector('.event-title');
        if (title) return `✅ SUCCESS: Viewing "${title.textContent}"`;
        
        return '❓ Unknown state';
      });
      
      console.log(`\n4. Result: ${pageStatus}`);
    }

    // Test direct link as well
    console.log('\n5. Testing direct navigation to specific event...');
    await page.goto('http://localhost:5651/events/79272089-a274-45a4-ae08-a31f5edb0965', {
      waitUntil: 'networkidle2'
    });
    
    await page.waitForSelector('.event-detail-container, .error-container', { timeout: 10000 });
    
    const directStatus = await page.evaluate(() => {
      const error = document.querySelector('.error-container h2');
      if (error) return `❌ ERROR: ${error.textContent}`;
      
      const title = document.querySelector('.event-title');
      if (title) return `✅ SUCCESS: Viewing "${title.textContent}"`;
      
      return '❓ Unknown state';
    });
    
    console.log(`Direct navigation result: ${directStatus}`);

  } catch (error) {
    console.error('Test failed:', error.message);
  } finally {
    await browser.close();
  }
}

testEventLinks();