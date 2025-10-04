import { test, expect } from '@playwright/test';

test.describe('Event Routing Debug Investigation', () => {
  let apiCalls: { url: string, method: string, response?: any }[] = [];

  test.beforeEach(async ({ page }) => {
    // Clear previous API calls
    apiCalls = [];
    
    // Monitor API calls
    page.on('response', async (response) => {
      if (response.url().includes('/api/events') || response.url().includes('/api/event/')) {
        const url = response.url();
        const method = response.request().method();
        try {
          const data = await response.json();
          apiCalls.push({ url, method, response: data });
          console.log(`API Call: ${method} ${url}`);
          console.log('Response:', JSON.stringify(data, null, 2));
        } catch (error) {
          apiCalls.push({ url, method });
          console.log(`API Call: ${method} ${url} - Could not parse JSON`);
        }
      }
    });

    // Monitor network failures
    page.on('requestfailed', request => {
      if (request.url().includes('/api/')) {
        console.log(`❌ Failed request: ${request.method()} ${request.url()}`);
      }
    });
  });

  test('investigate February rope jam routing issue', async ({ page }) => {
    console.log('🔍 Starting Event Routing Investigation...');
    
    // Step 1: Navigate to events page
    console.log('1. Navigating to events page...');
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of events page
    await page.screenshot({ path: 'test-results/events-page-investigation.png', fullPage: true });
    
    // Step 2: Look for any event cards or listings
    console.log('2. Checking for event listings...');
    const eventCards = await page.locator('[data-testid*="event"], .event-card, .card, [class*="event"]').all();
    console.log(`Found ${eventCards.length} potential event elements`);
    
    // Step 3: Look for any text containing "February" or "rope jam"
    console.log('3. Searching for February rope jam...');
    const februaryElements = await page.locator('text=/february/i').all();
    const ropeJamElements = await page.locator('text=/rope.*jam/i').all();
    
    console.log(`February mentions: ${februaryElements.length}`);
    console.log(`Rope jam mentions: ${ropeJamElements.length}`);
    
    // Step 4: Get all visible text to see what events are actually displayed
    console.log('4. Getting all visible event text...');
    const pageText = await page.textContent('body');
    const eventTitles = [
      'Introduction to Rope Bondage',
      'Midnight Rope Performance', 
      'Monthly Rope Social',
      'Advanced Suspension Techniques',
      'February rope jam'
    ];
    
    for (const title of eventTitles) {
      const found = pageText?.toLowerCase().includes(title.toLowerCase());
      console.log(`"${title}": ${found ? '✅ FOUND' : '❌ NOT FOUND'}`);
    }
    
    // Step 5: Check if we can find any clickable links
    console.log('5. Looking for clickable event links...');
    const links = await page.locator('a[href*="event"], button[class*="event"], [data-testid*="event"]').all();
    
    for (let i = 0; i < links.length; i++) {
      try {
        const href = await links[i].getAttribute('href');
        const text = await links[i].textContent();
        const role = await links[i].getAttribute('role');
        console.log(`Link ${i + 1}: href="${href}" text="${text?.substring(0, 50)}..." role="${role}"`);
      } catch (error) {
        console.log(`Link ${i + 1}: Could not read attributes`);
      }
    }
    
    // Step 6: If we find an event that mentions February, click it
    if (februaryElements.length > 0 || ropeJamElements.length > 0) {
      console.log('6. Found February/rope jam reference - attempting to click...');
      
      try {
        // Try to find a clickable parent element
        const clickableElement = februaryElements.length > 0 
          ? februaryElements[0].locator('xpath=ancestor-or-self::*[self::a or self::button or @role="button" or @data-testid]').first()
          : ropeJamElements[0].locator('xpath=ancestor-or-self::*[self::a or self::button or @role="button" or @data-testid]').first();
        
        const isVisible = await clickableElement.isVisible();
        if (isVisible) {
          const beforeUrl = page.url();
          console.log(`Current URL before click: ${beforeUrl}`);
          
          await clickableElement.click();
          await page.waitForLoadState('networkidle', { timeout: 10000 });
          
          const afterUrl = page.url();
          console.log(`URL after click: ${afterUrl}`);
          
          // Take screenshot of the resulting page
          await page.screenshot({ path: 'test-results/after-february-click.png', fullPage: true });
          
          // Check what event details are shown
          const detailsTitle = await page.locator('h1, h2, h3').first().textContent();
          const detailsContent = await page.textContent('main, .content, body');
          
          console.log(`Details page title: "${detailsTitle}"`);
          console.log('Details URL params:', afterUrl.split('/').pop());
          
          // Check if the event details match what was clicked
          const isCorrectEvent = detailsContent?.toLowerCase().includes('february') || 
                                 detailsContent?.toLowerCase().includes('rope jam');
          
          console.log(`Does details page show February rope jam? ${isCorrectEvent ? '✅ YES' : '❌ NO'}`);
          
          if (!isCorrectEvent) {
            console.log('❌ ROUTING ISSUE CONFIRMED: Clicked February rope jam but got different event');
            console.log('Actual details shown:', detailsTitle);
          }
        }
      } catch (error) {
        console.log('Could not click February element:', error);
      }
    } else {
      console.log('6. No February rope jam found on page - checking what events are actually displayed');
      
      // Try clicking the first available event link to see routing behavior
      if (links.length > 0) {
        console.log('Attempting to click first available event link...');
        const firstLink = links[0];
        const linkText = await firstLink.textContent();
        const href = await firstLink.getAttribute('href');
        
        console.log(`Clicking link with text: "${linkText}" href: "${href}"`);
        
        const beforeUrl = page.url();
        await firstLink.click();
        await page.waitForLoadState('networkidle', { timeout: 10000 });
        
        const afterUrl = page.url();
        console.log(`Navigation: ${beforeUrl} → ${afterUrl}`);
        
        // Take screenshot
        await page.screenshot({ path: 'test-results/first-event-click-result.png', fullPage: true });
        
        // Check what details are shown
        const detailsTitle = await page.locator('h1, h2, h3').first().textContent();
        console.log(`Event details page shows: "${detailsTitle}"`);
      }
    }
    
    // Step 7: Report findings
    console.log('\n🔍 INVESTIGATION SUMMARY:');
    console.log(`Total API calls made: ${apiCalls.length}`);
    console.log(`Events API responses: ${apiCalls.filter(call => call.url.includes('/api/events')).length}`);
    console.log(`Event details API calls: ${apiCalls.filter(call => call.url.includes('/event/')).length}`);
    
    if (apiCalls.length === 0) {
      console.log('⚠️ NO API CALLS DETECTED - Possible issues:');
      console.log('  - Frontend using mock data instead of API');
      console.log('  - API endpoints not configured correctly');
      console.log('  - CORS blocking API calls');
    }
  });

  test('verify events API returns expected data structure', async ({ page }) => {
    console.log('🔍 API Data Structure Verification...');
    
    // Direct API test
    const response = await page.request.get('http://localhost:5655/api/events');
    const eventsData = await response.json();
    
    console.log('Events API Response Structure:');
    console.log(`Total events: ${eventsData.totalCount}`);
    console.log(`Events returned: ${eventsData.events?.length || 0}`);
    
    if (eventsData.events && Array.isArray(eventsData.events)) {
      console.log('\nEvent Details:');
      eventsData.events.forEach((event, index) => {
        console.log(`${index + 1}. ID: ${event.id}`);
        console.log(`   Title: ${event.title}`);
        console.log(`   Slug: ${event.slug}`);
        console.log(`   Date: ${event.startDateTime}`);
        console.log(`   Type: ${event.type}`);
        console.log('');
      });
      
      // Check if February rope jam exists in API data
      const februaryEvent = eventsData.events.find(event => 
        event.title?.toLowerCase().includes('february') || 
        event.title?.toLowerCase().includes('rope jam')
      );
      
      if (februaryEvent) {
        console.log('✅ February rope jam found in API data:');
        console.log(`   ID: ${februaryEvent.id}`);
        console.log(`   Title: ${februaryEvent.title}`);
        console.log(`   Expected URL: /events/${februaryEvent.id} or /events/${februaryEvent.slug}`);
      } else {
        console.log('❌ February rope jam NOT found in API data');
        console.log('   This suggests the issue may be:');
        console.log('   - User referring to an event that no longer exists');
        console.log('   - Frontend showing cached/old data');
        console.log('   - Different API endpoint serving different data');
      }
    }
    
    // Test if individual event endpoint works
    if (eventsData.events && eventsData.events.length > 0) {
      const firstEvent = eventsData.events[0];
      console.log(`\nTesting individual event endpoint for: ${firstEvent.title}`);
      
      // Try both ID and slug-based URLs
      const idResponse = await page.request.get(`http://localhost:5655/api/events/${firstEvent.id}`);
      console.log(`GET /api/events/${firstEvent.id}: ${idResponse.status()}`);
      
      if (firstEvent.slug) {
        const slugResponse = await page.request.get(`http://localhost:5655/api/events/${firstEvent.slug}`);
        console.log(`GET /api/events/${firstEvent.slug}: ${slugResponse.status()}`);
      }
    }
  });

  test('check for mock vs real data patterns', async ({ page }) => {
    console.log('🔍 Mock vs Real Data Detection...');
    
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    // Check developer console for any errors or warnings
    const logs: string[] = [];
    page.on('console', msg => {
      logs.push(`${msg.type()}: ${msg.text()}`);
    });
    
    // Wait a bit to catch console messages
    await page.waitForTimeout(2000);
    
    // Look for common mock data patterns
    const pageContent = await page.textContent('body');
    const mockDataIndicators = [
      'lorem ipsum',
      'placeholder',
      'test event',
      'sample data',
      'mock',
      'fake data',
      'example event'
    ];
    
    console.log('Checking for mock data indicators:');
    mockDataIndicators.forEach(indicator => {
      const found = pageContent?.toLowerCase().includes(indicator);
      if (found) {
        console.log(`⚠️ Found mock indicator: "${indicator}"`);
      }
    });
    
    // Check console logs
    console.log('\nBrowser Console Messages:');
    logs.forEach(log => {
      if (log.includes('api') || log.includes('fetch') || log.includes('error')) {
        console.log(`  ${log}`);
      }
    });
    
    // Check if the events match our API data exactly
    const actualApiData = await page.request.get('http://localhost:5655/api/events');
    const apiEvents = await actualApiData.json();
    
    console.log('\nComparing page content with API data:');
    if (apiEvents.events) {
      apiEvents.events.forEach(event => {
        const titleFound = pageContent?.includes(event.title);
        console.log(`"${event.title}": ${titleFound ? '✅ DISPLAYED' : '❌ NOT DISPLAYED'}`);
      });
    }
  });
});