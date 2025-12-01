import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * TDD E2E Tests for Admin Events Edit Screen - UI Consistency
 *
 * STATUS: TDD RED PHASE - Tests for unimplemented UI consistency features
 * These tests are marked with test.fixme() because the features are not yet implemented.
 * They will pass once the UI consistency improvements are completed.
 *
 * Expected Failures (Features to Implement):
 * - Sessions and Tickets tabs may use modals but Volunteers tab uses bottom form
 * - Table layouts may not follow Edit-first-Delete-last pattern consistently
 * - "Add New" buttons may not be positioned consistently below grids
 * - Modal styling may not be consistent across all tabs
 * - Design System v7 button styles may not be applied consistently
 *
 * When implementing these features, convert test.fixme() back to test() to verify.
 */

/**
 * Helper to get a valid event ID from the admin events list
 */
async function getFirstEventId(page: Page): Promise<string> {
  await page.goto('/admin/events');
  await page.waitForLoadState('domcontentloaded');

  // Wait for the events table to load
  const eventsTable = page.locator('[data-testid="events-table"]');
  await expect(eventsTable).toBeVisible({ timeout: 10000 });

  // Click on the first event row
  const firstEventRow = page.locator('[data-testid="event-row"]').first();
  await expect(firstEventRow).toBeVisible();
  await firstEventRow.click();

  // Extract event ID from the URL
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);
  const url = page.url();
  const eventId = url.split('/admin/events/')[1]?.split('?')[0];

  if (!eventId) {
    throw new Error('Could not extract event ID from URL');
  }

  return eventId;
}

