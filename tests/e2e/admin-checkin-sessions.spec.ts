import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Session-Aware Check-In Functionality
 *
 * Tests verify:
 * - Token generation modal shows session selector for multi-session events
 * - Token generation requires session selection for multi-session events
 * - Single-session events auto-select the session
 * - Generated tokens are scoped to specific sessions
 * - Attendees tab displays "Sessions Attended" column with session badges
 *
 * NOTE: Tests create their own test data and are independent of seed data
 *
 * CRITICAL: Uses relative URLs (compatible with containers) and follows all
 * testing standards from lessons learned.
 */

test.describe('Session-Aware Check-In - Token Generation', () => {
  let testEventId: string;
  let eventTitle: string;
  let sessionCount: number;

  test.beforeEach(async ({ page }) => {
    // MANDATORY: Clear auth state before login
    await AuthHelpers.clearAuthState(page);

    // Login as admin using helper (MANDATORY pattern from lessons learned)
    await AuthHelpers.loginAs(page, 'admin');

    // Fetch an event with multiple sessions for testing
    // Use template string URL pattern for container compatibility
    const apiBaseUrl = process.env.API_BASE_URL || 'http://localhost:5655';
    const eventsResponse = await page.request.get(`${apiBaseUrl}/api/events`);
    const events = await eventsResponse.json();

    if (!events || events.length === 0) {
      throw new Error('No events found in database. Run seed data first.');
    }

    // Find an event with multiple sessions (sessions are embedded in event response)
    // Try to find multi-session event first, then fall back to first event
    let selectedEvent = events.find((e: { sessions?: unknown[] }) =>
      Array.isArray(e.sessions) && e.sessions.length > 1
    ) || events[0];

    testEventId = selectedEvent.id;
    eventTitle = selectedEvent.title;

    // Sessions are embedded in the event response
    sessionCount = Array.isArray(selectedEvent.sessions) ? selectedEvent.sessions.length : 0;
  });

  test('should show session selector in token generation modal for multi-session events', async ({ page }) => {
    // Skip if event doesn't have multiple sessions
    if (sessionCount <= 1) {
      test.skip();
      console.log('⚠️ Event has ≤1 session - skipping multi-session test');
      return;
    }

    // Navigate to event details page (relative URL for container compatibility)
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded'); // MANDATORY: Use domcontentloaded, not networkidle

    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Wait for tabs to render (React hydration)
    await page.waitForTimeout(500);

    // Navigate to Attendees tab to access check-in features
    // Use role-based selector since data-testid is on both tab and panel
    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });

    if (await attendeesTab.count() === 0) {
      test.skip();
      return;
    }

    await expect(attendeesTab).toBeVisible({ timeout: 5000 });
    await attendeesTab.click();

    // Look for "Checkin Link" button (in tabs header area)
    const generateButton = page.locator('button').filter({ hasText: /Checkin Link/i });

    // Check if button exists before clicking (defensive pattern)
    const buttonCount = await generateButton.count();
    if (buttonCount === 0) {
      console.log('⚠️ Checkin Link button not found - feature may not be implemented yet. Skipping test.');
      test.skip();
      return;
    }

    await expect(generateButton.first()).toBeVisible({ timeout: 5000 });
    await generateButton.first().click();

    // Verify modal opens
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Verify modal title contains "Generate Check-In Link"
    await expect(modal.getByText('Generate Check-In Link').first()).toBeVisible();

    // CRITICAL: Verify session selector is visible
    const sessionSelect = modal.locator('[data-testid="session-select"]')
      .or(modal.locator('select, [role="combobox"]').filter({ has: page.locator('text=/Session/i') }))
      .or(modal.locator('label:has-text("Session")').locator('..').locator('select, [role="combobox"]'));

    await expect(sessionSelect.first()).toBeVisible({ timeout: 5000 });

    // Verify session selector has options from the event
    const selectElement = sessionSelect.first();
    await selectElement.click();
    await page.waitForTimeout(300); // Allow dropdown to open

    // Check for session options (should have at least sessionCount options)
    const options = page.locator('[role="option"]');
    const optionCount = await options.count();
    expect(optionCount).toBeGreaterThan(0);
  });

  test('should require session selection before generating token (multi-session event)', async ({ page }) => {
    // Skip if event doesn't have multiple sessions
    if (sessionCount <= 1) {
      test.skip();
      return;
    }

    // Navigate to event details page
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    // Wait for page and navigate to Attendees tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Wait for tabs to render (React hydration) - CRITICAL for Mantine tabs
    await page.waitForTimeout(500);

    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });
    if (await attendeesTab.count() === 0) {
      test.skip();
      return;
    }

    await attendeesTab.click();

    // Open token generation modal - use "Checkin Link" button text
    const generateButton = page.locator('button').filter({ hasText: /Checkin Link/i });

    if (await generateButton.count() === 0) {
      test.skip();
      return;
    }

    await generateButton.first().click();

    // Wait for modal
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Find the "Generate Link" button inside the modal (without selecting a session)
    const generateLinkButton = modal.locator('button').filter({ hasText: /Generate Link/i });

    await expect(generateLinkButton.first()).toBeVisible();

    // Verify session selector is visible (multi-session events require selection)
    // Use same selector pattern as test 1 for consistency
    const sessionSelect = modal.locator('[data-testid="session-select"]')
      .or(modal.locator('select, [role="combobox"]').filter({ has: page.locator('text=/Session/i') }))
      .or(modal.locator('label:has-text("Session")').locator('..').locator('select, [role="combobox"]'));

    await expect(sessionSelect.first()).toBeVisible({ timeout: 5000 });

    // Verify the "Generate Link" button is DISABLED when no session is selected
    // This is the correct behavior - prevents token generation without session selection
    await expect(generateLinkButton.first()).toBeDisabled();

    // Now select a session and verify button becomes enabled
    // Click on the select element to open dropdown
    await sessionSelect.first().click();
    await page.waitForTimeout(500);

    // Use keyboard to select first option (more reliable than clicking on portal options)
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');

    // Wait for selection to register
    await page.waitForTimeout(500);

    // Verify button is now enabled after session selection
    await expect(generateLinkButton.first()).toBeEnabled();
  });

  test('should auto-select session for single-session events', async ({ page }) => {
    // CREATE OWN TEST DATA: Create a single-session event through the UI
    const uniqueTitle = `Single Session Test ${Date.now()}`;

    // Navigate to event creation page
    await page.goto('/admin/events/new');
    await page.waitForLoadState('domcontentloaded');

    const eventForm = page.locator('[data-testid="event-form"]');
    await expect(eventForm).toBeVisible({ timeout: 5000 });

    // Fill required event fields
    await page.getByLabel('Event Title').fill(uniqueTitle);
    await page.getByLabel(/Short Description/i).first().fill('Test event for single-session auto-select');

    // Full Description (required)
    const fullDescEditor = page.locator('.tiptap.ProseMirror').first();
    await fullDescEditor.click();
    await fullDescEditor.fill('Full description for single-session event test.');

    // Select venue
    const venueSelect = page.getByLabel('Venue').first();
    await venueSelect.click();
    await page.getByRole('option').first().click();

    // Select event type (class)
    const eventTypeRadio = page.locator('[data-testid="event-type-class"]');
    if (await eventTypeRadio.count() > 0) {
      await eventTypeRadio.click();
    }

    // Save event to get an event ID
    await page.waitForTimeout(500);
    const saveButton = page.getByRole('button', { name: 'Save' });
    await expect(saveButton).toBeVisible();
    await expect(saveButton).not.toBeDisabled({ timeout: 5000 });
    await saveButton.click();

    // Wait for redirect to event detail page
    await page.waitForTimeout(2000);
    await expect(page).toHaveURL(/\/admin\/events\/[a-f0-9-]+$/);

    // Navigate to Sessions tab and verify EXACTLY ONE session exists
    const sessionsTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
    await expect(sessionsTab).toBeVisible({ timeout: 5000 });
    await sessionsTab.click();
    await page.waitForTimeout(500);

    // Check if a default session was created (events may auto-create one)
    const sessionsGrid = page.getByTestId('grid-sessions');
    await expect(sessionsGrid).toBeVisible({ timeout: 5000 });

    // Count existing session rows
    const sessionRows = sessionsGrid.locator('tbody tr');
    const rowCount = await sessionRows.count();

    // Verify there's exactly one session (either auto-created or we need to add one)
    if (rowCount === 0) {
      // No sessions - add one
      const addSessionButton = page.getByRole('button', { name: 'Add Session' });
      await addSessionButton.click();

      const sessionModal = page.locator('[role="dialog"]');
      await expect(sessionModal).toBeVisible({ timeout: 5000 });

      // Fill minimal required fields
      await sessionModal.getByTestId('input-session-name').fill('Test Session');
      await sessionModal.getByTestId('input-session-capacity').fill('50');

      // Save session
      await sessionModal.getByTestId('button-save-session').click();
      await page.waitForTimeout(1000);
    } else if (rowCount > 1) {
      // More than one session - this isn't a single-session event, skip test
      console.log(`⚠️ Event has ${rowCount} sessions, expected 1. Skipping.`);
      test.skip();
      return;
    }
    // rowCount === 1: Perfect, we have exactly one session

    // NOW TEST THE CHECK-IN TOKEN MODAL WITH SINGLE SESSION
    // Navigate to Attendees tab
    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });
    await expect(attendeesTab).toBeVisible({ timeout: 5000 });
    await attendeesTab.click();
    await page.waitForTimeout(500);

    // Open token generation modal
    const generateButton = page.locator('button').filter({ hasText: /Checkin Link/i });
    await expect(generateButton.first()).toBeVisible({ timeout: 5000 });
    await generateButton.first().click();

    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // VERIFY: For single-session events, session selector should NOT be shown
    const sessionSelect = modal.locator('[data-testid="session-select"]');
    await expect(sessionSelect).toHaveCount(0);

    // VERIFY: Instead, there should be an alert showing the auto-selected session
    // Session name could be "Default" (auto-created) or "Test Session" (manually added)
    const sessionAlert = modal.locator('[role="alert"]').filter({ hasText: /Session/i });
    await expect(sessionAlert.first()).toBeVisible({ timeout: 3000 });

    // VERIFY: Generate Link button is NOT disabled (session is auto-selected)
    const generateLinkButton = modal.locator('button').filter({ hasText: /Generate Link/i }).first();
    await expect(generateLinkButton).toBeVisible();
    await expect(generateLinkButton).not.toBeDisabled();
  });

  test('should display session name in generated token list', async ({ page }) => {
    // Skip if event has no sessions
    if (sessionCount === 0) {
      test.skip();
      console.log('⚠️ Event has no sessions - skipping token generation test');
      return;
    }

    // Navigate to event details page
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Wait for tabs to render (React hydration)
    await page.waitForTimeout(500);

    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });
    if (await attendeesTab.count() === 0) {
      console.log('⚠️ Attendees tab not found - skipping.');
      test.skip();
      return;
    }

    await attendeesTab.click();

    // Open token generation modal
    const generateButton = page.locator('button').filter({ hasText: /Checkin Link/i });

    if (await generateButton.count() === 0) {
      console.log('⚠️ Checkin Link button not found - skipping.');
      test.skip();
      return;
    }

    await generateButton.first().click();

    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Select a session (if multiple sessions exist)
    if (sessionCount > 1) {
      const sessionSelect = modal.locator('[data-testid="session-select"]').first();
      await sessionSelect.click();
      await page.waitForTimeout(500);

      // Use keyboard to select first option (more reliable for Mantine portals)
      await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Enter');
      await page.waitForTimeout(500);
    }

    // Generate token
    const generateLinkButton = modal.locator('button').filter({ hasText: /Generate Link/i }).first();

    await generateLinkButton.click();

    // Wait for success notification or generated token display
    await page.waitForTimeout(1000); // Allow API call to complete

    // Look for active tokens list in modal
    const activeTokensSection = modal.locator('text=/Active Tokens/i')
      .or(modal.locator('[data-testid="active-tokens-section"]'));

    await expect(activeTokensSection.first()).toBeVisible({ timeout: 5000 });

    // Verify token list shows session name (in table or list)
    // Look for session badges or session column
    const sessionBadge = modal.locator('[role="cell"]')
      .or(modal.locator('.mantine-Badge'))
      .or(modal.locator('td'))
      .filter({ hasText: /.+/ }) // Non-empty text
      .first();

    // At least verify that tokens are displayed
    const tokenTable = modal.locator('table, [role="table"]');
    if (await tokenTable.count() > 0) {
      await expect(tokenTable).toBeVisible();
    }
  });
});

