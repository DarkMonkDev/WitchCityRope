import { test, expect } from '@playwright/test';

test.describe('Persistence Fix Validation', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to admin login
    await page.goto('http://localhost:5173');
    await page.click('text=LOGIN');

    // Login as admin
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for login to complete
    await page.waitForSelector('text=Welcome');

    // Navigate to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
  });

  test('Complete Persistence Validation - Add, Edit, Delete Data', async ({ page, request }) => {
    console.log('🧪 Starting comprehensive persistence validation...');

    // Take screenshot of initial state
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/persistence-initial-state.png', fullPage: true });

    // STEP 1: Test Adding New Data
    console.log('📝 Testing data addition...');

    // Find an existing event to modify
    const eventCard = page.locator('[data-testid="event-card"]').first();
    await expect(eventCard).toBeVisible();
    await eventCard.click();

    // Wait for event details to load
    await page.waitForSelector('[data-testid="event-edit-form"]', { timeout: 10000 });

    // Test adding a new session
    console.log('🎯 Adding new session...');
    await page.click('[data-testid="add-session-button"]');
    await page.waitForSelector('[data-testid="session-form"]');

    const uniqueSessionName = `Test Session ${Date.now()}`;
    await page.fill('[data-testid="session-name-input"]', uniqueSessionName);
    await page.fill('[data-testid="session-capacity-input"]', '25');

    // Save session
    await page.click('[data-testid="save-session-button"]');
    await page.waitForTimeout(1000); // Allow save to complete

    // Test adding a new ticket type
    console.log('🎟️ Adding new ticket type...');
    await page.click('[data-testid="add-ticket-type-button"]');
    await page.waitForSelector('[data-testid="ticket-type-form"]');

    const uniqueTicketName = `Test Ticket ${Date.now()}`;
    await page.fill('[data-testid="ticket-type-name-input"]', uniqueTicketName);
    await page.fill('[data-testid="ticket-type-price-input"]', '15.00');

    // Save ticket type
    await page.click('[data-testid="save-ticket-type-button"]');
    await page.waitForTimeout(1000); // Allow save to complete

    // Test adding a new volunteer position
    console.log('🤝 Adding new volunteer position...');
    await page.click('[data-testid="add-volunteer-position-button"]');
    await page.waitForSelector('[data-testid="volunteer-position-form"]');

    const uniqueVolunteerTitle = `Test Volunteer ${Date.now()}`;
    await page.fill('[data-testid="volunteer-position-title-input"]', uniqueVolunteerTitle);
    await page.fill('[data-testid="volunteer-position-description-input"]', 'Test volunteer description');

    // Save volunteer position
    await page.click('[data-testid="save-volunteer-position-button"]');
    await page.waitForTimeout(1000); // Allow save to complete

    // Save the entire event
    console.log('💾 Saving event with all new data...');
    await page.click('[data-testid="save-event-button"]');
    await page.waitForTimeout(2000); // Allow full save to complete

    // Take screenshot after adding data
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/persistence-after-add.png', fullPage: true });

    // STEP 2: Test Persistence by Refreshing Page
    console.log('🔄 Testing persistence by refreshing page...');
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Navigate back to the event
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    await eventCard.click();
    await page.waitForSelector('[data-testid="event-edit-form"]');

    // Verify data persisted
    console.log('✅ Verifying added data persisted...');

    // Check session persisted
    await expect(page.locator(`text=${uniqueSessionName}`)).toBeVisible();
    console.log(`✅ Session "${uniqueSessionName}" persisted successfully`);

    // Check ticket type persisted
    await expect(page.locator(`text=${uniqueTicketName}`)).toBeVisible();
    console.log(`✅ Ticket Type "${uniqueTicketName}" persisted successfully`);

    // Check volunteer position persisted
    await expect(page.locator(`text=${uniqueVolunteerTitle}`)).toBeVisible();
    console.log(`✅ Volunteer Position "${uniqueVolunteerTitle}" persisted successfully`);

    // STEP 3: Test Editing Data
    console.log('✏️ Testing data editing...');

    // Edit the session
    await page.click(`[data-testid="edit-session-button"]`);
    await page.waitForSelector('[data-testid="session-form"]');

    const editedSessionName = `${uniqueSessionName} - Edited`;
    await page.fill('[data-testid="session-name-input"]', editedSessionName);
    await page.click('[data-testid="save-session-button"]');
    await page.waitForTimeout(1000);

    // Save event
    await page.click('[data-testid="save-event-button"]');
    await page.waitForTimeout(2000);

    // Refresh and verify edit persisted
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.goto('http://localhost:5173/admin/events');
    await eventCard.click();
    await page.waitForSelector('[data-testid="event-edit-form"]');

    await expect(page.locator(`text=${editedSessionName}`)).toBeVisible();
    console.log(`✅ Session edit to "${editedSessionName}" persisted successfully`);

    // STEP 4: Test Deleting Data
    console.log('🗑️ Testing data deletion...');

    // Delete the session
    await page.click(`[data-testid="delete-session-button"]`);
    await page.click('[data-testid="confirm-delete-button"]');
    await page.waitForTimeout(1000);

    // Save event
    await page.click('[data-testid="save-event-button"]');
    await page.waitForTimeout(2000);

    // Refresh and verify deletion persisted
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.goto('http://localhost:5173/admin/events');
    await eventCard.click();
    await page.waitForSelector('[data-testid="event-edit-form"]');

    await expect(page.locator(`text=${editedSessionName}`)).not.toBeVisible();
    console.log(`✅ Session deletion persisted successfully`);

    // Take final screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react./test-results/persistence-final-state.png', fullPage: true });

    console.log('🎉 All persistence tests completed successfully!');
  });

  test('API Response Validation - Check Volunteer Positions in API', async ({ page, request }) => {
    console.log('🔍 Validating API responses include volunteer positions...');

    // Get events from API and check volunteer positions are included
    const eventsResponse = await request.get('http://localhost:5655/api/events');
    const eventsData = await eventsResponse.json();

    console.log('📊 API Response Analysis:');
    console.log(`Total events returned: ${eventsData.length}`);

    // Check each event for volunteer positions
    let eventsWithVolunteers = 0;
    let totalVolunteerPositions = 0;

    for (const event of eventsData) {
      if (event.volunteerPositions && event.volunteerPositions.length > 0) {
        eventsWithVolunteers++;
        totalVolunteerPositions += event.volunteerPositions.length;
        console.log(`✅ Event "${event.title}" has ${event.volunteerPositions.length} volunteer positions`);
      } else {
        console.log(`❌ Event "${event.title}" missing volunteer positions in API response`);
      }
    }

    console.log(`📈 Summary: ${eventsWithVolunteers}/${eventsData.length} events have volunteer positions`);
    console.log(`📈 Total volunteer positions: ${totalVolunteerPositions}`);

    // Expect at least one event to have volunteer positions
    expect(eventsWithVolunteers).toBeGreaterThan(0);
    expect(totalVolunteerPositions).toBeGreaterThan(0);

    console.log('✅ API response validation completed successfully!');
  });

  test('Network Error Monitoring', async ({ page }) => {
    console.log('🌐 Monitoring network requests for errors...');

    const networkErrors: string[] = [];

    page.on('response', response => {
      if (!response.ok()) {
        networkErrors.push(`${response.status()} ${response.url()}`);
        console.log(`❌ Network Error: ${response.status()} ${response.url()}`);
      } else {
        console.log(`✅ Success: ${response.status()} ${response.url()}`);
      }
    });

    page.on('requestfailed', request => {
      networkErrors.push(`FAILED ${request.url()}`);
      console.log(`❌ Request Failed: ${request.url()}`);
    });

    // Navigate to events page and perform basic operations
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');

    // Click on first event to trigger API calls
    const eventCard = page.locator('[data-testid="event-card"]').first();
    if (await eventCard.isVisible()) {
      await eventCard.click();
      await page.waitForLoadState('networkidle');
    }

    // Check for network errors
    if (networkErrors.length > 0) {
      console.log('❌ Network errors detected:');
      networkErrors.forEach(error => console.log(`  - ${error}`));
      throw new Error(`${networkErrors.length} network errors detected`);
    } else {
      console.log('✅ No network errors detected - all API calls successful!');
    }
  });
});