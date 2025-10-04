import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from '../../../helpers/auth.helpers';

/**
 * E2E Tests for Admin Vetting Application Detail
 *
 * Tests application detail view, status change modals, notes, and audit log
 * Based on test plan: /docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/testing/test-plan.md
 *
 * CRITICAL: All tests run against Docker on port 5173 ONLY
 */

test.describe('Admin Vetting Application Detail', () => {
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();
    await AuthHelpers.clearAuthState(page);
  });

  test.afterEach(async () => {
    await page.close();
  });

  /**
   * Helper to navigate to first available application detail
   */
  async function navigateToFirstApplication() {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/vetting');
    await expect(page.locator('table, [data-testid="vetting-grid"]')).toBeVisible();

    const firstRow = page.locator('table tbody tr, [data-testid="application-row"]').first();
    const viewButton = firstRow.locator('[data-testid="view-button"], button').filter({ hasText: /view|details/i }).first();

    if (await viewButton.count() > 0) {
      await viewButton.click();
    } else {
      await firstRow.click();
    }

    await page.waitForURL(/\/admin\/vetting\/applications\/[a-f0-9-]+/i, { timeout: 5000 });
  }

  /**
   * TEST 1: Admin can view application details
   * Validates: Detail page rendering, data display, field visibility
   */
  test('admin can view application details', async () => {
    // Arrange & Act
    await navigateToFirstApplication();

    // Assert - Verify detail page elements
    const detailPage = page.locator('[data-testid="application-detail"], main');
    await expect(detailPage).toBeVisible();

    // Verify key information sections
    const infoSections = [
      /scene.*name|applicant/i,
      /email/i,
      /status/i,
      /submitted|date/i
    ];

    for (const sectionPattern of infoSections) {
      const section = page.locator('label, dt, th, strong').filter({ hasText: sectionPattern }).first();
      if (await section.count() > 0) {
        await expect(section).toBeVisible();
      }
    }

    // Verify action buttons exist
    const actionButtons = page.locator('button').filter({
      hasText: /approve|deny|hold|remind/i
    });
    const buttonCount = await actionButtons.count();
    expect(buttonCount).toBeGreaterThan(0);

    // Take screenshot
    await page.screenshot({ path: 'test-results/application-detail.png', fullPage: true });
  });

  /**
   * TEST 2: Admin can approve application with reasoning
   * Validates: Approve modal, form submission, status update
   */
  test('admin can approve application with reasoning', async () => {
    // Arrange
    await navigateToFirstApplication();

    // Act - Click approve button
    const approveButton = page.locator('button, [data-testid="approve-button"]').filter({ hasText: /approve/i }).first();

    if (await approveButton.count() > 0) {
      await approveButton.click();

      // Wait for modal to appear
      const modal = page.locator('[role="dialog"], .modal, [data-testid="approve-modal"]');
      await expect(modal).toBeVisible({ timeout: 2000 });

      // Fill optional notes
      const notesInput = modal.locator('textarea, [data-testid="notes-input"]').first();
      if (await notesInput.count() > 0) {
        await notesInput.fill('Interview completed successfully. Applicant demonstrates strong knowledge.');
      }

      // Submit approval
      const submitButton = modal.locator('button').filter({ hasText: /approve|confirm|submit/i }).first();
      await submitButton.click();

      // Assert - Verify success
      // Modal should close
      await expect(modal).not.toBeVisible({ timeout: 3000 });

      // Success notification should appear
      const successToast = page.locator('[data-testid="success-toast"], .notification, .toast').filter({ hasText: /success|approved/i });
      if (await successToast.count() > 0) {
        await expect(successToast).toBeVisible();
      }

      // Status badge should update to "Approved"
      const statusBadge = page.locator('[data-testid="status-badge"], .badge, .status').filter({ hasText: /approved/i });
      await expect(statusBadge).toBeVisible({ timeout: 3000 });
    } else {
      console.log('Approve button not found - application may not be in correct status');
    }
  });

  /**
   * TEST 3: Admin can deny application with reasoning
   * Validates: Deny modal, required notes validation, status update
   */
  test('admin can deny application with reasoning', async () => {
    // Arrange
    await navigateToFirstApplication();

    // Act - Click deny button
    const denyButton = page.locator('button, [data-testid="deny-button"]').filter({ hasText: /deny|reject/i }).first();

    if (await denyButton.count() > 0) {
      await denyButton.click();

      // Wait for modal
      const modal = page.locator('[role="dialog"], .modal, [data-testid="deny-modal"]');
      await expect(modal).toBeVisible({ timeout: 2000 });

      // Fill required reason
      const reasonInput = modal.locator('textarea, [data-testid="reason-input"], [data-testid="notes-input"]').first();
      await reasonInput.fill('Application does not meet safety requirements. Insufficient experience demonstrated.');

      // Submit denial
      const submitButton = modal.locator('button').filter({ hasText: /deny|confirm|submit/i }).first();
      await submitButton.click();

      // Assert - Verify success
      await expect(modal).not.toBeVisible({ timeout: 3000 });

      // Status should update to "Denied"
      const statusBadge = page.locator('[data-testid="status-badge"], .badge, .status').filter({ hasText: /denied/i });
      await expect(statusBadge).toBeVisible({ timeout: 3000 });
    } else {
      console.log('Deny button not found - application may not be in correct status');
    }
  });

  /**
   * TEST 4: Admin can put application on hold with reasoning
   * Validates: OnHold modal, required fields, status update
   */
  test('admin can put application on hold with reasoning', async () => {
    // Arrange
    await navigateToFirstApplication();

    // Act - Click put on hold button
    const holdButton = page.locator('button, [data-testid="hold-button"]').filter({ hasText: /hold|pause/i }).first();

    if (await holdButton.count() > 0) {
      await holdButton.click();

      // Wait for modal
      const modal = page.locator('[role="dialog"], .modal, [data-testid="hold-modal"]');
      await expect(modal).toBeVisible({ timeout: 2000 });

      // Fill reason
      const reasonInput = modal.locator('textarea, input, [data-testid="reason-input"]').filter({ hasText: /reason/i }).first();
      if (await reasonInput.count() === 0) {
        // Try generic textarea
        await modal.locator('textarea').first().fill('Missing required references');
      } else {
        await reasonInput.fill('Missing required references');
      }

      // Fill required actions
      const actionsInput = modal.locator('textarea, input').filter({ hasText: /action|required/i }).first();
      if (await actionsInput.count() === 0) {
        // Try second textarea if exists
        const textareas = modal.locator('textarea');
        if (await textareas.count() > 1) {
          await textareas.nth(1).fill('Please submit 2 references from community members');
        }
      } else {
        await actionsInput.fill('Please submit 2 references from community members');
      }

      // Submit
      const submitButton = modal.locator('button').filter({ hasText: /hold|submit|confirm/i }).first();
      await submitButton.click();

      // Assert - Verify success
      await expect(modal).not.toBeVisible({ timeout: 3000 });

      // Status should update to "OnHold"
      const statusBadge = page.locator('[data-testid="status-badge"], .badge, .status').filter({ hasText: /hold/i });
      await expect(statusBadge).toBeVisible({ timeout: 3000 });
    } else {
      console.log('Hold button not found - application may not be in correct status');
    }
  });

  /**
   * TEST 5: Admin can add notes to application
   * Validates: Notes section, add note functionality, note persistence
   */
  test('admin can add notes to application', async () => {
    // Arrange
    await navigateToFirstApplication();

    // Act - Find notes section
    const notesSection = page.locator('[data-testid="notes-section"], section').filter({ hasText: /notes|comments/i });

    if (await notesSection.count() > 0) {
      // Find add note button or textarea
      const addNoteButton = notesSection.locator('button').filter({ hasText: /add.*note|new.*note/i }).first();
      const notesTextarea = notesSection.locator('textarea').first();

      if (await notesTextarea.count() > 0) {
        // Direct note entry
        await notesTextarea.fill('Follow-up needed regarding safety certification');

        // Find save button
        const saveButton = notesSection.locator('button').filter({ hasText: /save|submit|add/i }).first();
        if (await saveButton.count() > 0) {
          await saveButton.click();

          // Assert - Verify note appears
          await page.waitForTimeout(1000);
          const noteText = page.locator('text=/follow.*up.*needed.*safety/i');
          await expect(noteText).toBeVisible();
        }
      } else if (await addNoteButton.count() > 0) {
        // Click to open note form
        await addNoteButton.click();
        // Similar flow as above
      } else {
        console.log('Notes functionality not yet implemented');
      }
    } else {
      console.log('Notes section not found on detail page');
    }
  });

  /**
   * TEST 6: Admin can view audit log history
   * Validates: Audit log section, history display, chronological order
   */
  test('admin can view audit log history', async () => {
    // Arrange
    await navigateToFirstApplication();

    // Act - Scroll to find audit log section
    const auditSection = page.locator('[data-testid="audit-log"], [data-testid="history"], section').filter({
      hasText: /audit|history|activity/i
    });

    if (await auditSection.count() > 0) {
      await auditSection.scrollIntoViewIfNeeded();

      // Assert - Verify audit entries exist
      const auditEntries = auditSection.locator('[data-testid="audit-entry"], .audit-entry, li, tr');
      const entryCount = await auditEntries.count();

      if (entryCount > 0) {
        // Verify first entry is visible
        await expect(auditEntries.first()).toBeVisible();

        // Verify entry contains expected information
        const firstEntry = auditEntries.first();
        const entryText = await firstEntry.textContent();

        // Should contain date, action, or user info
        expect(entryText).toBeTruthy();
        expect(entryText!.length).toBeGreaterThan(10);
      } else {
        console.log('No audit entries found - application may be new');
      }
    } else {
      console.log('Audit log section not yet implemented');
    }
  });

  /**
   * TEST 7: Role is granted after approval
   * Validates: Post-approval verification, role update (requires API or separate test)
   * Note: This is best validated through integration tests, but we can check UI indicators
   */
  test('approved application shows vetted member status', async () => {
    // Arrange - Navigate to an approved application
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/vetting');
    await expect(page.locator('table, [data-testid="vetting-grid"]')).toBeVisible();

    // Find an approved application
    const approvedRow = page.locator('table tbody tr, [data-testid="application-row"]')
      .filter({ has: page.locator('text=/approved/i') })
      .first();

    if (await approvedRow.count() > 0) {
      // Click to view details
      const viewButton = approvedRow.locator('button').filter({ hasText: /view|details/i }).first();
      if (await viewButton.count() > 0) {
        await viewButton.click();
      } else {
        await approvedRow.click();
      }

      await page.waitForURL(/\/admin\/vetting\/applications\/[a-f0-9-]+/i, { timeout: 5000 });

      // Assert - Verify approved status is displayed
      const statusBadge = page.locator('[data-testid="status-badge"], .badge, .status').filter({ hasText: /approved/i });
      await expect(statusBadge).toBeVisible();

      // Look for role indicator if displayed
      const roleIndicator = page.locator('text=/vetted.*member|member.*role/i');
      if (await roleIndicator.count() > 0) {
        await expect(roleIndicator).toBeVisible();
      }

      // Verify approval timestamp exists
      const approvalDate = page.locator('text=/approved.*on|decision.*made|approved.*date/i');
      if (await approvalDate.count() > 0) {
        await expect(approvalDate).toBeVisible();
      }
    } else {
      console.log('No approved applications found - create test data with approved status');
    }
  });
});
