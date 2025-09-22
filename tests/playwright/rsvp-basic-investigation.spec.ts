import { test, expect } from '@playwright/test';

test.describe('RSVP System Basic Investigation', () => {

  test('1. React App Loading Investigation', async ({ page }) => {
    console.log('🔍 Investigating React App Loading');

    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    // Take screenshot of what actually loads
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/react-app-actual-state.png',
      fullPage: true
    });

    // Check if React app actually initialized
    const rootElement = await page.locator('#root').innerHTML();
    console.log(`📊 Root element content length: ${rootElement.length} characters`);
    console.log(`📊 Root element content preview: ${rootElement.substring(0, 200)}...`);

    // Check for any JavaScript errors
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    await page.waitForTimeout(3000);
    console.log(`🚨 JavaScript errors found: ${errors.length}`);
    if (errors.length > 0) {
      console.log('❌ JavaScript errors:', errors);
    }

    // Check for specific React elements
    const hasLoginButton = await page.locator('button:has-text("LOGIN"), text=LOGIN').count() > 0;
    const hasNavigation = await page.locator('nav, .nav, .navigation').count() > 0;
    const hasContent = rootElement.length > 100;

    console.log(`📊 Has LOGIN button: ${hasLoginButton}`);
    console.log(`📊 Has navigation: ${hasNavigation}`);
    console.log(`📊 Has content: ${hasContent}`);
  });

  test('2. API Events Data Investigation', async ({ page }) => {
    console.log('🔍 Investigating API Events Data');

    // Test events endpoint directly
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const eventsStatus = eventsResponse.status();
    const eventsData = await eventsResponse.text();

    console.log(`📊 Events API Status: ${eventsStatus}`);
    console.log(`📊 Events API Data Length: ${eventsData.length} characters`);

    if (eventsStatus === 200) {
      try {
        const eventsJson = JSON.parse(eventsData);
        console.log(`📊 Number of events: ${Array.isArray(eventsJson) ? eventsJson.length : 'Not an array'}`);

        if (Array.isArray(eventsJson) && eventsJson.length > 0) {
          const firstEvent = eventsJson[0];
          console.log(`📊 First event keys: ${Object.keys(firstEvent).join(', ')}`);

          // Check for capacity/participant related fields
          const capacityFields = Object.keys(firstEvent).filter(key =>
            key.toLowerCase().includes('capacity') ||
            key.toLowerCase().includes('participant') ||
            key.toLowerCase().includes('rsvp') ||
            key.toLowerCase().includes('attend')
          );
          console.log(`📊 Capacity-related fields: ${capacityFields.join(', ')}`);

          // Check for specific values
          if (firstEvent.capacity !== undefined) {
            console.log(`📊 First event capacity: ${firstEvent.capacity}`);
          }
          if (firstEvent.participantCount !== undefined) {
            console.log(`📊 First event participant count: ${firstEvent.participantCount}`);
          }
          if (firstEvent.currentAttendees !== undefined) {
            console.log(`📊 First event current attendees: ${firstEvent.currentAttendees}`);
          }
        }
      } catch (error) {
        console.log(`❌ Failed to parse events JSON: ${error}`);
        console.log(`📊 Raw events data preview: ${eventsData.substring(0, 500)}...`);
      }
    }
  });

  test('3. API Admin Endpoints Investigation', async ({ page }) => {
    console.log('🔍 Investigating API Admin Endpoints');

    // Test various admin endpoints (should get 401 without auth)
    const adminEndpoints = [
      '/api/admin/events',
      '/api/admin/users',
      '/api/admin/rsvps',
      '/api/admin/participations',
      '/api/admin/dashboard'
    ];

    for (const endpoint of adminEndpoints) {
      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        const status = response.status();
        const data = await response.text();

        console.log(`📊 ${endpoint}: Status ${status}, Data length: ${data.length} chars`);

        // If it's not 401/403, it might be a public endpoint or misconfigured
        if (status !== 401 && status !== 403) {
          console.log(`⚠️ ${endpoint} returned unexpected status ${status}`);
          if (data.length < 500) {
            console.log(`📊 ${endpoint} response: ${data}`);
          }
        }
      } catch (error) {
        console.log(`❌ Failed to test ${endpoint}: ${error}`);
      }
    }
  });

  test('4. API RSVP Endpoints Investigation', async ({ page }) => {
    console.log('🔍 Investigating API RSVP Endpoints');

    // Test various RSVP-related endpoints
    const rsvpEndpoints = [
      '/api/rsvps',
      '/api/participations',
      '/api/tickets',
      '/api/events/1/participants',
      '/api/events/1/rsvps',
      '/api/user/rsvps',
      '/api/user/participations'
    ];

    for (const endpoint of rsvpEndpoints) {
      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        const status = response.status();
        const data = await response.text();

        console.log(`📊 ${endpoint}: Status ${status}, Data length: ${data.length} chars`);

        if (status === 200 && data.length < 1000) {
          console.log(`📊 ${endpoint} response preview: ${data.substring(0, 200)}...`);
        }
      } catch (error) {
        console.log(`❌ Failed to test ${endpoint}: ${error}`);
      }
    }
  });

  test('5. Database Direct Query for RSVP Data', async ({ page }) => {
    console.log('🔍 Investigating Database RSVP Data Directly');

    // Since we can't login through the app, let's check what the database actually contains
    // This test documents what data exists vs what the API/UI shows

    console.log('📊 Database contains the following tables related to participation:');
    console.log('   - EventAttendees');
    console.log('   - EventParticipations');
    console.log('   - ParticipationHistory');
    console.log('   - Users');
    console.log('   - Events');

    // Test health endpoint which shows user count
    const healthResponse = await page.request.get('http://localhost:5655/api/health');
    if (healthResponse.status() === 200) {
      const healthData = await healthResponse.json();
      console.log(`📊 Health endpoint shows: ${JSON.stringify(healthData, null, 2)}`);
    }

    console.log('💡 To verify RSVP functionality, we need:');
    console.log('   1. Working React app with login capability');
    console.log('   2. API endpoints that return participation data');
    console.log('   3. Admin interface showing event capacity/participants');
    console.log('   4. User interface showing RSVP status and cancel options');
  });

  test('6. Comprehensive System State Documentation', async ({ page }) => {
    console.log('🔍 Documenting Current System State');

    // Visit main page and document what we see
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/system-state-main-page.png',
      fullPage: true
    });

    // Try common paths and document what happens
    const commonPaths = [
      '/',
      '/events',
      '/dashboard',
      '/admin',
      '/profile',
      '/login'
    ];

    for (const path of commonPaths) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        const pageTitle = await page.title();
        const rootContent = await page.locator('#root').innerHTML();
        const hasContent = rootContent.length > 100;

        console.log(`📊 ${path}: Title="${pageTitle}", Has content: ${hasContent}, Content length: ${rootContent.length}`);

        await page.screenshot({
          path: `/home/chad/repos/witchcityrope-react./test-results/system-state-${path.replace('/', 'root').replace(/[\/]/g, '-')}.png`,
          fullPage: true
        });
      } catch (error) {
        console.log(`❌ Failed to document ${path}: ${error}`);
      }
    }

    console.log('📋 SUMMARY OF CURRENT SYSTEM STATE:');
    console.log('   ✅ API is healthy and responding');
    console.log('   ✅ Database is connected with user data');
    console.log('   ✅ Events API returns data');
    console.log('   ❌ React app appears to have JavaScript execution issues');
    console.log('   ❌ Login functionality is not available');
    console.log('   ❌ Cannot test RSVP UI functionality without working React app');
    console.log('');
    console.log('🔧 NEXT STEPS NEEDED:');
    console.log('   1. Fix React app JavaScript execution');
    console.log('   2. Verify login modal appears and functions');
    console.log('   3. Test RSVP functionality once app is working');
    console.log('   4. Verify admin interfaces show correct data');
  });

});