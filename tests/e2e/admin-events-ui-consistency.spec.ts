import { test, expect } from '@playwright/test';
import { quickLogin } from './helpers/auth.helper';

/**
 * TDD E2E Tests for Admin Events Edit Screen - UI Consistency
 * 
 * These tests are designed to FAIL initially (Red phase of TDD)
 * They test the UI consistency issues described in the business requirements
 * 
 * Expected Failures:
 * - Sessions and Tickets tabs may use modals but Volunteers tab uses bottom form
 * - Table layouts may not follow Edit-first-Delete-last pattern consistently
 * - "Add New" buttons may not be positioned consistently below grids
 * - Modal styling may not be consistent across all tabs
 * - Design System v7 button styles may not be applied consistently
 */

test.describe('Admin Events Edit Screen - UI Consistency', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin user using established pattern from lessons learned
    await quickLogin(page, 'admin');
  });

  test('all tabs should use modal dialogs consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Test Sessions tab uses modals (might work if already implemented)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    if (await sessionsTab.isVisible()) {
      await sessionsTab.click();
      
      const addSessionButton = page.locator('[data-testid="button-add-session"]');
      if (await addSessionButton.isVisible()) {
        await addSessionButton.click();
        
        // Should open modal, not inline form
        const sessionModal = page.locator('[data-testid="modal-add-session"]');
        await expect(sessionModal).toBeVisible();
        
        // Close modal for next test
        const cancelButton = sessionModal.locator('[data-testid="button-cancel-session"]');
        if (await cancelButton.isVisible()) {
          await cancelButton.click();
        } else {
          await page.keyboard.press('Escape');
        }
      }
    }
    
    // Test Tickets tab uses modals (might work if already implemented)
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    if (await ticketsTab.isVisible()) {
      await ticketsTab.click();
      
      const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
      if (await addTicketButton.isVisible()) {
        await addTicketButton.click();
        
        // Should open modal, not inline form
        const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
        await expect(ticketModal).toBeVisible();
        
        // Close modal for next test
        const cancelButton = ticketModal.locator('[data-testid="button-cancel-ticket"]');
        if (await cancelButton.isVisible()) {
          await cancelButton.click();
        } else {
          await page.keyboard.press('Escape');
        }
      }
    }
    
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

  test('should follow Edit-first-Delete-last table pattern', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Test Sessions table follows pattern (if it exists)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    if (await sessionsTab.isVisible()) {
      await sessionsTab.click();
      
      const sessionGrid = page.locator('[data-testid="grid-sessions"]');
      if (await sessionGrid.isVisible()) {
        const headerRow = sessionGrid.locator('thead tr').first();
        
        // First column should be Edit (will fail if not consistent)
        const firstHeader = headerRow.locator('th').first();
        await expect(firstHeader).toContainText(/edit/i);
        
        // Last column should be Delete (will fail if not consistent)
        const lastHeader = headerRow.locator('th').last();
        await expect(lastHeader).toContainText(/delete/i);
        
        // Body rows should follow same pattern
        const firstDataRow = sessionGrid.locator('tbody tr').first();
        if (await firstDataRow.isVisible()) {
          const firstCell = firstDataRow.locator('td').first();
          await expect(firstCell.locator('[data-testid="button-edit-session"]')).toBeVisible();
          
          const lastCell = firstDataRow.locator('td').last();
          await expect(lastCell.locator('[data-testid="button-delete-session"]')).toBeVisible();
        }
      }
    }
    
    // Test Tickets table follows pattern (if it exists)
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    if (await ticketsTab.isVisible()) {
      await ticketsTab.click();
      
      const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
      if (await ticketGrid.isVisible()) {
        const headerRow = ticketGrid.locator('thead tr').first();
        
        // First column should be Edit
        const firstHeader = headerRow.locator('th').first();
        await expect(firstHeader).toContainText(/edit/i);
        
        // Last column should be Delete
        const lastHeader = headerRow.locator('th').last();
        await expect(lastHeader).toContainText(/delete/i);
      }
    }
    
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
    
    // Body rows should follow same pattern
    const firstDataRow = volunteerGrid.locator('tbody tr').first();
    if (await firstDataRow.isVisible()) {
      const firstCell = firstDataRow.locator('td').first();
      await expect(firstCell.locator('[data-testid="button-edit-volunteer-position"]')).toBeVisible();
      
      const lastCell = firstDataRow.locator('td').last();
      await expect(lastCell.locator('[data-testid="button-delete-volunteer-position"]')).toBeVisible();
    }
  });

  test('should have Add New buttons positioned below grids consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Test Sessions tab has Add button below grid (if implemented)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    if (await sessionsTab.isVisible()) {
      await sessionsTab.click();
      
      const sessionGrid = page.locator('[data-testid="grid-sessions"]');
      const addSessionButton = page.locator('[data-testid="button-add-session"]');
      
      if (await sessionGrid.isVisible() && await addSessionButton.isVisible()) {
        const gridBox = await sessionGrid.boundingBox();
        const buttonBox = await addSessionButton.boundingBox();
        
        // Button should be below the grid
        expect(buttonBox!.y).toBeGreaterThan(gridBox!.y + gridBox!.height);
      }
    }
    
    // Test Tickets tab has Add button below grid (if implemented)
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    if (await ticketsTab.isVisible()) {
      await ticketsTab.click();
      
      const ticketGrid = page.locator('[data-testid="grid-ticket-types"]');
      const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
      
      if (await ticketGrid.isVisible() && await addTicketButton.isVisible()) {
        const gridBox = await ticketGrid.boundingBox();
        const buttonBox = await addTicketButton.boundingBox();
        
        // Button should be below the grid
        expect(buttonBox!.y).toBeGreaterThan(gridBox!.y + gridBox!.height);
      }
    }
    
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

  test('should apply consistent modal styling across all tabs', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Collect modal styling from different tabs for comparison
    const modalStyles: Array<{tab: string, styles: any}> = [];
    
    // Test Sessions modal styling (if it exists)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    if (await sessionsTab.isVisible()) {
      await sessionsTab.click();
      
      const addSessionButton = page.locator('[data-testid="button-add-session"]');
      if (await addSessionButton.isVisible()) {
        await addSessionButton.click();
        
        const sessionModal = page.locator('[data-testid="modal-add-session"]');
        if (await sessionModal.isVisible()) {
          const styles = await sessionModal.evaluate(el => {
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
          modalStyles.push({ tab: 'sessions', styles });
          
          // Close modal
          await page.keyboard.press('Escape');
        }
      }
    }
    
    // Test Tickets modal styling (if it exists)
    const ticketsTab = page.locator('[data-testid="tab-tickets"]');
    if (await ticketsTab.isVisible()) {
      await ticketsTab.click();
      
      const addTicketButton = page.locator('[data-testid="button-add-ticket-type"]');
      if (await addTicketButton.isVisible()) {
        await addTicketButton.click();
        
        const ticketModal = page.locator('[data-testid="modal-add-ticket-type"]');
        if (await ticketModal.isVisible()) {
          const styles = await ticketModal.evaluate(el => {
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
          modalStyles.push({ tab: 'tickets', styles });
          
          // Close modal
          await page.keyboard.press('Escape');
        }
      }
    }
    
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
    modalStyles.push({ tab: 'volunteers', styles });
    
    // Verify all modals have consistent styling (will fail if inconsistent)
    if (modalStyles.length > 1) {
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
    }
  });

  test('should use Design System v7 button styles consistently', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Test button styles across all tabs
    const buttonStyles: Array<{location: string, element: string, styles: any}> = [];
    
    // Test Sessions tab buttons (if they exist)
    const sessionsTab = page.locator('[data-testid="tab-sessions"]');
    if (await sessionsTab.isVisible()) {
      await sessionsTab.click();
      
      const addSessionButton = page.locator('[data-testid="button-add-session"]');
      if (await addSessionButton.isVisible()) {
        const styles = await addSessionButton.evaluate(el => {
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
        buttonStyles.push({ location: 'sessions', element: 'add-button', styles });
      }
    }
    
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
    
    // Test Edit/Delete buttons in grid
    const volunteerGrid = page.locator('[data-testid="grid-volunteer-positions"]');
    if (await volunteerGrid.isVisible()) {
      const editButton = volunteerGrid.locator('[data-testid="button-edit-volunteer-position"]').first();
      if (await editButton.isVisible()) {
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
      }
    }
    
    // Verify Design System v7 standards (will fail if not applied)
    for (const buttonInfo of buttonStyles) {
      const { styles } = buttonInfo;
      
      // Design System v7 requirements (these will fail if not implemented)
      expect(styles.fontFamily).toContain('Source Sans 3'); // Or whatever Design System v7 specifies
      expect(styles.borderRadius).toMatch(/^\d+px$/); // Should have consistent border radius
      expect(styles.fontWeight).toBeOneOf(['400', '500', '600', '700']); // Standard font weights
      
      // Should have consistent padding
      expect(styles.padding).toMatch(/^\d+px \d+px$/);
    }
  });

  test('should have consistent tab navigation and layout', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Verify all required tabs exist (will fail if tabs not implemented)
    const requiredTabs = [
      { testId: 'tab-sessions', name: 'Sessions' },
      { testId: 'tab-tickets', name: 'Tickets/Orders' },
      { testId: 'tab-volunteers', name: 'Volunteers/Staff' }
    ];
    
    const tabContainer = page.locator('[data-testid="tabs-event-management"]');
    await expect(tabContainer).toBeVisible();
    
    for (const tab of requiredTabs) {
      const tabElement = page.locator(`[data-testid="${tab.testId}"]`);
      await expect(tabElement).toBeVisible();
      await expect(tabElement).toContainText(tab.name);
    }
    
    // Test tab switching functionality
    for (const tab of requiredTabs) {
      const tabElement = page.locator(`[data-testid="${tab.testId}"]`);
      await tabElement.click();
      
      // Verify tab becomes active (visual indicator)
      await expect(tabElement).toHaveAttribute('aria-selected', 'true');
      
      // Verify corresponding content panel is visible
      const contentPanel = page.locator(`[data-testid="panel-${tab.testId.replace('tab-', '')}"]`);
      await expect(contentPanel).toBeVisible();
    }
  });

  test('should show loading states consistently across all operations', async ({ page }) => {
    // Navigate to admin event edit page
    await page.goto('http://localhost:5173/admin/events/1');
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
    
    // Mock slow API responses to test loading states
    await page.route('**/api/events/1/**', route => {
      // Add delay to see loading states
      setTimeout(() => {
        route.continue();
      }, 2000);
    });
    
    // Test loading states in Volunteers tab (will fail if not implemented)
    const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
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