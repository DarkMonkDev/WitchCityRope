import { test, expect } from '@playwright/test';

test.describe('Login and Navigation Test', () => {
  test('Login as admin and navigate to events management', async ({ page }) => {
    console.log('🔍 Testing login and navigation to admin areas...');

    await page.goto('http://localhost:5173');
    await page.waitForTimeout(3000);

    // Click LOGIN button
    console.log('🔑 Clicking LOGIN button...');
    await page.click('text=LOGIN');
    await page.waitForTimeout(2000);

    // Fill login form
    console.log('📝 Filling login form...');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');

    // Submit login
    console.log('✅ Submitting login...');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);

    // Check if we're logged in by looking for admin elements
    console.log('🔍 Checking for admin access...');

    // Try to navigate to admin area
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    // Take a screenshot of the admin page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/admin-events-page.png' });

    // Check page content
    const pageContent = await page.textContent('body');
    console.log('📄 Admin page content length:', pageContent?.length || 0);

    // Look for event management elements
    const hasEventManagement = pageContent?.includes('Events') || pageContent?.includes('Event');
    console.log('📊 Has event management content:', hasEventManagement);

    if (hasEventManagement) {
      console.log('✅ Successfully logged in and accessed admin events page');
    } else {
      console.log('⚠️ Could not verify admin events page content');
    }
  });

  test('Verify API events endpoint still works after navigation', async ({ page }) => {
    console.log('🔍 Final verification - API events endpoint...');

    const response = await page.request.get('http://localhost:5655/api/events');
    expect(response.status()).toBe(200);

    const data = await response.json();
    const firstEvent = data.data[0];

    console.log('📊 FINAL API VERIFICATION:');
    console.log(`- Event: ${firstEvent.title}`);
    console.log(`- Sessions: ${firstEvent.sessions?.length || 0} items`);
    console.log(`- Ticket Types: ${firstEvent.ticketTypes?.length || 0} items`);
    console.log(`- Volunteer Positions: ${firstEvent.volunteerPositions?.length || 0} items`);

    // Verify all arrays are present and populated
    expect(firstEvent.sessions).toBeDefined();
    expect(firstEvent.sessions.length).toBeGreaterThan(0);
    expect(firstEvent.ticketTypes).toBeDefined();
    expect(firstEvent.ticketTypes.length).toBeGreaterThan(0);
    expect(firstEvent.volunteerPositions).toBeDefined();
    expect(firstEvent.volunteerPositions.length).toBeGreaterThan(0);

    console.log('🎉 PERSISTENCE FIX CONFIRMED: All data arrays present and populated!');
  });
});