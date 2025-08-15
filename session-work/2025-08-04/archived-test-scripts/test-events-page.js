const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing Events Page ===\n');
    
    // 1. Go to events page
    console.log('1. Navigating to events page...');
    await page.goto('http://localhost:5651/events', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);
    
    // Check for events
    const eventCards = await page.locator('.event-card, .card, [class*="event"]').count();
    console.log('Event cards found: ' + eventCards);
    
    // Check for event titles
    const eventTitles = await page.locator('h3, h4, .event-title').allTextContents();
    console.log('\nEvent titles found:');
    eventTitles.forEach(title => {
      if (title.trim()) console.log('  - ' + title.trim());
    });
    
    // Check for no events message
    const noEventsMessage = await page.locator('text=/no events|no upcoming events/i').count();
    console.log('\nNo events message: ' + (noEventsMessage > 0 ? 'Present' : 'Not present'));
    
    // Take screenshot
    await page.screenshot({ path: 'events-page.png' });
    
    console.log('\n✅ Screenshot saved as events-page.png');
    console.log('\nEvents loading: ' + (eventCards > 0 ? 'WORKING' : 'NOT WORKING'));
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();