test.describe('Admin Events Edit Screen - UI Consistency', () => {
  let testEventId: string;

  test.beforeEach(async ({ page }) => {
    // Monitor console errors - tests should fail on JavaScript errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.error(`Console error: ${msg.text()}`);
      }
    });

    // Login as admin user using established pattern from lessons learned
    await AuthHelpers.loginAs(page, 'admin');

    // Get a valid event ID dynamically
    testEventId = await getFirstEventId(page);
  });

  test.fixme('all tabs should use modal dialogs consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Test Sessions tab uses modals - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const sessionsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();

    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible();
    await addSessionButton.click();

    // Should open modal, not inline form - HARD ASSERTION
    const sessionModal = page.locator('[data-testid="modal-add-session"]');
    await expect(sessionModal).toBeVisible();

    // Close modal for next test
    const cancelButton = sessionModal.locator('[data-testid="button-cancel-session"]');
    await expect(cancelButton).toBeVisible();
    await cancelButton.click();

    // Test Tickets tab uses modals - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const ticketsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(ticketsTab).toBeVisible();
    await ticketsTab.click();

    const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
    await expect(addTicketButton).toBeVisible();
    await addTicketButton.click();

    // Should open modal, not inline form - HARD ASSERTION
    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    await expect(ticketModal).toBeVisible();

    // Close modal for next test
    const ticketCancelButton = ticketModal.locator('[data-testid="button-cancel-ticket"]');
    await expect(ticketCancelButton).toBeVisible();
    await ticketCancelButton.click();

    // Test Volunteers tab uses modals (will fail - currently uses bottom form)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible({ timeout: 5000 });
    await volunteersTab.click();

    const addVolunteerButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addVolunteerButton).toBeVisible();
    await addVolunteerButton.click();

    // Should open modal, NOT inline form (will fail)
    const volunteerModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    await expect(volunteerModal).toBeVisible();

    // Should NOT see inline form at bottom (will fail with current implementation)
    const inlineForm = page.locator('[data-testid="form-volunteer-position-inline"]');
    await expect(inlineForm).not.toBeVisible();
  });

  test.fixme('should follow Edit-first-Delete-last table pattern', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Test Sessions table follows pattern - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const sessionsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    const sessionHeaderRow = sessionGrid.locator('thead tr').first();

    // First column should be Edit (will fail if not consistent)
    const sessionFirstHeader = sessionHeaderRow.locator('th').first();
    await expect(sessionFirstHeader).toContainText(/edit/i);

    // Last column should be Delete (will fail if not consistent)
    const sessionLastHeader = sessionHeaderRow.locator('th').last();
    await expect(sessionLastHeader).toContainText(/delete/i);

    // Body rows should follow same pattern - HARD ASSERTION
    const firstSessionRow = sessionGrid.locator('tbody tr').first();
    await expect(firstSessionRow).toBeVisible();

    const firstSessionCell = firstSessionRow.locator('td').first();
    await expect(firstSessionCell.locator('[data-testid="button-edit-session"]')).toBeVisible();

    const lastSessionCell = firstSessionRow.locator('td').last();
    await expect(lastSessionCell.locator('[data-testid="button-delete-session"]')).toBeVisible();

    // Test Tickets table follows pattern - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const ticketsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(ticketsTab).toBeVisible();
    await ticketsTab.click();

    const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
    await expect(ticketGrid).toBeVisible();

    const ticketHeaderRow = ticketGrid.locator('thead tr').first();

    // First column should be Edit
    const ticketFirstHeader = ticketHeaderRow.locator('th').first();
    await expect(ticketFirstHeader).toContainText(/edit/i);

    // Last column should be Delete
    const ticketLastHeader = ticketHeaderRow.locator('th').last();
    await expect(ticketLastHeader).toContainText(/delete/i);

    // Test Volunteers table follows pattern (will fail if not consistent)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await expect(volunteersTab).toBeVisible();
    await volunteersTab.click();

    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(volunteerGrid).toBeVisible();

    const headerRow = volunteerGrid.locator('thead tr').first();

    // First column should be Edit (will fail if not implemented)
    const firstHeader = headerRow.locator('th').first();
    await expect(firstHeader).toContainText(/edit/i);

    // Last column should be Delete (will fail if not implemented)
    const lastHeader = headerRow.locator('th').last();
    await expect(lastHeader).toContainText(/delete/i);

    // Body rows should follow same pattern - HARD ASSERTION
    const firstDataRow = volunteerGrid.locator('tbody tr').first();
    await expect(firstDataRow).toBeVisible();

    const firstCell = firstDataRow.locator('td').first();
    await expect(firstCell.locator('[data-testid="button-edit-volunteer-position"]')).toBeVisible();

    const lastCell = firstDataRow.locator('td').last();
    await expect(lastCell.locator('[data-testid="button-delete-volunteer-position"]')).toBeVisible();
  });

  test.fixme('should have Add New buttons positioned below grids consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Test Sessions tab has Add button below grid - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const sessionsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible();

    const sessionGridBox = await sessionGrid.boundingBox();
    const sessionButtonBox = await addSessionButton.boundingBox();

    // Button should be below the grid
    expect(sessionButtonBox!.y).toBeGreaterThan(sessionGridBox!.y + sessionGridBox!.height);

    // Test Tickets tab has Add button below grid - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const ticketsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(ticketsTab).toBeVisible();
    await ticketsTab.click();

    const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
    await expect(ticketGrid).toBeVisible();

    const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
    await expect(addTicketButton).toBeVisible();

    const ticketGridBox = await ticketGrid.boundingBox();
    const ticketButtonBox = await addTicketButton.boundingBox();

    // Button should be below the grid
    expect(ticketButtonBox!.y).toBeGreaterThan(ticketGridBox!.y + ticketGridBox!.height);

    // Test Volunteers tab has Add button below grid (will fail - uses bottom form)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();

    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(volunteerGrid).toBeVisible();

    const addVolunteerButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addVolunteerButton).toBeVisible();

    const gridBox = await volunteerGrid.boundingBox();
    const buttonBox = await addVolunteerButton.boundingBox();

    // Button should be below the grid, not part of an inline form (will fail)
    expect(buttonBox!.y).toBeGreaterThan(gridBox!.y + gridBox!.height);
  });

  test.fixme('should apply consistent modal styling across all tabs', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Collect modal styling from different tabs for comparison
    const modalStyles: Array<{tab: string, styles: any}> = [];

    // Test Sessions modal styling - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const sessionsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();

    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible();
    await addSessionButton.click();

    const sessionModal = page.locator('[data-testid="modal-add-session"]');
    await expect(sessionModal).toBeVisible();

    const sessionStyles = await sessionModal.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        borderRadius: computed.borderRadius,
        padding: computed.padding,
        backgroundColor: computed.backgroundColor,
        boxShadow: computed.boxShadow,
        width: computed.width,
        maxWidth: computed.maxWidth
      };
    });
    modalStyles.push({ tab: 'sessions', styles: sessionStyles });

    // Close modal
    await page.keyboard.press('Escape');

    // Test Tickets modal styling - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const ticketsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(ticketsTab).toBeVisible();
    await ticketsTab.click();

    const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
    await expect(addTicketButton).toBeVisible();
    await addTicketButton.click();

    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    await expect(ticketModal).toBeVisible();

    const ticketStyles = await ticketModal.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        borderRadius: computed.borderRadius,
        padding: computed.padding,
        backgroundColor: computed.backgroundColor,
        boxShadow: computed.boxShadow,
        width: computed.width,
        maxWidth: computed.maxWidth
      };
    });
    modalStyles.push({ tab: 'tickets', styles: ticketStyles });

    // Close modal
    await page.keyboard.press('Escape');

    // Test Volunteers modal styling (will fail because modal doesn't exist)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();

    const addVolunteerButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await addVolunteerButton.click();

    const volunteerModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    await expect(volunteerModal).toBeVisible();

    const volunteerStyles = await volunteerModal.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        borderRadius: computed.borderRadius,
        padding: computed.padding,
        backgroundColor: computed.backgroundColor,
        boxShadow: computed.boxShadow,
        width: computed.width,
        maxWidth: computed.maxWidth
      };
    });
    modalStyles.push({ tab: 'volunteers', styles: volunteerStyles });

    // Verify all modals have consistent styling (will fail if inconsistent)
    expect(modalStyles.length).toBeGreaterThan(1);

    const baseStyles = modalStyles[0].styles;

    for (let i = 1; i < modalStyles.length; i++) {
      const compareStyles = modalStyles[i].styles;

      // Check key styling properties are consistent
      expect(compareStyles.borderRadius).toBe(baseStyles.borderRadius);
      expect(compareStyles.backgroundColor).toBe(baseStyles.backgroundColor);
      expect(compareStyles.padding).toBe(baseStyles.padding);
      // Note: width might differ based on content, but maxWidth should be consistent
      expect(compareStyles.maxWidth).toBe(baseStyles.maxWidth);
    }
  });

  test.fixme('should use Design System v7 button styles consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Test button styles across all tabs
    const buttonStyles: Array<{location: string, element: string, styles: any}> = [];

    // Test Sessions tab buttons - HARD ASSERTION
    // Use role="tab" to distinguish from panel with same data-testid
    const sessionsTab = page.locator('[role="tab"][data-testid="setup-tab"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();

    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible();

    const sessionButtonStyles = await addSessionButton.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        fontFamily: computed.fontFamily,
        fontSize: computed.fontSize,
        fontWeight: computed.fontWeight,
        borderRadius: computed.borderRadius,
        padding: computed.padding,
        backgroundColor: computed.backgroundColor,
        color: computed.color,
        border: computed.border
      };
    });
    buttonStyles.push({ location: 'sessions', element: 'add-button', styles: sessionButtonStyles });

    // Test Volunteers tab buttons (will fail if not using Design System v7)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();

    const addVolunteerButton = page.locator('[data-testid="button-add-volunteer-position"]');
    await expect(addVolunteerButton).toBeVisible();

    const volunteerButtonStyles = await addVolunteerButton.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        fontFamily: computed.fontFamily,
        fontSize: computed.fontSize,
        fontWeight: computed.fontWeight,
        borderRadius: computed.borderRadius,
        padding: computed.padding,
        backgroundColor: computed.backgroundColor,
        color: computed.color,
        border: computed.border
      };
    });
    buttonStyles.push({ location: 'volunteers', element: 'add-button', styles: volunteerButtonStyles });

    // Test Edit/Delete buttons in grid - HARD ASSERTION
    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    await expect(volunteerGrid).toBeVisible();

    const editButton = volunteerGrid.locator('[data-testid="button-edit-volunteer-position"]').first();
    await expect(editButton).toBeVisible();

    const editStyles = await editButton.evaluate(el => {
      const computed = window.getComputedStyle(el);
      return {
        fontFamily: computed.fontFamily,
        fontSize: computed.fontSize,
        fontWeight: computed.fontWeight,
        borderRadius: computed.borderRadius,
        padding: computed.padding
      };
    });
    buttonStyles.push({ location: 'volunteers', element: 'edit-button', styles: editStyles });

    // Verify Design System v7 standards (will fail if not applied)
    expect(buttonStyles.length).toBeGreaterThan(0);

    for (const buttonInfo of buttonStyles) {
      const { styles } = buttonInfo;

      // Design System v7 requirements (these will fail if not implemented)
      expect(styles.fontFamily).toContain('Source Sans 3'); // Or whatever Design System v7 specifies
      expect(styles.borderRadius).toMatch(/^\d+px$/); // Should have consistent border radius
      expect(['400', '500', '600', '700']).toContain(styles.fontWeight); // Standard font weights

      // Should have consistent padding
      expect(styles.padding).toMatch(/^\d+px \d+px$/);
    }
  });

  test.fixme('should have consistent tab navigation and layout', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Verify all required tabs exist (will fail if tabs not implemented)
    // Note: setup-tab has both a tab button and panel with same data-testid
    // Use role="tab" to target only the button element
    // Tab names must match actual UI text (updated from business requirements)
    const requiredTabs = [
      { testId: 'setup-tab', name: 'Sessions' },
      { testId: 'rsvp-tickets-tab', name: 'RSVP/Tickets' },
      { testId: 'tab-volunteers', name: 'Volunteers' }
    ];

    const tabContainer = page.locator('[data-testid="tabs-event-management"]');
    await expect(tabContainer).toBeVisible();

    for (const tab of requiredTabs) {
      // Use role="tab" to distinguish from panel with same data-testid
      const tabElement = page.locator(`[role="tab"][data-testid="${tab.testId}"]`);
      await expect(tabElement).toBeVisible();
      await expect(tabElement).toContainText(tab.name);
    }

    // Test tab switching functionality
    for (const tab of requiredTabs) {
      // Use role="tab" to distinguish from panel with same data-testid
      const tabElement = page.locator(`[role="tab"][data-testid="${tab.testId}"]`);
      await tabElement.click();

      // Verify tab becomes active (visual indicator)
      await expect(tabElement).toHaveAttribute('aria-selected', 'true');

      // Verify corresponding content panel is visible
      // Mantine Tabs panels use role="tabpanel" with same data-testid as tab button
      const contentPanel = page.locator(`[role="tabpanel"][data-testid="${tab.testId}"]`);
      await expect(contentPanel).toBeVisible();
    }
  });

  test.fixme('should show loading states consistently across all operations', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();

    // Mock slow API responses to test loading states
    await page.route('**/api/events/1/**', route => {
      // Add delay to see loading states
      setTimeout(() => {
        route.continue();
      }, 2000);
    });

    // Test loading states in Volunteers tab (will fail if not implemented)
    // Use role="tab" to distinguish from panel with same data-testid
    const volunteersTab = page.locator('[role="tab"][data-testid="tab-volunteers"]');
    await volunteersTab.click();
    
    await page.locator('[data-testid="button-add-volunteer-position"]').click();
    await page.locator('[data-testid="input-position-name"]').fill('Test Position');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('1');
    
    const saveButton = page.locator('[data-testid="button-save-volunteer-position"]');
    await saveButton.click();
    
    // Should show loading state consistently (will fail if not implemented)
    await expect(saveButton).toHaveAttribute('disabled');
    await expect(saveButton.locator('[data-testid="loading-spinner"]')).toBeVisible();
    await expect(saveButton).toContainText(/saving|loading/i);
    
    // Loading indicator should disappear after operation completes
    await expect(saveButton.locator('[data-testid="loading-spinner"]')).not.toBeVisible({ timeout: 15000 });
    await expect(saveButton).not.toHaveAttribute('disabled');
  });
});