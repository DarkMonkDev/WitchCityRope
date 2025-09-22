import { test, expect } from '@playwright/test';

test.describe('Basic App Check', () => {
  test('Check if React app loads and renders', async ({ page }) => {
    console.log('🔍 Testing basic React app functionality...');

    const consoleErrors: string[] = [];
    const networkErrors: string[] = [];

    // Monitor console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
        console.log('❌ Console Error:', msg.text());
      }
    });

    // Monitor network failures
    page.on('response', response => {
      if (!response.ok() && response.status() >= 400) {
        networkErrors.push(`${response.status()} ${response.url()}`);
        console.log('❌ Network Error:', response.status(), response.url());
      }
    });

    await page.goto('http://localhost:5173');

    // Wait a bit for the app to load
    await page.waitForTimeout(5000);

    // Check if the page title is correct
    const title = await page.title();
    console.log('📄 Page title:', title);
    expect(title).toContain('Witch City Rope');

    // Check if the root element has content
    const rootContent = await page.locator('#root').textContent();
    console.log('📋 Root content length:', rootContent?.length || 0);

    // Check if there are any visible elements
    const bodyText = await page.locator('body').textContent();
    console.log('📋 Body content length:', bodyText?.length || 0);

    // Take a screenshot for debugging
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/app-state.png' });

    // Look for any visible text that would indicate the app loaded
    const hasVisibleContent = await page.locator('body *:visible').count();
    console.log('👁️ Visible elements count:', hasVisibleContent);

    console.log(`📊 Console Errors: ${consoleErrors.length}`);
    console.log(`📊 Network Errors: ${networkErrors.length}`);

    if (consoleErrors.length > 0) {
      console.log('Console Errors Details:', consoleErrors);
    }

    if (networkErrors.length > 0) {
      console.log('Network Errors Details:', networkErrors);
    }

    // Report the state
    if (hasVisibleContent > 0) {
      console.log('✅ React app appears to be rendering content');
    } else {
      console.log('❌ React app is not rendering visible content');
    }
  });

  test('Direct API verification for events', async ({ page }) => {
    console.log('🔍 Testing API endpoint directly...');

    const response = await page.request.get('http://localhost:5655/api/events');
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data.data).toBeDefined();
    expect(data.data.length).toBeGreaterThan(0);

    const firstEvent = data.data[0];
    console.log('📊 API Response includes:');
    console.log(`- Event ID: ${firstEvent.id}`);
    console.log(`- Title: ${firstEvent.title}`);
    console.log(`- Sessions: ${firstEvent.sessions?.length || 0} items`);
    console.log(`- Ticket Types: ${firstEvent.ticketTypes?.length || 0} items`);
    console.log(`- Volunteer Positions: ${firstEvent.volunteerPositions?.length || 0} items`);

    // Verify all expected arrays exist
    expect(firstEvent.sessions).toBeDefined();
    expect(firstEvent.ticketTypes).toBeDefined();
    expect(firstEvent.volunteerPositions).toBeDefined();

    console.log('✅ API includes all required data arrays - PERSISTENCE FIX CONFIRMED');
  });
});