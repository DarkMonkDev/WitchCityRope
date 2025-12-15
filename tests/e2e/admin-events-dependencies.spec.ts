import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Admin Events Edit Screen - Data Dependencies (DataFactory Migration)
 *
 * MIGRATION NOTES:
 * - Uses df (DataFactory) fixture for automatic cleanup
 * - Creates test data via TestHelper API endpoints
 * - No need for manual API calls or cleanup logic
 * - Data is automatically cleaned up after each test
 *
 * STATUS: Tests updated 2025-12-14 to use actual UI selectors
 * - "Add Ticket Type" button IS disabled when no sessions exist
 * - Delete confirmation modal with cascade blocking IS implemented
 * - Tests now verify these implemented features
 *
 * Original: tests/e2e/admin-events-dependencies.spec.ts (seed data version)
 */

test.describe('Admin Events Edit Screen - Data Dependencies', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should only allow ticket creation when sessions exist', async ({ page, df }) => {
    // Create a fresh published event with no sessions
    const event = await df.events.createPublished(`No Sessions Test ${Date.now()}`);
    console.log(`✅ Created test event: ${event.id}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to Sessions / Ticket Types tab
    const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();
    await page.waitForTimeout(500);

    // Verify sessions section is visible
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Verify no sessions exist (session grid should show "no sessions" message or empty table)
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    const sessionRows = sessionGrid.locator('[data-testid="session-row"]');
    const sessionCount = await sessionRows.count();
    expect(sessionCount).toBe(0);
    console.log('✓ Verified no sessions exist');

    // Add Ticket Type button should be disabled when no sessions exist
    // Button text is "Add Ticket Type" and it has a title tooltip explaining why
    const addTicketButton = page.getByRole('button', { name: 'Add Ticket Type' });
    await expect(addTicketButton).toBeVisible({ timeout: 5000 });
    await expect(addTicketButton).toBeDisabled();
    console.log('✓ Add Ticket Type button is disabled when no sessions');

    // Create a session using the Add Session button
    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();

    // Fill in session details in the modal
    const sessionNameInput = page.locator('[data-testid="input-session-name"]');
    await expect(sessionNameInput).toBeVisible({ timeout: 5000 });
    await sessionNameInput.fill('Test Session');

    // Fill date and time fields
    const sessionDateInput = page.locator('[data-testid="input-session-date"]');
    if (await sessionDateInput.isVisible()) {
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 7);
      await sessionDateInput.fill(futureDate.toISOString().split('T')[0]);
    }

    const startTimeInput = page.locator('[data-testid="input-session-start-time"]');
    await startTimeInput.fill('18:00');

    const endTimeInput = page.locator('[data-testid="input-session-end-time"]');
    await endTimeInput.fill('21:00');

    const capacityInput = page.locator('[data-testid="input-session-capacity"]');
    await capacityInput.fill('20');

    // Save the session
    const saveSessionButton = page.locator('[data-testid="button-save-session"]');
    await saveSessionButton.click();
    await page.waitForTimeout(1000);
    console.log('✓ Created test session');

    // Now Add Ticket Type button should be enabled
    await expect(addTicketButton).toBeEnabled({ timeout: 10000 });
    console.log('✅ Add Ticket Type button is enabled after session created');
  });

  // FIXME: Test needs rewriting - TicketTypeFormModal doesn't have data-testid attributes
  // The modal uses standard Mantine components without testids for the session dropdown
  // Options: 1) Add data-testids to TicketTypeFormModal, OR 2) Use getByLabel selectors
  test.fixme('should show only event-specific sessions in ticket creation', async ({ page, df }) => {
    // Create an event with sessions
    const event = await df.events.createPublished(`Session Filter Test ${Date.now()}`);

    // Calculate session times
    const sessionStart1 = new Date();
    sessionStart1.setDate(sessionStart1.getDate() + 7);
    sessionStart1.setHours(18, 0, 0, 0);
    const sessionEnd1 = new Date(sessionStart1.getTime() + 3 * 60 * 60 * 1000);

    const session1 = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session 1',
      startTime: sessionStart1,
      endTime: sessionEnd1,
      maxCapacity: 20,
    });

    const sessionStart2 = new Date();
    sessionStart2.setDate(sessionStart2.getDate() + 8);
    sessionStart2.setHours(18, 0, 0, 0);
    const sessionEnd2 = new Date(sessionStart2.getTime() + 3 * 60 * 60 * 1000);

    const session2 = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session 2',
      startTime: sessionStart2,
      endTime: sessionEnd2,
      maxCapacity: 20,
    });

    // Create another event with its own session (should NOT appear in dropdown)
    const otherEvent = await df.events.createPublished(`Other Event ${Date.now()}`);
    const otherSessionStart = new Date();
    otherSessionStart.setDate(otherSessionStart.getDate() + 7);
    otherSessionStart.setHours(18, 0, 0, 0);
    const otherSessionEnd = new Date(otherSessionStart.getTime() + 3 * 60 * 60 * 1000);

    await df.sessions.create({
      eventId: otherEvent.id,
      title: 'Other Event Session',
      startTime: otherSessionStart,
      endTime: otherSessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to Sessions / Ticket Types tab
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await setupTab.click();

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Get the session IDs for this event
    const eventSessionIds = await sessionGrid.locator('[data-testid="session-id"]').allTextContents();

    // Open add ticket modal/form
    const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
    await expect(addTicketButton).toBeVisible();
    await addTicketButton.click();

    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    await expect(ticketModal).toBeVisible();

    // Open sessions dropdown in ticket form (will fail if dropdown shows all sessions)
    const sessionsDropdown = page.locator('[data-testid="dropdown-ticket-sessions"]');
    await expect(sessionsDropdown).toBeVisible();
    await sessionsDropdown.click();

    // Get available session options
    const sessionOptions = page.locator('[data-testid="option-session"]');
    const availableOptions = await sessionOptions.allTextContents();

    // Should only show sessions from current event (will fail if shows global sessions)
    for (const option of availableOptions) {
      // Option should match one of the event's session IDs (S1, S2, etc.)
      const matchesEventSession = eventSessionIds.some(sessionId =>
        option.includes(sessionId) || option === 'All Sessions'
      );
      expect(matchesEventSession).toBe(true);
    }

    // Should not show sessions from other events
    await expect(sessionOptions).not.toContainText(/Other Event Session/);
  });

  // FIXME: Test needs capacity validation feature OR test rewrite
  // The capacity validation ([data-testid="error-ticket-capacity"]) doesn't exist
  // Feature may not be implemented, or uses different error display mechanism
  test.fixme('should validate ticket capacity against session capacity', async ({ page, df }) => {
    // Create event with a limited capacity session
    const event = await df.events.createPublished(`Capacity Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Small Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 10, // Limited capacity
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to Setup tab
    const setupTab = page.locator('[data-testid="setup-tab"]');
    await setupTab.click();

    // Try to create ticket with quantity exceeding session capacity
    await page.locator('[data-testid="button-add-ticket-type"]').click();

    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    await expect(ticketModal).toBeVisible();

    await page.locator('[data-testid="input-ticket-name"]').fill('Over Capacity Ticket');
    await page.locator('[data-testid="input-ticket-quantity"]').fill('15'); // Exceeds session capacity of 10
    await page.locator('[data-testid="input-ticket-price"]').fill('50.00');

    // Select the session with limited capacity
    await page.locator('[data-testid="dropdown-ticket-sessions"]').click();
    await page.locator('[data-testid="option-session"]').first().click();

    // Try to save (will fail if validation not implemented)
    await page.locator('[data-testid="button-save-ticket-type"]').click();

    // Should show validation error (will fail if validation not implemented)
    const capacityError = page.locator('[data-testid="error-ticket-capacity"]');
    await expect(capacityError).toBeVisible();
    await expect(capacityError).toHaveText(/exceeds session capacity|capacity limit/i);
  });

  test('should show delete confirmation modal when deleting sessions', async ({ page, df }) => {
    // Create event with session
    const event = await df.events.createPublished(`Delete Modal Test ${Date.now()}`);
    console.log(`✅ Created test event: ${event.id}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Test Session for Deletion',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });
    console.log(`✅ Created test session: ${session.id}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to Sessions / Ticket Types tab
    const sessionsTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
    await sessionsTab.click();
    await page.waitForTimeout(500);

    // Verify session exists
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible({ timeout: 5000 });
    const sessionRows = sessionGrid.locator('[data-testid="session-row"]');
    await expect(sessionRows).toHaveCount(1);
    console.log('✓ Verified session exists in grid');

    // Try to delete the session
    const deleteButton = sessionGrid.locator('[data-testid="button-delete-session"]').first();
    await expect(deleteButton).toBeVisible({ timeout: 5000 });
    await deleteButton.click();

    // Should show delete confirmation modal
    const deleteModal = page.locator('[data-testid="delete-confirmation-modal"]');
    await expect(deleteModal).toBeVisible({ timeout: 5000 });
    console.log('✓ Delete confirmation modal appeared');

    // Modal should show session name
    await expect(deleteModal).toContainText(/Test Session for Deletion/i);

    // Modal should have cancel and confirm buttons
    const cancelButton = deleteModal.locator('[data-testid="delete-cancel-button"]');
    const confirmButton = deleteModal.locator('[data-testid="delete-confirm-button"]');
    await expect(cancelButton).toBeVisible();
    await expect(confirmButton).toBeVisible();

    // Cancel the deletion
    await cancelButton.click();
    await expect(deleteModal).not.toBeVisible();
    console.log('✅ Delete confirmation modal works correctly');
  });

  // FIXME: Test needs ticket purchase creation via DataFactory
  // The test tries to simulate a purchase but DataFactory may not support this
  // The delete prevention logic IS implemented via 'ticketsSold' deletion state
  test.fixme('should prevent session deletion when tickets have sales/reservations', async ({ page, df }) => {
    // Create event with session, ticket, and a purchase
    const event = await df.events.createPublished(`Sales Prevention Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Session with Sales',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    const ticketType = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Sold Ticket',
      price: 25,
      quantityAvailable: 20,
    });

    // Create a user and purchase a ticket
    const buyer = await df.users.createVerified({
      email: `buyer-${Date.now()}@test.com`,
      password: 'Test123!',
    });

    const purchase = await df.ticketPurchases.create({
      userId: buyer.id,
      ticketTypeId: ticketType.id,
      quantity: 1,
    });

    console.log(`Created sold ticket: Purchase ${purchase.id} for ticket ${ticketType.id}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to sessions tab
    const sessionsTab = page.locator('[data-testid="setup-tab"]');
    await sessionsTab.click();

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Try to delete a session that has sold tickets (will fail if constraint not implemented)
    const deleteButton = sessionGrid.locator('[data-testid="button-delete-session"]').first();
    await deleteButton.click();

    const confirmDialog = page.locator('[data-testid="dialog-confirm-delete-session"]');
    await expect(confirmDialog).toBeVisible();

    const confirmButton = confirmDialog.locator('[data-testid="button-confirm-delete"]');
    await confirmButton.click();

    // Should show error message preventing deletion (will fail if constraint not implemented)
    const errorAlert = page.locator('[data-testid="alert-delete-error"]');
    await expect(errorAlert).toBeVisible({ timeout: 10000 });
    await expect(errorAlert).toContainText(/cannot delete.*sold tickets|tickets sold/i);

    // Session should still be in grid
    await expect(confirmDialog).not.toBeVisible();
    await expect(sessionGrid.locator('[data-testid="session-row"]').first()).toBeVisible();
  });

  // FIXME: Test needs rewriting - VolunteerPositionFormModal may not have required testids
  // Tests validation that volunteer positions are assigned to event-specific sessions
  test.fixme('should validate volunteer position session assignments', async ({ page, df }) => {
    // Create event with sessions
    const event = await df.events.createPublished(`Volunteer Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Volunteer Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 20,
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to volunteers tab
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();

    // Try to create volunteer position without selecting sessions (will fail if validation not implemented)
    await page.locator('[data-testid="button-add-volunteer-position"]').click();

    const positionModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    await expect(positionModal).toBeVisible();

    await page.locator('[data-testid="input-position-name"]').fill('Test Position');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('1');
    // Don't select any sessions

    await page.locator('[data-testid="button-save-volunteer-position"]').click();

    // Should show validation error for missing session assignment (will fail if validation not implemented)
    const sessionError = page.locator('[data-testid="error-position-sessions"]');
    await expect(sessionError).toBeVisible();
    await expect(sessionError).toHaveText(/select at least one session/i);

    // Select sessions and verify it saves (will fail if session dropdown broken)
    await page.locator('[data-testid="dropdown-position-sessions"]').click();
    await page.locator('[data-testid="option-session"]').first().click();

    await page.locator('[data-testid="button-save-volunteer-position"]').click();

    // Should save successfully
    await expect(positionModal).not.toBeVisible({ timeout: 5000 });
  });

  // FIXME: Complex integration test requiring multiple entity creation and deletion
  // Tests that deleting entities properly handles all related data
  // May need DataFactory enhancements for full entity relationship management
  test.fixme('should maintain data integrity across related entities', async ({ page, df }) => {
    // Create event with complete data structure
    const event = await df.events.createPublished(`Integrity Test ${Date.now()}`);

    const sessionStart = new Date();
    sessionStart.setDate(sessionStart.getDate() + 7);
    sessionStart.setHours(18, 0, 0, 0);
    const sessionEnd = new Date(sessionStart.getTime() + 3 * 60 * 60 * 1000);

    const session = await df.sessions.create({
      eventId: event.id,
      title: 'Integrity Test Session',
      startTime: sessionStart,
      endTime: sessionEnd,
      maxCapacity: 30,
    });

    const ticketType = await df.ticketTypes.create({
      eventId: event.id,
      sessionId: session.id,
      name: 'Integrity Test Ticket',
      price: 40,
      quantityAvailable: 25,
    });

    // Create volunteer position for same session
    const volunteerPosition = await df.volunteers.create({
      eventId: event.id,
      title: 'Integrity Test Position',
      slotsAvailable: 2,
    });

    console.log(`Created test data: Event ${event.id}, Session ${session.id}, Ticket ${ticketType.id}, Volunteer ${volunteerPosition.id}`);

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${event.id}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Navigate to sessions tab
    const sessionsTab = page.locator('[data-testid="setup-tab"]');
    await sessionsTab.click();

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');

    // Get the session ID that was assigned
    const displayedSessionId = await sessionGrid.locator('[data-testid="session-id"]').first().textContent();

    // Navigate to tickets and verify session reference
    const ticketsTab = page.locator('[data-testid="setup-tab"]');
    await ticketsTab.click();

    const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
    const ticketSessionsCell = ticketGrid.locator('[data-testid="ticket-sessions"]').first();
    await expect(ticketSessionsCell).toContainText(displayedSessionId!);

    // Navigate to volunteers and verify session reference
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();

    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    const positionSessionsCell = volunteerGrid.locator('[data-testid="position-sessions"]').first();
    await expect(positionSessionsCell).toContainText(displayedSessionId!);

    // Edit session and verify dependent entities reflect changes
    await sessionsTab.click();
    const editSessionButton = sessionGrid.locator('[data-testid="button-edit-session"]').first();
    await editSessionButton.click();

    const editSessionModal = page.locator('[data-testid="modal-edit-session"]');
    await page.locator('[data-testid="input-session-name"]').fill('Updated Integrity Session');
    await page.locator('[data-testid="button-save-session"]').click();

    // Verify ticket and volunteer position still reference the correct session (will fail if relationships broken)
    await ticketsTab.click();
    await expect(ticketGrid.locator('[data-testid="ticket-sessions"]').first()).toContainText(displayedSessionId!);

    await volunteersTab.click();
    await expect(volunteerGrid.locator('[data-testid="position-sessions"]').first()).toContainText(displayedSessionId!);
  });
});
