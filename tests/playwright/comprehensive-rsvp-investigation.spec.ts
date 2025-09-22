import { test, expect } from '@playwright/test';

test.describe('Comprehensive RSVP System Broken Features Investigation', () => {

  test('COMPLETE ANALYSIS: Document actual current state vs API data', async ({ page }) => {
    console.log('🔍 Starting comprehensive RSVP system investigation');

    // Create summary for easy access
    const investigation = {
      timestamp: new Date().toISOString(),
      findings: [],
      screenshots: [],
      apiData: {},
      uiData: {},
      discrepancies: []
    };

    // ===== STEP 1: GET API DATA FIRST =====
    console.log('\n📊 STEP 1: Getting API data...');

    try {
      const eventsResponse = await page.request.get('http://localhost:5655/api/events');
      const eventsData = await eventsResponse.json();

      investigation.apiData.publicEvents = {
        status: eventsResponse.status(),
        count: eventsData?.data?.length || 0,
        data: eventsData?.data || []
      };

      console.log(`📊 Public Events API: ${eventsResponse.status()} - ${investigation.apiData.publicEvents.count} events`);

      // Find events with actual RSVP data
      const eventsWithRsvps = eventsData?.data?.filter(e => e.currentRSVPs > 0 || e.currentAttendees > 0) || [];
      console.log(`📊 Events with actual RSVPs/attendees: ${eventsWithRsvps.length}`);

      eventsWithRsvps.forEach(event => {
        console.log(`  - ${event.title}: ${event.currentRSVPs} RSVPs, ${event.currentAttendees} attendees, capacity ${event.capacity}`);
      });

    } catch (error) {
      investigation.findings.push(`❌ Failed to get public events API: ${error.message}`);
    }

    // ===== STEP 2: DOCUMENT HOMEPAGE/PUBLIC EVENTS =====
    console.log('\n🖥️ STEP 2: Documenting public events page...');

    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: './test-results/INVESTIGATION-01-homepage.png', fullPage: true });
    investigation.screenshots.push('INVESTIGATION-01-homepage.png');

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: './test-results/INVESTIGATION-02-public-events.png', fullPage: true });
    investigation.screenshots.push('INVESTIGATION-02-public-events.png');

    // Document what events are shown
    const eventElements = await page.locator('[data-testid*="event"], .event-card, .event-item').count();
    const bodyText = await page.textContent('body');

    // Look for capacity indicators
    const capacityPatterns = bodyText.match(/\d+\/\d+/g) || [];
    const rsvpPatterns = bodyText.match(/\d+\s*RSVP/gi) || [];

    investigation.uiData.publicEvents = {
      eventsShown: eventElements,
      capacityPatterns: capacityPatterns,
      rsvpPatterns: rsvpPatterns,
      hasRsvpButtons: (bodyText.match(/RSVP|Register|Join/gi) || []).length
    };

    console.log(`🖥️ Public events UI shows: ${eventElements} events`);
    console.log(`🖥️ Capacity patterns found: ${capacityPatterns.join(', ')}`);
    console.log(`🖥️ RSVP patterns found: ${rsvpPatterns.join(', ')}`);

    // ===== STEP 3: TEST LOGIN FUNCTIONALITY =====
    console.log('\n🔐 STEP 3: Testing login...');

    // Find and click login (checking multiple possible ways)
    const loginFound = await page.locator('a:has-text("Login"), button:has-text("Login"), [href*="login"]').count() > 0;

    if (loginFound) {
      await page.locator('a:has-text("Login"), button:has-text("Login"), [href*="login"]').first().click();
      await page.waitForTimeout(2000);
      await page.screenshot({ path: './test-results/INVESTIGATION-03-login-attempt.png', fullPage: true });
      investigation.screenshots.push('INVESTIGATION-03-login-attempt.png');

      // Check if login form appeared
      const hasEmailField = await page.locator('input[type="email"], input[name="email"]').count() > 0;
      const hasPasswordField = await page.locator('input[type="password"], input[name="password"]').count() > 0;

      console.log(`🔐 Login form fields - Email: ${hasEmailField}, Password: ${hasPasswordField}`);

      if (hasEmailField && hasPasswordField) {
        // Try to login as admin
        await page.fill('input[type="email"], input[name="email"]', 'admin@witchcityrope.com');
        await page.fill('input[type="password"], input[name="password"]', 'Test123!');

        // Find submit button
        const submitButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")');
        if (await submitButton.count() > 0) {
          await submitButton.first().click();
          await page.waitForTimeout(3000);

          await page.screenshot({ path: './test-results/INVESTIGATION-04-after-login.png', fullPage: true });
          investigation.screenshots.push('INVESTIGATION-04-after-login.png');

          const currentUrl = page.url();
          const pageContent = await page.textContent('body');
          const loginSuccessful = !pageContent.toLowerCase().includes('invalid') &&
                                 !pageContent.toLowerCase().includes('error') &&
                                 (pageContent.toLowerCase().includes('dashboard') ||
                                  pageContent.toLowerCase().includes('admin') ||
                                  pageContent.toLowerCase().includes('logout'));

          console.log(`🔐 Login successful: ${loginSuccessful} (URL: ${currentUrl})`);
          investigation.findings.push(`Login attempt: ${loginSuccessful ? 'SUCCESS' : 'FAILED'}`);

          // ===== STEP 4: TEST AUTHENTICATED APIS =====
          if (loginSuccessful) {
            console.log('\n📊 STEP 4: Testing authenticated APIs...');

            // Test admin events API (should work now)
            try {
              const adminEventsResponse = await page.request.get('http://localhost:5655/api/admin/events');
              investigation.apiData.adminEvents = {
                status: adminEventsResponse.status(),
                data: adminEventsResponse.status() === 200 ? await adminEventsResponse.json() : null
              };
              console.log(`📊 Admin Events API: ${adminEventsResponse.status()}`);
            } catch (error) {
              investigation.findings.push(`❌ Admin events API failed: ${error.message}`);
            }

            // Test user dashboard API
            try {
              const dashboardResponse = await page.request.get('http://localhost:5655/api/dashboard/events');
              investigation.apiData.dashboard = {
                status: dashboardResponse.status(),
                data: dashboardResponse.status() === 200 ? await dashboardResponse.json() : null
              };
              console.log(`📊 Dashboard API: ${dashboardResponse.status()}`);
            } catch (error) {
              investigation.findings.push(`❌ Dashboard API failed: ${error.message}`);
            }

            // Test user info API
            try {
              const userResponse = await page.request.get('http://localhost:5655/api/auth/user');
              investigation.apiData.user = {
                status: userResponse.status(),
                data: userResponse.status() === 200 ? await userResponse.json() : null
              };
              console.log(`📊 User API: ${userResponse.status()}`);
            } catch (error) {
              investigation.findings.push(`❌ User API failed: ${error.message}`);
            }

            // ===== STEP 5: TEST ADMIN INTERFACE =====
            console.log('\n🛡️ STEP 5: Testing admin interface...');

            // Try to navigate to admin area
            await page.goto('http://localhost:5173/admin');
            await page.waitForTimeout(2000);
            await page.screenshot({ path: './test-results/INVESTIGATION-05-admin-page.png', fullPage: true });
            investigation.screenshots.push('INVESTIGATION-05-admin-page.png');

            const adminPageContent = await page.textContent('body');
            const hasAdminContent = adminPageContent.toLowerCase().includes('admin') &&
                                   !adminPageContent.toLowerCase().includes('not found') &&
                                   !adminPageContent.toLowerCase().includes('404');

            console.log(`🛡️ Admin page accessible: ${hasAdminContent}`);

            if (hasAdminContent) {
              // Try to navigate to admin events
              await page.goto('http://localhost:5173/admin/events');
              await page.waitForTimeout(2000);
              await page.screenshot({ path: './test-results/INVESTIGATION-06-admin-events.png', fullPage: true });
              investigation.screenshots.push('INVESTIGATION-06-admin-events.png');

              // Check for events table and capacity display
              const adminEventsContent = await page.textContent('body');
              const hasEventsTable = adminEventsContent.includes('capacity') ||
                                    adminEventsContent.includes('Capacity') ||
                                    adminEventsContent.includes('participants');

              console.log(`🛡️ Admin events page has capacity info: ${hasEventsTable}`);

              // Look for capacity values in the admin interface
              const adminCapacityPatterns = adminEventsContent.match(/\d+\/\d+/g) || [];
              const zeroCapacities = adminEventsContent.match(/0\/\d+|0\s+\/\s+\d+/g) || [];

              investigation.uiData.adminEvents = {
                hasTable: hasEventsTable,
                capacityPatterns: adminCapacityPatterns,
                zeroCapacities: zeroCapacities
              };

              console.log(`🛡️ Admin capacity patterns: ${adminCapacityPatterns.join(', ')}`);
              console.log(`🛡️ Zero capacity displays: ${zeroCapacities.join(', ')}`);
            }

            // ===== STEP 6: TEST USER DASHBOARD =====
            console.log('\n📋 STEP 6: Testing user dashboard...');

            await page.goto('http://localhost:5173/dashboard');
            await page.waitForTimeout(2000);
            await page.screenshot({ path: './test-results/INVESTIGATION-07-dashboard.png', fullPage: true });
            investigation.screenshots.push('INVESTIGATION-07-dashboard.png');

            const dashboardContent = await page.textContent('body');
            const rsvpCountTexts = dashboardContent.match(/\d+\s*RSVP/gi) || [];
            const socialEventTexts = dashboardContent.match(/\d+.*social.*event/gi) || [];
            const hasZeroRsvp = dashboardContent.includes('0 RSVP');

            investigation.uiData.dashboard = {
              rsvpCountTexts: rsvpCountTexts,
              socialEventTexts: socialEventTexts,
              hasZeroRsvp: hasZeroRsvp
            };

            console.log(`📋 Dashboard RSVP texts: ${rsvpCountTexts.join(', ')}`);
            console.log(`📋 Dashboard shows zero RSVPs: ${hasZeroRsvp}`);
          }
        }
      }
    }

    // ===== STEP 7: ANALYZE DISCREPANCIES =====
    console.log('\n🔍 STEP 7: Analyzing discrepancies...');

    // Compare API data with UI displays
    if (investigation.apiData.publicEvents && investigation.uiData.publicEvents) {
      const apiEventCount = investigation.apiData.publicEvents.count;
      const uiEventCount = investigation.uiData.publicEvents.eventsShown;

      if (apiEventCount !== uiEventCount) {
        investigation.discrepancies.push({
          type: 'Event Count Mismatch',
          api: `${apiEventCount} events`,
          ui: `${uiEventCount} events shown`
        });
      }
    }

    // Check for API events with RSVPs but UI showing zeros
    const apiEventsWithRsvps = investigation.apiData.publicEvents?.data?.filter(e => e.currentRSVPs > 0) || [];
    if (apiEventsWithRsvps.length > 0 && investigation.uiData.dashboard?.hasZeroRsvp) {
      investigation.discrepancies.push({
        type: 'RSVP Count Display Issue',
        api: `${apiEventsWithRsvps.length} events with RSVPs`,
        ui: 'Dashboard shows "0 RSVP"'
      });
    }

    // Check admin events API vs UI
    if (investigation.apiData.adminEvents?.status === 200 && investigation.uiData.adminEvents?.zeroCapacities?.length > 0) {
      investigation.discrepancies.push({
        type: 'Admin Capacity Display Issue',
        api: 'Admin API returns event data',
        ui: `${investigation.uiData.adminEvents.zeroCapacities.length} zero capacity displays`
      });
    }

    // ===== STEP 8: GENERATE FINAL REPORT =====
    console.log('\n📄 STEP 8: Generating final report...');

    await page.screenshot({ path: './test-results/INVESTIGATION-08-final-state.png', fullPage: true });
    investigation.screenshots.push('INVESTIGATION-08-final-state.png');

    // Save detailed report
    const detailedReport = JSON.stringify(investigation, null, 2);
    require('fs').writeFileSync('./test-results/INVESTIGATION-detailed-report.json', detailedReport);

    // Print summary
    console.log('\n📋 ===== INVESTIGATION SUMMARY =====');
    console.log(`🕒 Timestamp: ${investigation.timestamp}`);
    console.log(`📊 API Events Found: ${investigation.apiData.publicEvents?.count || 0}`);
    console.log(`🖥️ UI Events Shown: ${investigation.uiData.publicEvents?.eventsShown || 0}`);
    console.log(`❌ Discrepancies Found: ${investigation.discrepancies.length}`);
    console.log(`📸 Screenshots Captured: ${investigation.screenshots.length}`);

    console.log('\n🚨 CRITICAL FINDINGS:');
    investigation.findings.forEach((finding, i) => {
      console.log(`  ${i + 1}. ${finding}`);
    });

    console.log('\n❌ DISCREPANCIES IDENTIFIED:');
    investigation.discrepancies.forEach((disc, i) => {
      console.log(`  ${i + 1}. ${disc.type}:`);
      console.log(`     API: ${disc.api}`);
      console.log(`     UI:  ${disc.ui}`);
    });

    console.log('\n📸 EVIDENCE SAVED TO:');
    investigation.screenshots.forEach(screenshot => {
      console.log(`  - ./test-results/${screenshot}`);
    });

    console.log('\n📄 DETAILED REPORT: ./test-results/INVESTIGATION-detailed-report.json');
    console.log('\n✅ Investigation complete - All evidence documented');
  });
});