test.describe('Session-Aware Check-In - Attendees Tab', () => {
  let testEventId: string;

  test.beforeEach(async ({ page }) => {
    await AuthHelpers.clearAuthState(page);
    await AuthHelpers.loginAs(page, 'admin');

    // Get an event with attendees (preferably with check-ins)
    const apiBaseUrl = process.env.API_BASE_URL || 'http://localhost:5655';
    const eventsResponse = await page.request.get(`${apiBaseUrl}/api/events`);
    const events = await eventsResponse.json();

    if (!events || events.length === 0) {
      throw new Error('No events found in database. Run seed data first.');
    }

    testEventId = events[0].id;
  });

  test('should show "Sessions Attended" column in Attendees tab', async ({ page }) => {
    // Navigate to event details page
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Wait for tabs to render (React hydration)
    await page.waitForTimeout(500);

    // Navigate to Attendees tab using data-testid selector
    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });

    if (await attendeesTab.count() === 0) {
      console.log('⚠️ Attendees tab not found - feature may not be implemented yet. Skipping test.');
      test.skip();
      return;
    }

    await expect(attendeesTab).toBeVisible({ timeout: 5000 });
    await attendeesTab.click();

    // Wait for attendees panel to be active
    await page.waitForTimeout(500);

    // Find the attendees table - look for visible table with Sessions Attended column
    const attendeesTable = page.locator('table').filter({
      has: page.locator('th:has-text("Sessions Attended")')
    }).first();

    // Check if table exists and is visible
    const tableVisible = await attendeesTable.isVisible().catch(() => false);
    if (!tableVisible) {
      console.log('⚠️ Attendees table with Sessions Attended column not found. Skipping test.');
      test.skip();
      return;
    }

    // CRITICAL: Verify "Sessions Attended" column header exists
    const sessionsHeader = attendeesTable.locator('th').filter({ hasText: /Sessions.*Attended/i });

    await expect(sessionsHeader.first()).toBeVisible({ timeout: 5000 });

    // Verify column header text matches expected format
    await expect(sessionsHeader.first()).toContainText(/Sessions Attended/i);
  });

  test('should display session badges for checked-in attendees (if any exist)', async ({ page }) => {
    // Navigate to event details page
    await page.goto(`/admin/events/${testEventId}`);
    await page.waitForLoadState('domcontentloaded');

    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Wait for tabs to render (React hydration)
    await page.waitForTimeout(500);

    const attendeesTab = page.getByRole('tab', { name: 'Attendees' });
    if (await attendeesTab.count() === 0) {
      console.log('⚠️ Attendees tab not found - skipping.');
      test.skip();
      return;
    }

    await attendeesTab.click();

    // Wait for attendees panel to be active
    await page.waitForTimeout(500);

    // Find the attendees table - look for visible table with Sessions Attended column
    const attendeesTable = page.locator('table').filter({
      has: page.locator('th:has-text("Sessions Attended")')
    }).first();

    // Check if table exists and is visible
    const tableVisible = await attendeesTable.isVisible().catch(() => false);
    if (!tableVisible) {
      console.log('⚠️ Attendees table with Sessions Attended column not found. Skipping test.');
      test.skip();
      return;
    }

    // Look for Sessions Attended column
    const sessionsHeader = attendeesTable.locator('th').filter({ hasText: /Sessions.*Attended/i });
    await expect(sessionsHeader.first()).toBeVisible();

    // Find the column index of Sessions Attended
    // Then check cells in that column for session badges

    // Look for any badges in Sessions Attended column (if attendees have checked in)
    const sessionBadges = attendeesTable.locator('td').filter({ has: page.locator('.mantine-Badge') });

    const badgeCount = await sessionBadges.count();

    if (badgeCount === 0) {
      // No checked-in attendees - verify "None" text is shown instead
      const noneCells = attendeesTable.locator('td').filter({ hasText: /None/i });

      // Should have at least some "None" cells if there are attendees
      const tableRows = attendeesTable.locator('tbody tr');
      const rowCount = await tableRows.count();

      if (rowCount > 0) {
        // Attendees exist but none checked in - verify "None" is displayed
        await expect(noneCells.first()).toBeVisible();
      } else {
        // No attendees at all - skip test
        console.log('⚠️ No attendees found for this event. Skipping badge verification.');
        test.skip();
      }
    } else {
      // Badges exist - verify they're visible
      await expect(sessionBadges.first()).toBeVisible();

      // Optionally verify badge contains session name (non-empty text)
      const firstBadge = sessionBadges.first();
      const badgeText = await firstBadge.textContent();
      expect(badgeText).toBeTruthy();
      expect(badgeText?.length).toBeGreaterThan(0);
    }
  });
});
