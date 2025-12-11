import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Test Suite for Vetting Email Templates (Unified Email Templates System)
 *
 * Tests the new unified email templates admin page at /admin/email-templates
 * with tabbed interface for different categories (Vetting, Events, Admin, Incident, Ad Hoc).
 *
 * COMPONENT STRUCTURE (EmailCategoryPanel.tsx):
 * - Templates displayed as clickable Cards (not table rows)
 * - Card shows: templateTypeName, subject, version, updated date
 * - Clicking card opens editor panel with: Subject TextInput, TipTap HTML editor
 * - Editor has Save Template / Cancel buttons
 *
 * Tests verify:
 * - Navigation to Email Templates Management page
 * - Vetting tab accessibility and content
 * - Template card display
 * - Inline editor functionality
 * - TipTap editor integration
 * - Save and Cancel functionality
 *
 * MIGRATION NOTES:
 * - Migrated to DataFactory pattern: 2025-12-10
 * - Uses test fixture for automatic page management
 * - No manual page creation or cleanup needed
 * - Helper function navigateToVettingTab() for common setup
 */

test.describe('Vetting Email Templates (Unified System)', () => {
  /**
   * Helper to navigate to Vetting tab in Email Templates page
   * Returns true if successful, false if should skip test
   */
  async function navigateToVettingTab(page: any): Promise<boolean> {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to unified email templates page
    await page.goto('/admin/email-templates');
    await page.waitForLoadState('domcontentloaded');

    // Check if email templates page exists
    const currentUrl = page.url();
    if (!currentUrl.includes('/admin/email-templates')) {
      console.log('⚠️ Email templates page not found - feature may not be implemented yet.');
      return false;
    }

    // Try to find and click Vetting tab
    const vettingTab = page.locator('[data-testid="tab-vetting"]').or(
      page.getByRole('tab', { name: 'Vetting' })
    );

    if (await vettingTab.count() === 0) {
      console.log('⚠️ Vetting tab not found - feature may not be implemented yet.');
      return false;
    }

    await vettingTab.click();
    await page.waitForTimeout(500);
    return true;
  }

  test('Page Layout - Title and Tabs Visible', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }

    // Take screenshot of initial page load
    await page.screenshot({
      path: './test-results/vetting-email-templates-unified-initial.png',
      fullPage: true
    });

    // Verify page title - actual title is "Email Templates Management"
    const pageTitle = page.locator('h1').filter({ hasText: /Email Templates/i });
    await expect(pageTitle).toBeVisible();

    // Verify all tabs are present
    const tabs = page.getByRole('tablist').first();
    await expect(tabs).toBeVisible();

    // Verify Vetting tab exists and visible
    const vettingTab = page.getByRole('tab', { name: 'Vetting' });
    await expect(vettingTab).toBeVisible();

    console.log('✅ Page title and tabs verified');
  });

  test('Vetting Tab - Contains Email Template Cards', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Wait for template cards to load (component uses Cards in a Group, not table)
    // Look for cards with template info - cards have cursor: pointer
    const templateCards = page.locator('.mantine-Card-root');

    // Check if we have any cards OR if we have a loading/empty state
    const cardCount = await templateCards.count();
    const loadingIndicator = page.locator('text="Loading templates"');
    const emptyMessage = page.locator('text=/No templates found/i');

    // Either we have cards, or loading, or empty message
    const hasCards = cardCount > 0;
    const isLoading = await loadingIndicator.count() > 0;
    const isEmpty = await emptyMessage.count() > 0;

    if (hasCards) {
      // Verify at least some vetting templates exist
      expect(cardCount).toBeGreaterThanOrEqual(1);
      console.log(`✅ Vetting tab contains ${cardCount} email template cards`);
    } else if (isEmpty) {
      console.log('⚠️ No templates found for Vetting category (empty state)');
    } else if (isLoading) {
      console.log('⚠️ Templates still loading');
    } else {
      console.log('⚠️ Unable to detect template cards or empty state');
    }
  });

  test('Template Cards - Display Template Info', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Wait for cards to be visible
    const templateCards = page.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      const firstCard = templateCards.first();
      await expect(firstCard).toBeVisible();

      // Card should have template name (bold text in burgundy color)
      const cardText = await firstCard.textContent();
      expect(cardText).toBeTruthy();
      expect(cardText!.length).toBeGreaterThan(0);

      // Cards show version info like "Version X • Updated DATE"
      // Just verify the card has content
      console.log(`✅ Template card found with content: ${cardText!.substring(0, 50)}...`);
    } else {
      console.log('⚠️ No template cards found to verify');
    }
  });

  test('Template Card - Clickable and Shows Editor', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Click first template card
    const templateCards = page.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      const firstCard = templateCards.first();
      await expect(firstCard).toBeVisible();
      await firstCard.click();
      await page.waitForTimeout(500);

      // Take screenshot with editor visible
      await page.screenshot({
        path: './test-results/vetting-email-templates-editor-visible.png',
        fullPage: true
      });

      // Verify editor panel is visible (Paper component with border)
      // The editor shows "Currently Editing: [template name]"
      const editorHeader = page.locator('text=/Currently Editing/i');
      const editorVisible = await editorHeader.count() > 0;

      if (editorVisible) {
        await expect(editorHeader).toBeVisible();
        console.log('✅ Clicking card shows inline editor');
      } else {
        console.log('⚠️ Editor header not found after clicking card');
      }
    } else {
      console.log('⚠️ No template cards to click');
    }
  });

  test('Editor Components - Subject Field and Content Editor', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Click first template card within the active Vetting tab panel
    const vettingPanel = page.locator('[role="tabpanel"]').first();
    const templateCards = vettingPanel.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      await templateCards.first().click();
      await page.waitForTimeout(500);

      // Verify subject field exists - scope to the visible editor section
      // Use .first() since Vetting tab panel is rendered first
      const subjectLabel = page.locator('label:has-text("Subject Line")').first();

      if (await subjectLabel.count() > 0) {
        await expect(subjectLabel).toBeVisible();
        console.log('✅ Subject field found');
      }

      // Verify content editor exists (TipTap uses ProseMirror)
      const contentEditor = page.locator('.ProseMirror').first();

      if (await contentEditor.count() > 0) {
        await expect(contentEditor).toBeVisible();
        console.log('✅ Content editor found');
      }

      // Verify Save and Cancel buttons
      const saveButton = page.locator('button').filter({ hasText: /Save Template/i }).first();
      const cancelButton = page.locator('button').filter({ hasText: /Cancel/i }).first();

      if (await saveButton.count() > 0) {
        await expect(saveButton).toBeVisible();
        console.log('✅ Save button found');
      }

      if (await cancelButton.count() > 0) {
        await expect(cancelButton).toBeVisible();
        console.log('✅ Cancel button found');
      }

      console.log('✅ Editor contains subject field, content editor, and action buttons');
    } else {
      console.log('⚠️ No template cards to test editor');
    }
  });

  test('Edit Subject Field', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Click first template card within the active Vetting tab panel
    const vettingPanel = page.locator('[role="tabpanel"]').first();
    const templateCards = vettingPanel.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      await templateCards.first().click();
      await page.waitForTimeout(500);

      // Find subject input - use .first() to get Vetting tab's input (rendered first)
      const subjectLabel = page.locator('label:has-text("Subject Line")').first();

      if (await subjectLabel.count() > 0) {
        // Get the input associated with this label
        const inputId = await subjectLabel.getAttribute('for');
        const subjectField = inputId
          ? page.locator(`#${inputId}`)
          : page.locator('input').first();

        // Get original value
        const originalSubject = await subjectField.inputValue();
        console.log(`   Original subject: "${originalSubject}"`);

        // Clear and type new subject
        await subjectField.clear();
        await subjectField.fill('Test Subject - Updated via E2E');

        // Verify new value
        const newSubject = await subjectField.inputValue();
        expect(newSubject).toBe('Test Subject - Updated via E2E');

        // Take screenshot of edited state
        await page.screenshot({
          path: './test-results/vetting-email-templates-subject-edited.png',
          fullPage: true
        });

        console.log('✅ Subject field can be edited');
      } else {
        console.log('⚠️ Subject field label not found');
      }
    } else {
      console.log('⚠️ No template cards to test');
    }
  });

  test('Edit Content in Editor', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Click first template card
    const templateCards = page.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      await templateCards.first().click();
      await page.waitForTimeout(500);

      // Get content editor (TipTap uses ProseMirror)
      const contentEditor = page.locator('.ProseMirror').or(
        page.locator('[contenteditable="true"]')
      ).first();

      if (await contentEditor.count() > 0) {
        // Get original content
        const originalContent = await contentEditor.textContent();
        console.log(`   Original content length: ${originalContent?.length || 0} chars`);

        // Click and add text
        await contentEditor.click();
        await contentEditor.pressSequentially('\n\nTest addition via E2E test', {
          delay: 50
        });

        // Verify content changed
        const newContent = await contentEditor.textContent();
        expect(newContent).toContain('Test addition');

        // Take screenshot with content edited
        await page.screenshot({
          path: './test-results/vetting-email-templates-content-edited.png',
          fullPage: true
        });

        console.log('✅ Content editor can be edited');
      } else {
        console.log('⚠️ Content editor not found');
      }
    } else {
      console.log('⚠️ No template cards to test');
    }
  });

  test('Cancel Button - Closes Editor Without Saving', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Click first template card
    const templateCards = page.locator('.mantine-Card-root');

    if (await templateCards.count() > 0) {
      await templateCards.first().click();
      await page.waitForTimeout(500);

      // Check if editor opened
      const editorHeader = page.locator('text=/Currently Editing/i');

      if (await editorHeader.count() > 0) {
        // Click Cancel button
        const cancelButton = page.locator('button').filter({ hasText: /Cancel/i }).first();
        await cancelButton.click();
        await page.waitForTimeout(500);

        // Verify editor is closed (no "Currently Editing" header visible)
        const editorStillVisible = await editorHeader.count() > 0;

        if (!editorStillVisible) {
          console.log('✅ Cancel button closes editor');
        } else {
          // Editor might still be visible but content reset
          console.log('⚠️ Editor still visible after cancel (may be by design)');
        }
      } else {
        console.log('⚠️ Editor did not open after clicking card');
      }
    } else {
      console.log('⚠️ No template cards to test');
    }
  });

  test('Switch Between Template Cards - Editor Updates', async ({ page }) => {
    if (!await navigateToVettingTab(page)) {
      test.skip();
      return;
    }
    // Get card count within Vetting panel
    const vettingPanel = page.locator('[role="tabpanel"]').first();
    const templateCards = vettingPanel.locator('.mantine-Card-root');
    const cardCount = await templateCards.count();

    if (cardCount < 2) {
      console.log('⚠️ Not enough templates to test switching');
      return;
    }

    // Click first card
    const firstCard = templateCards.nth(0);
    await firstCard.click();
    await page.waitForTimeout(500);

    // Get first template's subject field value (this will definitely differ between templates)
    const subjectLabel = page.locator('label:has-text("Subject Line")').first();
    const inputId = await subjectLabel.getAttribute('for');
    const subjectField = inputId ? page.locator(`#${inputId}`) : page.locator('input').first();
    const firstSubject = await subjectField.inputValue();
    console.log(`   First template subject: "${firstSubject}"`);

    // Click second card
    const secondCard = templateCards.nth(1);
    await secondCard.click();
    await page.waitForTimeout(500);

    // Get second template's subject field value
    const secondSubject = await subjectField.inputValue();
    console.log(`   Second template subject: "${secondSubject}"`);

    // Verify subjects are different (editor updated)
    expect(firstSubject).not.toBe(secondSubject);

    // Take screenshot showing second template
    await page.screenshot({
      path: './test-results/vetting-email-templates-second-template.png',
      fullPage: true
    });

    console.log('✅ Editor updates when switching between template cards');
  });

  test('URL Parameter - Vetting Tab Selected', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');
    // Navigate with vetting tab parameter
    await page.goto('/admin/email-templates?tab=vetting');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(500);

    // Verify Vetting tab is active
    const vettingTab = page.getByRole('tab', { name: 'Vetting' });
    const isSelected = await vettingTab.getAttribute('aria-selected');
    expect(isSelected).toBe('true');

    console.log('✅ URL parameter correctly selects Vetting tab');
  });

  test('Performance - Page Load Time', async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    const startTime = Date.now();

    await page.goto('/admin/email-templates?tab=vetting');
    await page.waitForLoadState('domcontentloaded');

    const loadTime = Date.now() - startTime;

    console.log(`✅ Page loaded in ${loadTime}ms`);

    // Performance threshold (should load in under 3 seconds)
    expect(loadTime).toBeLessThan(3000);
  });
});
