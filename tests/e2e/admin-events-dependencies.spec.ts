import { test, expect } from '@playwright/test';
import { quickLogin } from './helpers/auth.helper';

/**
 * TDD E2E Tests for Admin Events Edit Screen - Data Dependencies
 * 
 * These tests are designed to FAIL initially (Red phase of TDD)
 * They test the data dependency issues described in the business requirements
 * 
 * Expected Failures:
 * - Ticket creation may be allowed even when no sessions exist
 * - Session dropdowns in ticket creation may show all platform sessions instead of event-specific ones
 * - Volunteer position dropdowns may show global sessions instead of event sessions
 * - Cascade delete operations may not be properly handled
 * - Data integrity validation may not be implemented
 */

test.describe('Admin Events Edit Screen - Data Dependencies', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await quickLogin(page, 'admin');
  });

  test('should only allow ticket creation when sessions exist', async ({ page }) => {
    // Navigate to admin event edit page (assuming a fresh event with no sessions)
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Navigate to Tickets tab
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    await expect(ticketsTab).toBeVisible({ timeout: 5000 });
    await ticketsTab.click();
    
    // Check if sessions exist for this event
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await expect(sessionsTab).toBeVisible();
    await sessionsTab.click();
    
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();
    
    const sessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    
    // Go back to Tickets tab
    await ticketsTab.click();
    
    if (sessionCount === 0) {
      // No sessions exist - should show message and disable ticket creation (will fail if not implemented)
      const noSessionsMessage = page.locator('[data-testid="message-no-sessions"]');
      await expect(noSessionsMessage).toBeVisible();
      await expect(noSessionsMessage).toContainText(/add sessions first/i);
      
      const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
      await expect(addTicketButton).toBeDisabled();
      
      // Create a session first
      await sessionsTab.click();
      await page.locator('[data-testid="button-add-session"]').click();
      await page.locator('[data-testid="input-session-name"]').fill('Test Session');
      await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
      await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
      await page.locator('[data-testid="input-session-capacity"]').fill('20');
      await page.locator('[data-testid="button-save-session"]').click();
      
      // Go back to tickets tab
      await ticketsTab.click();
      
      // Now ticket creation should be enabled (will fail if dependency not implemented)
      await expect(noSessionsMessage).not.toBeVisible();
      await expect(addTicketButton).toBeEnabled();
    } else {
      // Sessions exist - ticket creation should be enabled
      const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
      await expect(addTicketButton).toBeEnabled();
    }
  });

  test('should show only event-specific sessions in ticket creation', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Ensure we have sessions for this event
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await sessionsTab.click();
    
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();
    
    // Get the session IDs for this event
    const eventSessionIds = await sessionGrid.locator('[data-testid="session-id"]').allTextContents();
    
    // Navigate to Tickets tab
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    await ticketsTab.click();
    
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
    await expect(sessionOptions).not.toContainText(/Event \d+ Session|Other Event/);
  });

  test('should validate ticket capacity against session capacity', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Create a session with limited capacity first
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await sessionsTab.click();
    
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('Small Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('10'); // Limited capacity
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Navigate to Tickets tab
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    await ticketsTab.click();
    
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

  test('should handle cascade operations when deleting sessions with dependent tickets', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Create a session
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await sessionsTab.click();
    
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('Test Session for Deletion');
    await page.locator('[data-testid="input-session-start-time"]').fill('09:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('12:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('20');
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Create a ticket that depends on this session
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    await ticketsTab.click();
    
    await page.locator('[data-testid="button-add-ticket-type"]').click();
    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    await expect(ticketModal).toBeVisible();
    
    await page.locator('[data-testid="input-ticket-name"]').fill('Dependent Ticket');
    await page.locator('[data-testid="input-ticket-quantity"]').fill('10');
    await page.locator('[data-testid="input-ticket-price"]').fill('25.00');
    
    // Associate with the session we created
    await page.locator('[data-testid="dropdown-ticket-sessions"]').click();
    await page.locator('[data-testid="option-session"]').last().click(); // Select the session we just created
    await page.locator('[data-testid="button-save-ticket-type"]').click();
    
    // Now try to delete the session that has dependent tickets
    await sessionsTab.click();
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    const deleteButton = sessionGrid.locator('[data-testid="button-delete-session"]').last();
    await deleteButton.click();
    
    // Should show cascade warning (will fail if cascade handling not implemented)
    const cascadeDialog = page.locator('[data-testid="dialog-cascade-delete-warning"]');
    await expect(cascadeDialog).toBeVisible();
    await expect(cascadeDialog).toContainText(/dependent tickets/i);
    await expect(cascadeDialog).toContainText(/will also be deleted/i);
    
    // Show affected items (will fail if not implemented)
    const affectedItems = cascadeDialog.locator('[data-testid="list-affected-items"]');
    await expect(affectedItems).toBeVisible();
    await expect(affectedItems).toContainText('Dependent Ticket');
    
    // Confirm cascade deletion
    const confirmCascadeButton = cascadeDialog.locator('[data-testid="button-confirm-cascade-delete"]');
    await confirmCascadeButton.click();
    
    // Verify session is deleted
    await expect(cascadeDialog).not.toBeVisible();
    
    // Verify dependent ticket is also deleted
    await ticketsTab.click();
    const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
    await expect(ticketGrid.locator('[data-testid="ticket-name"]', { hasText: 'Dependent Ticket' })).not.toBeVisible();
  });

  test('should prevent session deletion when tickets have sales/reservations', async ({ page }) => {
    // This test assumes we have seed data with sold tickets
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Navigate to sessions tab
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
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

  test('should validate volunteer position session assignments', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
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

  test('should maintain data integrity across related entities', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Create a session
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    await sessionsTab.click();
    
    await page.locator('[data-testid="button-add-session"]').click();
    await page.locator('[data-testid="input-session-name"]').fill('Integrity Test Session');
    await page.locator('[data-testid="input-session-start-time"]').fill('14:00');
    await page.locator('[data-testid="input-session-end-time"]').fill('17:00');
    await page.locator('[data-testid="input-session-capacity"]').fill('30');
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Get the session ID that was assigned
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    const newSessionId = await sessionGrid.locator('[data-testid="session-id"]').last().textContent();
    
    // Create ticket type that references this session
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    await ticketsTab.click();
    
    await page.locator('[data-testid="button-add-ticket-type"]').click();
    const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
    
    await page.locator('[data-testid="input-ticket-name"]').fill('Integrity Test Ticket');
    await page.locator('[data-testid="input-ticket-quantity"]').fill('25');
    await page.locator('[data-testid="input-ticket-price"]').fill('40.00');
    
    await page.locator('[data-testid="dropdown-ticket-sessions"]').click();
    await page.locator('[data-testid="option-session"]', { hasText: newSessionId! }).click();
    await page.locator('[data-testid="button-save-ticket-type"]').click();
    
    // Create volunteer position for same session
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
    await volunteersTab.click();
    
    await page.locator('[data-testid="button-add-volunteer-position"]').click();
    const positionModal = page.locator('[data-testid="modal-add-volunteer-position"]');
    
    await page.locator('[data-testid="input-position-name"]').fill('Integrity Test Position');
    await page.locator('[data-testid="input-volunteers-needed"]').fill('2');
    
    await page.locator('[data-testid="dropdown-position-sessions"]').click();
    await page.locator('[data-testid="option-session"]', { hasText: newSessionId! }).click();
    await page.locator('[data-testid="button-save-volunteer-position"]').click();
    
    // Now verify all entities reference the correct session (will fail if relationships broken)
    
    // Check ticket shows correct session
    await ticketsTab.click();
    const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
    const ticketSessionsCell = ticketGrid.locator('[data-testid="ticket-sessions"]').last();
    await expect(ticketSessionsCell).toHaveText(newSessionId!);
    
    // Check volunteer position shows correct session
    await volunteersTab.click();
    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    const positionSessionsCell = volunteerGrid.locator('[data-testid="position-sessions"]').last();
    await expect(positionSessionsCell).toHaveText(newSessionId!);
    
    // Edit session and verify dependent entities reflect changes
    await sessionsTab.click();
    const editSessionButton = sessionGrid.locator('[data-testid="button-edit-session"]').last();
    await editSessionButton.click();
    
    const editSessionModal = page.locator('[data-testid="modal-edit-session"]');
    await page.locator('[data-testid="input-session-name"]').fill('Updated Integrity Session');
    await page.locator('[data-testid="button-save-session"]').click();
    
    // Verify ticket and volunteer position still reference the correct session (will fail if relationships broken)
    await ticketsTab.click();
    await expect(ticketGrid.locator('[data-testid="ticket-sessions"]').last()).toHaveText(newSessionId!);
    
    await volunteersTab.click();
    await expect(volunteerGrid.locator('[data-testid="position-sessions"]').last()).toHaveText(newSessionId!);
  });
});