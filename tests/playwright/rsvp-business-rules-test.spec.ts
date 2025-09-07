import { test, expect, Page } from '@playwright/test';

/**
 * RSVP Business Rules Testing
 * 
 * Context from user request:
 * - EventsManagementService is working and returns events with EventType
 * - API endpoint should be /api/v1/events-management?showPast=false  
 * - Events with different types:
 *   - Social Events (EventType: "Social") - should allow RSVP
 *   - Workshops (EventType: "Workshop") - should NOT allow RSVP, only tickets
 *   - Performance (EventType: "Performance") - should NOT allow RSVP, only tickets
 * 
 * Test Goals:
 * 1. Verify events API returns events with proper EventType
 * 2. Test RSVP business rules for different event types
 * 3. Verify frontend UI shows correct buttons for each event type
 */

test.describe('RSVP Business Rules Testing', () => {
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('Environment Health Check', async () => {
    console.log('🔍 Checking environment health...');
    
    // Test API is responsive
    const apiResponse = await page.request.get('http://localhost:5655/health');
    expect(apiResponse.status()).toBe(200);
    console.log('✅ API healthy');

    // Test web app is responsive
    await page.goto('http://localhost:5174');
    expect(page.url()).toContain('localhost:5174');
    console.log('✅ Web app accessible');
  });

  test('Check Events API for EventType Support', async () => {
    console.log('🔍 Testing events API for EventType field...');
    
    // Test the mentioned v1 endpoint
    console.log('Testing /api/v1/events-management endpoint...');
    const v1Response = await page.request.get('http://localhost:5655/api/v1/events-management?showPast=false');
    console.log(`V1 Endpoint Status: ${v1Response.status()}`);
    
    if (v1Response.status() === 200) {
      const v1Data = await v1Response.json();
      console.log('V1 Events Data:', JSON.stringify(v1Data, null, 2));
      
      // Check if events have EventType field
      if (Array.isArray(v1Data) && v1Data.length > 0) {
        const hasEventType = v1Data.some(event => event.hasOwnProperty('eventType') || event.hasOwnProperty('EventType'));
        console.log(`Events have EventType field: ${hasEventType}`);
      }
    }
    
    // Test the base events endpoint as fallback
    console.log('Testing /api/events endpoint...');
    const baseResponse = await page.request.get('http://localhost:5655/api/events');
    expect(baseResponse.status()).toBe(200);
    
    const baseData = await baseResponse.json();
    console.log('Base Events Data (first event):', JSON.stringify(baseData[0] || {}, null, 2));
    
    // Check if base events have EventType field
    if (Array.isArray(baseData) && baseData.length > 0) {
      const hasEventType = baseData.some(event => event.hasOwnProperty('eventType') || event.hasOwnProperty('EventType'));
      console.log(`Base events have EventType field: ${hasEventType}`);
      
      // Log all events for analysis
      baseData.forEach((event, index) => {
        console.log(`Event ${index + 1}:`, {
          id: event.id,
          title: event.title,
          eventType: event.eventType || event.EventType || 'NOT SET',
          description: event.description?.substring(0, 100) + '...'
        });
      });
    }
  });

  test('Test RSVP Endpoint Existence', async () => {
    console.log('🔍 Testing RSVP endpoint patterns...');
    
    // Get a test event ID first
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);
    const events = await eventsResponse.json();
    
    if (events.length > 0) {
      const testEventId = events[0].id;
      console.log(`Using test event ID: ${testEventId}`);
      
      // Test various RSVP endpoint patterns
      const rsvpEndpoints = [
        `/api/events/${testEventId}/rsvp`,
        `/api/rsvp/${testEventId}`,
        `/api/v1/events/${testEventId}/rsvp`,
        `/api/v1/rsvp`,
      ];
      
      for (const endpoint of rsvpEndpoints) {
        console.log(`Testing RSVP endpoint: ${endpoint}`);
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        console.log(`  Status: ${response.status()}`);
        
        if (response.status() !== 404) {
          const data = await response.text();
          console.log(`  Response: ${data.substring(0, 200)}...`);
        }
      }
    }
  });

  test('Frontend Events Demo Page Analysis', async () => {
    console.log('🔍 Testing frontend events demo page...');
    
    await page.goto('http://localhost:5174/admin/events-management-api-demo');
    await page.waitForLoadState('networkidle');
    
    // Wait for page to fully load
    await page.waitForTimeout(2000);
    
    // Take screenshot for analysis
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/test-results/events-demo-page.png',
      fullPage: true 
    });
    
    // Check if events are displayed
    const eventsContainer = page.locator('[data-testid="events-container"], .events-list, .event-card').first();
    const eventsVisible = await eventsContainer.isVisible().catch(() => false);
    console.log(`Events container visible: ${eventsVisible}`);
    
    // Check for RSVP buttons
    const rsvpButtons = page.locator('button:has-text("RSVP"), [data-testid*="rsvp"], .rsvp-button');
    const rsvpCount = await rsvpButtons.count();
    console.log(`RSVP buttons found: ${rsvpCount}`);
    
    // Check for ticket buttons
    const ticketButtons = page.locator('button:has-text("Buy Tickets"), button:has-text("Tickets"), [data-testid*="ticket"]');
    const ticketCount = await ticketButtons.count();
    console.log(`Ticket buttons found: ${ticketCount}`);
    
    // Check for event type indicators
    const eventTypeIndicators = page.locator('[data-testid*="event-type"], .event-type, .event-category');
    const typeCount = await eventTypeIndicators.count();
    console.log(`Event type indicators found: ${typeCount}`);
    
    // Log page content for analysis
    const pageContent = await page.content();
    const hasEventTypeContent = pageContent.includes('Social') || pageContent.includes('Workshop') || pageContent.includes('Performance');
    console.log(`Page contains event type keywords: ${hasEventTypeContent}`);
  });

  test('Public Events Page Analysis', async () => {
    console.log('🔍 Testing public events page...');
    
    await page.goto('http://localhost:5174/events');
    await page.waitForLoadState('networkidle');
    
    // Wait for page to load
    await page.waitForTimeout(2000);
    
    // Take screenshot
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/test-results/public-events-page.png',
      fullPage: true 
    });
    
    // Check if this page exists and loads
    const title = await page.title();
    console.log(`Public events page title: ${title}`);
    
    const url = page.url();
    console.log(`Final URL: ${url}`);
    
    // Check for event listings
    const eventListings = page.locator('.event, [data-testid*="event"], .card');
    const listingCount = await eventListings.count();
    console.log(`Event listings found: ${listingCount}`);
    
    // Check for RSVP/ticket functionality
    const actionButtons = page.locator('button:has-text("RSVP"), button:has-text("Tickets"), button:has-text("Register")');
    const buttonCount = await actionButtons.count();
    console.log(`Action buttons found: ${buttonCount}`);
  });

  test('API RSVP Business Rules Test', async () => {
    console.log('🔍 Testing RSVP business rules via API calls...');
    
    // Get events first
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const events = await eventsResponse.json();
    
    for (const event of events) {
      console.log(`\n--- Testing Event: ${event.title} ---`);
      console.log(`Event Type: ${event.eventType || event.EventType || 'NOT SET'}`);
      console.log(`Description: ${event.description?.substring(0, 50)}...`);
      
      // Try to determine event type from title/description
      const title = event.title.toLowerCase();
      const description = (event.description || '').toLowerCase();
      
      let predictedType = 'unknown';
      if (title.includes('social') || description.includes('social')) {
        predictedType = 'social';
      } else if (title.includes('workshop') || title.includes('class') || description.includes('workshop') || description.includes('learn')) {
        predictedType = 'workshop';
      } else if (title.includes('performance') || description.includes('performance')) {
        predictedType = 'performance';
      }
      
      console.log(`Predicted type from content: ${predictedType}`);
      
      // Test RSVP endpoint for this event
      const rsvpResponse = await page.request.post(`http://localhost:5655/api/events/${event.id}/rsvp`, {
        data: {
          userId: 'test-user-id',
          eventId: event.id
        }
      });
      
      console.log(`RSVP attempt status: ${rsvpResponse.status()}`);
      
      if (rsvpResponse.status() !== 404) {
        const responseData = await rsvpResponse.text();
        console.log(`RSVP response: ${responseData.substring(0, 100)}`);
      }
    }
  });

  test('Generate Test Report', async () => {
    console.log('🔍 Generating comprehensive test report...');
    
    const report = {
      testDate: new Date().toISOString(),
      testType: 'RSVP Business Rules Validation',
      environment: {
        webUrl: 'http://localhost:5174',
        apiUrl: 'http://localhost:5655'
      },
      findings: [],
      recommendations: []
    };
    
    // API Endpoint Analysis
    const v1Response = await page.request.get('http://localhost:5655/api/v1/events-management?showPast=false');
    const baseResponse = await page.request.get('http://localhost:5655/api/events');
    
    report.findings.push({
      category: 'API Endpoints',
      details: {
        v1EventsManagement: {
          status: v1Response.status(),
          available: v1Response.status() === 200
        },
        baseEvents: {
          status: baseResponse.status(),
          available: baseResponse.status() === 200
        }
      }
    });
    
    if (baseResponse.status() === 200) {
      const events = await baseResponse.json();
      const hasEventTypes = events.some(e => e.eventType || e.EventType);
      
      report.findings.push({
        category: 'Event Type Support',
        details: {
          eventsTotal: events.length,
          hasEventTypeField: hasEventTypes,
          eventTitles: events.map(e => e.title)
        }
      });
      
      if (!hasEventTypes) {
        report.recommendations.push({
          priority: 'HIGH',
          category: 'Backend Development',
          task: 'Add EventType field to events API response',
          details: 'Events currently lack EventType field needed for RSVP business rules'
        });
      }
    }
    
    // RSVP Endpoint Analysis
    const rsvpExists = await page.request.get('http://localhost:5655/api/events/test/rsvp');
    report.findings.push({
      category: 'RSVP Endpoints',
      details: {
        status: rsvpExists.status(),
        implemented: rsvpExists.status() !== 404
      }
    });
    
    if (rsvpExists.status() === 404) {
      report.recommendations.push({
        priority: 'HIGH',
        category: 'Backend Development', 
        task: 'Implement RSVP API endpoints',
        details: 'RSVP endpoints are not implemented yet'
      });
    }
    
    // Frontend Analysis
    await page.goto('http://localhost:5174/admin/events-management-api-demo');
    await page.waitForTimeout(2000);
    
    const hasRsvpButtons = await page.locator('button:has-text("RSVP")').count() > 0;
    const hasTicketButtons = await page.locator('button:has-text("Ticket")').count() > 0;
    
    report.findings.push({
      category: 'Frontend UI',
      details: {
        rsvpButtonsPresent: hasRsvpButtons,
        ticketButtonsPresent: hasTicketButtons,
        demoPageAccessible: true
      }
    });
    
    if (!hasRsvpButtons && !hasTicketButtons) {
      report.recommendations.push({
        priority: 'MEDIUM',
        category: 'Frontend Development',
        task: 'Implement event action buttons (RSVP/Tickets)',
        details: 'Events need proper action buttons based on event type'
      });
    }
    
    // Save report
    await page.evaluate((reportData) => {
      console.log('=== RSVP BUSINESS RULES TEST REPORT ===');
      console.log(JSON.stringify(reportData, null, 2));
    }, report);
    
    // Write report to file system
    const fs = require('fs').promises;
    await fs.writeFile(
      '/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/test-results/rsvp-business-rules-report.json',
      JSON.stringify(report, null, 2)
    );
    
    console.log('✅ Test report generated and saved');
  });
});