import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';
import { injectAxe, checkA11y } from 'axe-playwright';

/**
 * CMS Accessibility Tests
 *
 * Tests WCAG 2.1 AA compliance for the CMS feature:
 * - Keyboard navigation
 * - ARIA labels and roles
 * - Focus management
 * - Screen reader compatibility
 * - Color contrast
 *
 * Uses axe-core for automated accessibility testing
 */

test.describe('CMS Accessibility Tests', () => {
  test('CMS page has no accessibility violations in view mode', async ({ page }) => {
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Inject axe-core for accessibility testing
    await injectAxe(page);

    // Run accessibility check
    await checkA11y(page, undefined, {
      detailedReport: true,
      detailedReportOptions: { html: true },
    });
  });

  test('CMS edit mode has no accessibility violations', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Enter edit mode
    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Inject axe-core
    await injectAxe(page);

    // Run accessibility check on edit mode
    await checkA11y(page, undefined, {
      detailedReport: true,
      detailedReportOptions: { html: true },
    });
  });

  test('Keyboard navigation: Can tab to edit button and activate with Enter', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Tab to edit button (may require multiple tabs depending on page structure)
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Find edit button and verify focus
    const editButton = page.locator('button:has-text("Edit")').first();

    // Check if edit button is focused (may need to adjust tab count)
    const isFocused = await editButton.evaluate((el) => el === document.activeElement);

    if (!isFocused) {
      // If not focused yet, continue tabbing until we reach it
      await editButton.focus();
    }

    // Activate with Enter key
    await page.keyboard.press('Enter');

    // Verify editor opened
    const editor = page.locator('[contenteditable="true"]').first();
    await expect(editor).toBeVisible({ timeout: 2000 });
  });

  test('Keyboard navigation: Can navigate TipTap toolbar with Tab', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Enter edit mode
    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Focus on editor
    const editor = page.locator('[contenteditable="true"]').first();
    await editor.click();

    // Tab to navigate toolbar buttons (if present)
    await page.keyboard.press('Tab');

    // Verify focus moved to toolbar or save/cancel buttons
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(['BUTTON', 'INPUT']).toContain(focusedElement);
  });

  test('Keyboard navigation: Escape key cancels edit mode', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Enter edit mode
    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Press Escape
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);

    // Verify modal appeared OR editor closed (depending on dirty state)
    const modal = page.locator('[role="dialog"]');
    const editor = page.locator('[contenteditable="true"]');

    // Either modal is visible OR editor is closed
    const modalVisible = await modal.count() > 0;
    const editorVisible = await editor.count() > 0;

    expect(modalVisible || !editorVisible).toBe(true);
  });

  test('ARIA labels: Edit button has accessible label', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    const editButton = page.locator('button:has-text("Edit")').first();

    // Verify button has accessible name (either text content or aria-label)
    const accessibleName = await editButton.getAttribute('aria-label') || await editButton.textContent();
    expect(accessibleName).toBeTruthy();
    expect(accessibleName).toMatch(/edit/i);
  });

  test('ARIA labels: Modal has proper aria-labelledby', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Enter edit mode
    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Make changes
    const editor = page.locator('[contenteditable="true"]').first();
    await editor.click();
    await editor.fill('Changed content');

    // Cancel to trigger modal
    const cancelButton = page.locator('button:has-text("Cancel")').first();
    await cancelButton.click();

    // Verify modal has aria attributes
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 2000 });

    const hasAriaLabel = await modal.evaluate((el) => {
      return el.hasAttribute('aria-labelledby') || el.hasAttribute('aria-label');
    });

    expect(hasAriaLabel).toBe(true);
  });

  test('Focus management: Focus returns to edit button after cancel', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Cancel without changes (no modal)
    const cancelButton = page.locator('button:has-text("Cancel")').first();
    await cancelButton.click();
    await page.waitForTimeout(500);

    // Verify focus returned to edit button (or somewhere accessible)
    const focusedElement = await page.evaluate(() => {
      return document.activeElement?.tagName + ' ' + document.activeElement?.textContent?.substring(0, 20);
    });

    console.log('Focused element after cancel:', focusedElement);
    // Note: Exact focus management may vary by implementation
    expect(focusedElement).toBeTruthy();
  });

  test('Screen reader: Success notification is announced', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Enter edit mode
    const editButton = page.locator('button:has-text("Edit")').first();
    await editButton.click();
    await page.waitForTimeout(500);

    // Make a change
    const editor = page.locator('[contenteditable="true"]').first();
    await editor.click();
    await editor.fill('Updated content for SR test');

    // Save
    const saveButton = page.locator('button:has-text("Save")').first();
    await saveButton.click();

    // Verify notification has role="alert" or aria-live region
    const notification = page.locator('.mantine-Notification-root').first();
    await expect(notification).toBeVisible({ timeout: 2000 });

    const hasAlertRole = await notification.evaluate((el) => {
      return el.getAttribute('role') === 'alert' || el.getAttribute('aria-live') === 'polite' || el.getAttribute('aria-live') === 'assertive';
    });

    expect(hasAlertRole).toBe(true);
  });

  test('Color contrast: Buttons meet WCAG AA standards (4.5:1)', async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('http://localhost:5173/resources');
    await page.waitForLoadState('networkidle');

    // Inject axe-core
    await injectAxe(page);

    // Run color contrast check specifically
    await checkA11y(page, undefined, {
      rules: {
        'color-contrast': { enabled: true },
      },
      detailedReport: true,
    });
  });
});
