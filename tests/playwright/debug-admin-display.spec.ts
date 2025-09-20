import { test, expect } from '@playwright/test';

test.describe('Debug Admin Display Issues', () => {
  test('Check admin events list capacity column', async ({ page }) => {
    console.log('🔍 Testing admin events list display');

    // Navigate to login page
    await page.goto('http://localhost:5173/');

    // Login as admin
    await page.getByText('LOGIN').click();
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.getByText('Sign In').click();

    // Wait for redirect
    await page.waitForURL('**/dashboard');

    // Navigate to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');

    // Find the Rope Social event row
    const ropeSocialRow = page.locator('tr:has-text("Rope Social & Discussion")');
    await expect(ropeSocialRow).toBeVisible();

    // Check the capacity column value
    const capacityCell = ropeSocialRow.locator('td').nth(3); // Assuming capacity is 4th column
    const capacityText = await capacityCell.textContent();
    console.log(`Capacity column shows: "${capacityText}"`);

    // Check if it shows current RSVPs
    if (capacityText?.includes('2')) {
      console.log('✅ Capacity column shows RSVP count (2)');
    } else {
      console.log('❌ Capacity column missing RSVP count');
    }

    // Take screenshot for debugging
    await page.screenshot({ path: 'test-results/admin-events-list-debug.png', fullPage: true });
  });

  test('Check admin event details RSVP tab', async ({ page }) => {
    console.log('🔍 Testing admin event details RSVP display');

    // Navigate and login
    await page.goto('http://localhost:5173/');
    await page.getByText('LOGIN').click();
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.getByText('Sign In').click();
    await page.waitForURL('**/dashboard');

    // Go directly to Rope Social admin page
    await page.goto('http://localhost:5173/admin/events/5290be55-59e0-4ec9-b62b-5cc215e6e848');
    await page.waitForLoadState('networkidle');

    // Look for RSVP/Tickets tab or content
    const rsvpTab = page.locator('text="RSVPs and Tickets"');
    const participationsTab = page.locator('text="Participations"');
    const rsvpContent = page.locator('text="RSVP"');

    if (await rsvpTab.isVisible()) {
      console.log('✅ Found "RSVPs and Tickets" tab');
      await rsvpTab.click();
    } else if (await participationsTab.isVisible()) {
      console.log('✅ Found "Participations" tab');
      await participationsTab.click();
    } else {
      console.log('❌ No RSVP/Participations tab found');
      console.log('Available tabs:');
      const tabs = await page.locator('[role="tab"], .tab, button:has-text("tab")').allTextContents();
      console.log(tabs);
    }

    // Check for RSVP data display
    await page.waitForTimeout(1000);
    const rsvpData = await page.locator('text="admin"').count();
    console.log(`Found ${rsvpData} elements containing "admin"`);

    // Look for table or grid with participation data
    const tables = await page.locator('table, .grid, [role="grid"]').count();
    console.log(`Found ${tables} table/grid elements`);

    if (tables > 0) {
      const tableData = await page.locator('table, .grid, [role="grid"]').first().textContent();
      console.log('Table/grid content preview:', tableData?.substring(0, 200));
    }

    // Take screenshot
    await page.screenshot({ path: 'test-results/admin-event-details-rsvp-debug.png', fullPage: true });
  });
});