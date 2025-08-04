const puppeteer = require('puppeteer');

async function checkEventHTML() {
  const browser = await puppeteer.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  try {
    const page = await browser.newPage();
    
    console.log('Loading home page...');
    await page.goto('http://localhost:5651', {
      waitUntil: 'networkidle2',
      timeout: 30000
    });

    // Wait for events to load
    await page.waitForSelector('.event-card', { timeout: 10000 });
    
    // Get the HTML of the first event card
    const eventCardHTML = await page.evaluate(() => {
      const firstCard = document.querySelector('.event-card');
      return firstCard ? firstCard.outerHTML.substring(0, 500) : 'No event card found';
    });

    console.log('First event card HTML:');
    console.log(eventCardHTML);

    // Check if it's a link or has onclick
    const linkInfo = await page.evaluate(() => {
      const firstCard = document.querySelector('.event-card');
      if (!firstCard) return { found: false };

      const parentLink = firstCard.closest('a');
      const hasOnClick = firstCard.hasAttribute('onclick');
      
      return {
        found: true,
        isWrappedInLink: !!parentLink,
        linkHref: parentLink?.href,
        hasOnClick: hasOnClick,
        onclick: firstCard.getAttribute('onclick')
      };
    });

    console.log('\nLink info:', linkInfo);

    // Try clicking
    if (linkInfo.isWrappedInLink) {
      console.log('\nClicking parent link...');
      await page.click('a .event-card');
    } else {
      console.log('\nClicking event card...');
      await page.click('.event-card');
    }

    // Wait for navigation
    await new Promise(resolve => setTimeout(resolve, 2000));

    const newUrl = page.url();
    console.log(`\nNavigated to: ${newUrl}`);

  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
}

checkEventHTML();