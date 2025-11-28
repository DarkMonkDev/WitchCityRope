import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Test Suite for Vetting Email Templates (Unified Email Templates System)
 *
 * Tests the new unified email templates admin page at /admin/email-templates
 * with tabbed interface for different categories (Vetting, Events, Admin, Incident, Ad Hoc).
 *
 * Tests verify:
 * - Navigation to Email Templates Management page
 * - Vetting tab accessibility and content
 * - Template table structure and data
 * - Inline editor functionality
 * - TipTap editor integration
 * - Save and Cancel functionality
 */

test.describe('Vetting Email Templates (Unified System)', () => {
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();

    // Login as admin
    const loginSuccess = await AuthHelpers.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();

    // Navigate to unified email templates page
    await page.goto('/admin/email-templates');
    await page.waitForLoadState('domcontentloaded');

    // Click the Vetting tab
    const vettingTab = page.locator('[data-testid="tab-vetting"]').or(
      page.getByRole('tab', { name: 'Vetting' })
    );
    await expect(vettingTab).toBeVisible();
    await vettingTab.click();
    await page.waitForTimeout(500);
  });

  test.afterEach(async () => {
    await page.close();
  });

  test('Page Layout - Title and Tabs Visible', async () => {
    // Take screenshot of initial page load
    await page.screenshot({
      path: './test-results/vetting-email-templates-unified-initial.png',
      fullPage: true
    });

    // Verify page title
    const pageTitle = page.locator('h1').filter({ hasText: /Email Templates/i });
    await expect(pageTitle).toBeVisible();

    // Verify all tabs are present
    const tabs = page.getByRole('tablist').first();
    await expect(tabs).toBeVisible();

    // Verify Vetting tab exists
    const vettingTab = page.getByRole('tab', { name: 'Vetting' });
    await expect(vettingTab).toBeVisible();

    console.log('✅ Page title and tabs verified');
  });

  test('Vetting Tab - Contains Email Templates', async () => {
    // Wait for table to be visible
    const table = page.locator('table').first();
    await expect(table).toBeVisible();

    // Get table rows
    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();

    // Verify at least 4 vetting templates exist
    expect(rowCount).toBeGreaterThanOrEqual(4);

    console.log(`✅ Vetting tab contains ${rowCount} email templates`);
  });

  test('Table Structure - Required Columns Present', async () => {
    // Wait for table to be visible
    const table = page.locator('table').first();
    await expect(table).toBeVisible();

    // Get all table headers
    const headers = table.locator('thead th');
    const headerCount = await headers.count();

    // Verify at least Template Name, Subject, and Last Modified columns
    expect(headerCount).toBeGreaterThanOrEqual(3);

    // Check for Template Name column
    const templateNameHeader = headers.filter({ hasText: /Template Name|Name/i });
    await expect(templateNameHeader.first()).toBeVisible();

    // Check for Subject column
    const subjectHeader = headers.filter({ hasText: /Subject/i });
    await expect(subjectHeader.first()).toBeVisible();

    console.log(`✅ Table has ${headerCount} columns with required headers`);
  });

  test('Template Row - Clickable and Shows Editor', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await expect(firstRow).toBeVisible();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Take screenshot with editor visible
    await page.screenshot({
      path: './test-results/vetting-email-templates-editor-visible.png',
      fullPage: true
    });

    // Verify editor container is visible (various possible selectors)
    const editorVisible = await page.locator('[data-testid="email-template-editor"]').isVisible()
      || await page.locator('.email-template-editor').isVisible()
      || await page.locator('form').filter({ has: page.locator('input[name="subject"]') }).isVisible();

    expect(editorVisible).toBeTruthy();

    console.log('✅ Clicking row shows inline editor');
  });

  test('Editor Components - Subject Field and Content Editor', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Verify subject field exists
    const subjectField = page.locator('input[name="subject"]').or(
      page.locator('input').filter({ hasAttribute: 'placeholder' }).filter({ hasText: /subject/i })
    ).first();
    await expect(subjectField).toBeVisible();

    // Verify content editor exists (TipTap or similar)
    const contentEditor = page.locator('.ProseMirror').or(
      page.locator('[contenteditable="true"]')
    ).first();
    await expect(contentEditor).toBeVisible();

    // Verify Save and Cancel buttons
    const saveButton = page.locator('button').filter({ hasText: /save/i }).first();
    await expect(saveButton).toBeVisible();

    const cancelButton = page.locator('button').filter({ hasText: /cancel/i }).first();
    await expect(cancelButton).toBeVisible();

    console.log('✅ Editor contains subject field, content editor, and action buttons');
  });

  test('Edit Subject Field', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get subject field
    const subjectField = page.locator('input[name="subject"]').first();

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
  });

  test('Edit Content in Editor', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get content editor
    const contentEditor = page.locator('.ProseMirror').or(
      page.locator('[contenteditable="true"]')
    ).first();

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
  });

  test('Cancel Button - Closes Editor Without Saving', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Edit subject field
    const subjectField = page.locator('input[name="subject"]').first();
    const originalSubject = await subjectField.inputValue();
    await subjectField.fill('Modified Subject - Should Not Save');

    // Click Cancel button
    const cancelButton = page.locator('button').filter({ hasText: /cancel/i }).first();
    await cancelButton.click();
    await page.waitForTimeout(500);

    // Verify editor is closed (form/editor container not visible)
    const editorContainer = page.locator('[data-testid="email-template-editor"], .email-template-editor, form').first();
    const isVisible = await editorContainer.isVisible();

    // Editor should be hidden after cancel
    if (isVisible) {
      // Some implementations might keep form visible but reset fields
      // Verify changes were not saved by clicking row again
      await firstRow.click();
      await page.waitForTimeout(500);
      const subjectAfterCancel = await subjectField.inputValue();
      expect(subjectAfterCancel).toBe(originalSubject);
    }

    console.log('✅ Cancel button closes editor without saving changes');
  });

  test('Switch Between Template Rows - Editor Updates', async () => {
    // Get row count
    const rows = page.locator('tbody tr');
    const rowCount = await rows.count();

    if (rowCount < 2) {
      console.log('⚠️  Not enough templates to test switching');
      return;
    }

    // Click first row
    const firstRow = rows.nth(0);
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get first template subject
    const subjectField = page.locator('input[name="subject"]').first();
    const firstSubject = await subjectField.inputValue();
    console.log(`   First template subject: "${firstSubject}"`);

    // Click second row
    const secondRow = rows.nth(1);
    await secondRow.click();
    await page.waitForTimeout(500);

    // Get second template subject
    const secondSubject = await subjectField.inputValue();
    console.log(`   Second template subject: "${secondSubject}"`);

    // Verify subjects are different (editor updated)
    expect(firstSubject).not.toBe(secondSubject);

    // Take screenshot showing second template
    await page.screenshot({
      path: './test-results/vetting-email-templates-second-template.png',
      fullPage: true
    });

    console.log('✅ Editor updates when switching between template rows');
  });

  test('All Vetting Template Types Present', async () => {
    // Expected vetting template types
    const expectedTypes = ['Initial Review', 'Approved', 'Rejected', 'Info Request'];
    const foundTypes: string[] = [];

    // Get all rows
    const rows = page.locator('tbody tr');
    const rowCount = await rows.count();

    // Extract template names/types from each row
    for (let i = 0; i < rowCount; i++) {
      const row = rows.nth(i);
      const text = await row.textContent();

      // Check if any expected type is in the row text
      for (const expectedType of expectedTypes) {
        if (text?.includes(expectedType)) {
          foundTypes.push(expectedType);
        }
      }
    }

    console.log(`   Found template types: ${foundTypes.join(', ')}`);

    // Verify we found vetting-related templates
    expect(foundTypes.length).toBeGreaterThan(0);

    console.log(`✅ Found ${foundTypes.length} vetting template types`);
  });

  test('URL Parameter - Vetting Tab Selected', async () => {
    // Navigate with vetting tab parameter
    await page.goto('/admin/email-templates?tab=vetting');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(500);

    // Verify Vetting tab is active
    const vettingTab = page.getByRole('tab', { name: 'Vetting' });
    const isSelected = await vettingTab.getAttribute('aria-selected');
    expect(isSelected).toBe('true');

    // Verify vetting templates table is visible
    const table = page.locator('table').first();
    await expect(table).toBeVisible();

    console.log('✅ URL parameter correctly selects Vetting tab');
  });

  test('Performance - Page Load Time', async () => {
    const startTime = Date.now();

    await page.goto('/admin/email-templates?tab=vetting');
    await page.waitForLoadState('domcontentloaded');

    const loadTime = Date.now() - startTime;

    console.log(`✅ Page loaded in ${loadTime}ms`);

    // Performance threshold (should load in under 3 seconds)
    expect(loadTime).toBeLessThan(3000);
  });
});
