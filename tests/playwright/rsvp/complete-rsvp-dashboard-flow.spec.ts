import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { RsvpPage } from '../pages/rsvp.page';
import { MemberDashboardPage } from '../pages/member-dashboard.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Complete RSVP Dashboard Flow Tests
 * Converted from Puppeteer test: test-complete-rsvp-dashboard-flow.js
 * 
 * Tests the complete RSVP to Dashboard flow including:
 * - Login as vetted member
 * - Navigate to events and RSVP to a social event
 * - Verify the RSVP appears on the dashboard
 * - API tracking and debugging
 */

test.describe('Complete RSVP to Dashboard Flow', () => {
  let loginPage: LoginPage;
  let rsvpPage: RsvpPage;
  let memberDashboardPage: MemberDashboardPage;
  let apiCalls: Array<{ url: string; status: number; method: string }> = [];

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    rsvpPage = new RsvpPage(page);
    memberDashboardPage = new MemberDashboardPage(page);
    
    // Reset API calls tracking
    apiCalls = [];
    
    // Track API calls
    page.on('response', response => {
      const url = response.url();
      if (url.includes('/api/')) {
        apiCalls.push({
          url: url.replace(testConfig.baseUrl, ''),
          status: response.status(),
          method: response.request().method()
        });
        console.log(`📡 API: ${response.request().method()} ${url.replace(testConfig.baseUrl, '')} - ${response.status()}`);
      }
    });
  });

  test('Complete RSVP to Dashboard Flow', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'complete-rsvp-dashboard-flow' });
    
    console.log('🚀 Testing Complete RSVP to Dashboard Flow');
    console.log('================================================');
    console.log('This test will:');
    console.log('1. Login as vetted member');
    console.log('2. Navigate to events and RSVP to a social event');
    console.log('3. Verify the RSVP appears on the dashboard');
    console.log('================================================\n');
    
    // Step 1: Login
    await loginPage.goto();
    await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
    await loginPage.verifyLoginSuccess();
    console.log('✅ Login successful');
    
    // Step 2: Navigate to events page
    console.log('\n📅 Step 2: Navigating to events page...');
    await page.goto(`${testConfig.baseUrl}/events`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    
    // Find a social event (Monthly Rope Jam)
    console.log('\n🔍 Looking for social events to RSVP to...');
    
    // Wait for event cards to load
    await page.waitForSelector('.event-card', { timeout: 10000 });
    
    const eventClicked = await page.evaluate(() => {
      // Find event cards
      const cards = document.querySelectorAll('.event-card');
      console.log(`Found ${cards.length} event cards`);
      
      for (let i = 0; i < cards.length; i++) {
        const card = cards[i];
        // Get the title
        const titleElement = card.querySelector('.event-image-title');
        const title = titleElement ? titleElement.textContent : '';
        
        // Look for Monthly Rope Jam or other social events
        if (title.includes('Monthly Rope Jam') || 
            card.querySelector('.member-only-badge')) {
          
          // Find the Learn More button
          const learnMoreBtn = card.querySelector('.wcr-button-primary');
          if (learnMoreBtn) {
            console.log(`Clicking on event: ${title}`);
            console.log(`Button text: ${learnMoreBtn.textContent}`);
            learnMoreBtn.click();
            return {
              success: true,
              eventName: title,
              buttonText: learnMoreBtn.textContent.trim()
            };
          }
        }
      }
      return { success: false };
    });
    
    if (!eventClicked.success) {
      console.log('❌ Could not find social event to click');
      await page.screenshot({ path: 'test-results/no-social-event-found.png' });
      throw new Error('No social event found to RSVP to');
    }
    
    console.log(`✅ Clicked on event: "${eventClicked.eventName}"`);
    console.log(`   Button clicked: "${eventClicked.buttonText}"`);
    
    // Wait for navigation to event details page
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    
    // Step 3: RSVP to the event
    console.log('\n🎫 Step 3: RSVPing to the event...');
    
    const rsvpResult = await page.evaluate(() => {
      const pageText = document.body.textContent;
      
      // Check if already registered
      if (pageText && (pageText.includes('already registered') || 
          pageText.includes("You're registered") ||
          pageText.includes('RSVP confirmed'))) {
        return { 
          status: 'already_registered',
          message: 'User is already registered for this event'
        };
      }
      
      // Find RSVP button
      const buttons = document.querySelectorAll('button');
      for (let i = 0; i < buttons.length; i++) {
        const btn = buttons[i];
        const btnText = btn.textContent || '';
        if ((btnText.includes('RSVP') || btnText.includes('Register')) && 
            !btn.disabled) {
          console.log(`Found RSVP button: "${btnText}"`);
          btn.click();
          return { 
            status: 'clicked',
            buttonText: btnText
          };
        }
      }
      
      // No RSVP button found
      return { 
        status: 'no_button',
        pageTitle: document.querySelector('h1')?.textContent || 'Unknown'
      };
    });
    
    console.log(`RSVP Result: ${rsvpResult.status}`);
    if (rsvpResult.buttonText) {
      console.log(`Clicked: "${rsvpResult.buttonText}"`);
    }
    
    // Wait for RSVP to process
    await page.waitForTimeout(5000);
    
    // Take screenshot of event page after RSVP
    await page.screenshot({ path: 'test-results/after-rsvp.png' });
    console.log('📸 Screenshot saved: after-rsvp.png');
    
    // Step 4: Navigate to dashboard
    console.log('\n📊 Step 4: Checking dashboard for RSVP...');
    await memberDashboardPage.goto();
    
    // Wait extra time for dashboard data
    console.log('⏳ Waiting for dashboard to load events...');
    await memberDashboardPage.waitForDashboardData(7000);
    
    // Analyze dashboard
    const dashboardSummary = await memberDashboardPage.getDashboardSummary();
    
    // Display results
    console.log('\n📋 Dashboard Analysis Results:');
    console.log(`   Upcoming Events Section: ${dashboardSummary.hasUpcomingSection ? '✅' : '❌'}`);
    console.log(`   Event Count: ${dashboardSummary.eventCount}`);
    console.log(`   RSVP Count: ${dashboardSummary.rsvpCount}`);
    console.log(`   Ticket Count: ${dashboardSummary.ticketCount}`);
    
    if (dashboardSummary.emptyStateMessage) {
      console.log(`   Empty State: "${dashboardSummary.emptyStateMessage}"`);
    }
    
    // Get all RSVPs
    const rsvps = await memberDashboardPage.getAllRsvps();
    if (rsvps.length > 0) {
      console.log('\n   RSVPs on Dashboard:');
      rsvps.forEach((rsvp, i) => {
        console.log(`   ${i + 1}. ${rsvp.title}`);
        console.log(`      Status: ${rsvp.status}`);
        console.log(`      Date: ${rsvp.date}`);
        if (rsvp.title.includes('Jam') || rsvp.title.includes('Social')) {
          console.log(`      ✨ This is a social event!`);
        }
      });
    } else {
      console.log('\n   ❌ No RSVPs found on dashboard');
      
      // Debug information
      const pageContent = await page.evaluate(() => {
        const upcomingSection = document.querySelector('.upcoming-events');
        return {
          hasUpcomingSection: !!upcomingSection,
          sectionHTML: upcomingSection ? upcomingSection.innerHTML.substring(0, 300) : 'Section not found'
        };
      });
      console.log(`   Debug HTML: ${pageContent.sectionHTML}`);
    }
    
    // Take final screenshot
    await page.screenshot({ path: 'test-results/dashboard-final.png', fullPage: true });
    console.log('\n📸 Screenshot saved: dashboard-final.png');
    
    // Display API calls summary
    console.log('\n📡 API Calls Summary:');
    const rsvpCalls = apiCalls.filter(call => call.url.includes('rsvp'));
    const dashboardCalls = apiCalls.filter(call => 
      call.url.includes('/users/me/rsvps') || 
      call.url.includes('/users/me/tickets')
    );
    
    console.log(`   RSVP-related calls: ${rsvpCalls.length}`);
    console.log(`   Dashboard data calls: ${dashboardCalls.length}`);
    
    // Final test verdict
    console.log('\n' + '='.repeat(60));
    console.log('📊 TEST VERDICT:');
    console.log('='.repeat(60));
    
    const testPassed = dashboardSummary.eventCount > 0 && 
                      dashboardSummary.rsvpCount > 0;
    
    if (testPassed) {
      console.log('✅ SUCCESS! Complete RSVP to Dashboard flow works!');
      console.log('   - User can RSVP to social events');
      console.log('   - RSVP appears on dashboard');
      console.log('   - Shows correct RSVP status/buttons');
    } else {
      console.log('❌ FAIL: RSVP not showing on dashboard');
      console.log('\n   Possible issues:');
      console.log('   1. API endpoints not returning RSVPs');
      console.log('   2. Dashboard not fetching from correct endpoints');
      console.log('   3. RSVP not being saved properly');
      console.log('   4. Authentication issue between web and API');
    }
    
    console.log('='.repeat(60));
    
    // Assert test passed
    expect(testPassed).toBeTruthy();
  });

  test('should track API calls correctly', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'api-tracking' });
    
    // Login
    await loginPage.goto();
    await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
    await loginPage.verifyLoginSuccess();
    
    // Navigate to dashboard
    await memberDashboardPage.goto();
    await memberDashboardPage.waitForDashboardData();
    
    // Check that dashboard API calls were made
    const dashboardApiCalls = apiCalls.filter(call => 
      call.url.includes('/users/me/') && call.method === 'GET'
    );
    
    expect(dashboardApiCalls.length).toBeGreaterThan(0);
    console.log(`✅ Made ${dashboardApiCalls.length} dashboard API calls`);
    
    // Log all API endpoints hit
    console.log('\nAPI Endpoints accessed:');
    dashboardApiCalls.forEach(call => {
      console.log(`  - ${call.method} ${call.url} (${call.status})`);
    });
  });

  test('should handle empty RSVP state gracefully', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'empty-rsvp-state' });
    
    // Create a new test user that has no RSVPs
    // This test would verify the empty state UI
    // Implementation depends on test data setup
  });
});

test.describe('RSVP Dashboard Edge Cases', () => {
  test('should update dashboard immediately after RSVP', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'immediate-dashboard-update' });
    
    const loginPage = new LoginPage(page);
    const memberDashboardPage = new MemberDashboardPage(page);
    
    // This test would verify real-time updates
    // Implementation depends on actual application behavior
  });

  test('should show correct RSVP count in dashboard stats', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'dashboard-rsvp-stats' });
    
    const loginPage = new LoginPage(page);
    const memberDashboardPage = new MemberDashboardPage(page);
    
    // Login as member
    await loginPage.goto();
    await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
    await loginPage.verifyLoginSuccess();
    
    // Go to dashboard
    await memberDashboardPage.goto();
    await memberDashboardPage.waitForDashboardData();
    
    // Get summary
    const summary = await memberDashboardPage.getDashboardSummary();
    
    // Verify counts are consistent
    expect(summary.eventCount).toBeGreaterThanOrEqual(summary.rsvpCount);
    console.log(`✅ Dashboard shows ${summary.rsvpCount} RSVPs out of ${summary.eventCount} total events`);
  });
});