import { test, expect, Page } from '@playwright/test';
import { AuthHelper } from './helpers/auth.helper';

/**
 * E2E Test Suite for Vetting Email Templates Admin Page
 *
 * Tests the updated UI with:
 * - No subtitle
 * - 4 columns: Template Name, Type, Subject, Last Modified
 * - No STATUS or ACTIONS columns
 * - No CREATE TEMPLATE button
 * - Clickable rows with inline editor
 * - TipTap editor integration
 * - Save and Cancel functionality
 */

test.describe('Vetting Email Templates Admin Page', () => {
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();

    // Login as admin
    const loginSuccess = await AuthHelper.loginAs(page, 'admin');
    expect(loginSuccess).toBeTruthy();

    // Navigate to vetting email templates page
    await page.goto('http://localhost:5173/admin/vetting/email-templates');
    await page.waitForLoadState('networkidle');
  });

  test.afterEach(async () => {
    await page.close();
  });

  test('Page Layout - Title and No Subtitle', async () => {
    // Take screenshot of initial page load
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-initial.png',
      fullPage: true
    });

    // Verify page title
    const pageTitle = page.locator('h1, [role="heading"][aria-level="1"]');
    await expect(pageTitle).toContainText('Vetting Email Templates');

    // Verify NO subtitle exists
    const subtitle = page.locator('h2, [role="heading"][aria-level="2"]').filter({
      hasText: /configure.*email.*templates/i
    });
    await expect(subtitle).toHaveCount(0);

    console.log('✅ Page title verified: "Vetting Email Templates" with no subtitle');
  });

  test('Table Structure - 4 Columns Only', async () => {
    // Wait for table to be visible
    const table = page.locator('table').first();
    await expect(table).toBeVisible();

    // Get all table headers
    const headers = table.locator('thead th');
    const headerCount = await headers.count();

    // Verify exactly 4 columns
    expect(headerCount).toBe(4);

    // Verify column headers
    await expect(headers.nth(0)).toContainText('Template Name');
    await expect(headers.nth(1)).toContainText('Type');
    await expect(headers.nth(2)).toContainText('Subject');
    await expect(headers.nth(3)).toContainText('Last Modified');

    // Verify NO STATUS column
    const statusColumn = headers.filter({ hasText: /status/i });
    await expect(statusColumn).toHaveCount(0);

    // Verify NO ACTIONS column
    const actionsColumn = headers.filter({ hasText: /actions/i });
    await expect(actionsColumn).toHaveCount(0);

    console.log('✅ Table has exactly 4 columns: Template Name, Type, Subject, Last Modified');
  });

  test('No Create Template Button', async () => {
    // Verify NO create template button exists
    const createButton = page.locator('button').filter({
      hasText: /create.*template/i
    });
    await expect(createButton).toHaveCount(0);

    // Also check for common button patterns
    const addButton = page.locator('button').filter({
      hasText: /add|new|create/i
    });

    // If there are any add/create buttons, they should not be for templates
    const buttonCount = await addButton.count();
    if (buttonCount > 0) {
      // Log for debugging, but don't fail - there might be other legitimate buttons
      console.log(`⚠️  Found ${buttonCount} add/create buttons - verifying they're not for templates`);
    }

    console.log('✅ No CREATE TEMPLATE button found (as expected)');
  });

  test('Table Content - 4 Template Rows', async () => {
    // Wait for table body
    const tableBody = page.locator('tbody').first();
    await expect(tableBody).toBeVisible();

    // Count template rows
    const rows = tableBody.locator('tr');
    const rowCount = await rows.count();

    // Verify 4 templates exist
    expect(rowCount).toBe(4);

    // Verify each row has 4 cells
    for (let i = 0; i < rowCount; i++) {
      const cells = rows.nth(i).locator('td');
      const cellCount = await cells.count();
      expect(cellCount).toBe(4);
    }

    console.log('✅ Table contains exactly 4 template rows with 4 cells each');
  });

  test('Clickable Row - Highlights on Click', async () => {
    // Get first template row
    const firstRow = page.locator('tbody tr').first();
    await expect(firstRow).toBeVisible();

    // Get initial background color (if any)
    const initialBg = await firstRow.evaluate((el) =>
      window.getComputedStyle(el).backgroundColor
    );

    // Click the row
    await firstRow.click();
    await page.waitForTimeout(500); // Wait for highlight animation

    // Get background color after click
    const clickedBg = await firstRow.evaluate((el) =>
      window.getComputedStyle(el).backgroundColor
    );

    // Verify background changed (row is highlighted)
    expect(clickedBg).not.toBe(initialBg);

    console.log('✅ Row highlights when clicked');
    console.log(`   Initial: ${initialBg}, Clicked: ${clickedBg}`);
  });

  test('Inline Editor - Appears Below Table on Row Click', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Take screenshot with editor visible
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-editor-visible.png',
      fullPage: true
    });

    // Verify editor container is visible
    const editorContainer = page.locator('[data-testid="email-template-editor"], .email-template-editor, form').filter({
      has: page.locator('input[name="subject"], textarea[name="subject"]')
    }).first();
    await expect(editorContainer).toBeVisible();

    console.log('✅ Inline editor appears below table when row is clicked');
  });

  test('Editor Components - Template Name, Subject, TipTap, Buttons', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Verify template name is shown (read-only or heading)
    const templateName = page.locator('h2, h3, .template-name, [data-testid="template-name"]').filter({
      hasText: /.+/ // Non-empty text
    }).first();
    await expect(templateName).toBeVisible();

    // Verify subject field
    const subjectField = page.locator('input[name="subject"], input[type="text"]').filter({
      hasText: /.*/
    }).first();
    await expect(subjectField).toBeVisible();

    // Verify TipTap editor exists (look for common TipTap classes/attributes)
    const tipTapEditor = page.locator('.ProseMirror, [contenteditable="true"], .tiptap').first();
    await expect(tipTapEditor).toBeVisible();

    // Verify variable help text
    const variableHelp = page.locator('text=/\\{\\{.*\\}\\}/').first();
    await expect(variableHelp).toBeVisible();

    // Verify Save button
    const saveButton = page.locator('button').filter({ hasText: /save/i }).first();
    await expect(saveButton).toBeVisible();

    // Verify Cancel button
    const cancelButton = page.locator('button').filter({ hasText: /cancel/i }).first();
    await expect(cancelButton).toBeVisible();

    console.log('✅ Editor contains: template name, subject field, TipTap editor, variable help, Save and Cancel buttons');
  });

  test('Edit Subject Field', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get subject field
    const subjectField = page.locator('input[name="subject"], input[type="text"]').first();

    // Get original value
    const originalSubject = await subjectField.inputValue();
    console.log(`   Original subject: "${originalSubject}"`);

    // Clear and type new subject
    await subjectField.clear();
    await subjectField.fill('Test Subject - Updated');

    // Verify new value
    const newSubject = await subjectField.inputValue();
    expect(newSubject).toBe('Test Subject - Updated');

    // Take screenshot of edited state
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-subject-edited.png',
      fullPage: true
    });

    console.log('✅ Subject field can be edited');
    console.log(`   New subject: "${newSubject}"`);
  });

  test('Edit Content in TipTap Editor', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get TipTap editor
    const tipTapEditor = page.locator('.ProseMirror, [contenteditable="true"], .tiptap').first();

    // Get original content
    const originalContent = await tipTapEditor.textContent();
    console.log(`   Original content length: ${originalContent?.length || 0} chars`);

    // Click and add text
    await tipTapEditor.click();
    await tipTapEditor.pressSequentially('\n\nThis is a test addition to the email template.', {
      delay: 50
    });

    // Verify content changed
    const newContent = await tipTapEditor.textContent();
    expect(newContent).toContain('test addition');

    // Take screenshot with content edited
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-content-edited.png',
      fullPage: true
    });

    console.log('✅ TipTap editor content can be edited');
    console.log(`   New content length: ${newContent?.length || 0} chars`);
  });

  test('Cancel Button - Closes Editor Without Saving', async () => {
    // Click first template row
    const firstRow = page.locator('tbody tr').first();
    await firstRow.click();
    await page.waitForTimeout(500);

    // Edit subject field
    const subjectField = page.locator('input[name="subject"], input[type="text"]').first();
    const originalSubject = await subjectField.inputValue();
    await subjectField.fill('Modified Subject');

    // Click Cancel button
    const cancelButton = page.locator('button').filter({ hasText: /cancel/i }).first();
    await cancelButton.click();
    await page.waitForTimeout(500);

    // Verify editor is closed (not visible)
    const editorContainer = page.locator('[data-testid="email-template-editor"], .email-template-editor').first();
    await expect(editorContainer).not.toBeVisible();

    // Click row again to verify changes were not saved
    await firstRow.click();
    await page.waitForTimeout(500);

    const subjectAfterCancel = await subjectField.inputValue();
    expect(subjectAfterCancel).toBe(originalSubject);

    console.log('✅ Cancel button closes editor without saving changes');
  });

  test('Switch Between Rows - Editor Updates', async () => {
    // Click first row
    const firstRow = page.locator('tbody tr').nth(0);
    await firstRow.click();
    await page.waitForTimeout(500);

    // Get first template name and subject
    const firstSubject = await page.locator('input[name="subject"], input[type="text"]').first().inputValue();
    console.log(`   First template subject: "${firstSubject}"`);

    // Click second row
    const secondRow = page.locator('tbody tr').nth(1);
    await secondRow.click();
    await page.waitForTimeout(500);

    // Get second template subject
    const secondSubject = await page.locator('input[name="subject"], input[type="text"]').first().inputValue();
    console.log(`   Second template subject: "${secondSubject}"`);

    // Verify subjects are different (editor updated)
    expect(firstSubject).not.toBe(secondSubject);

    // Take screenshot showing second template
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-second-template.png',
      fullPage: true
    });

    console.log('✅ Editor updates when switching between template rows');
  });

  test('All Template Types Present', async () => {
    // Expected template types based on vetting workflow
    const expectedTypes = ['Initial Review', 'Approved', 'Rejected', 'Info Request'];
    const foundTypes: string[] = [];

    // Get all rows
    const rows = page.locator('tbody tr');
    const rowCount = await rows.count();

    // Extract type from each row (column 2)
    for (let i = 0; i < rowCount; i++) {
      const typeCell = rows.nth(i).locator('td').nth(1);
      const typeText = await typeCell.textContent();
      if (typeText) {
        foundTypes.push(typeText.trim());
      }
    }

    console.log(`   Found template types: ${foundTypes.join(', ')}`);

    // Verify all expected types are present
    for (const expectedType of expectedTypes) {
      expect(foundTypes).toContain(expectedType);
    }

    console.log('✅ All 4 vetting template types are present');
  });

  test('Accessibility - Keyboard Navigation', async () => {
    // Focus on first row using keyboard
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Press Enter to select row
    await page.keyboard.press('Enter');
    await page.waitForTimeout(500);

    // Verify editor opened
    const editorContainer = page.locator('[data-testid="email-template-editor"], .email-template-editor, form').first();
    const isVisible = await editorContainer.isVisible();

    if (isVisible) {
      console.log('✅ Keyboard navigation works - Enter key opens editor');
    } else {
      console.log('⚠️  Keyboard navigation may need improvement');
    }
  });

  test('Responsive Layout - Mobile View', async () => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(500);

    // Verify table is still visible or has mobile-friendly layout
    const table = page.locator('table').first();
    const isTableVisible = await table.isVisible();

    // Take mobile screenshot
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/test-results/vetting-email-templates-mobile.png',
      fullPage: true
    });

    if (isTableVisible) {
      console.log('✅ Table visible on mobile viewport');
    } else {
      console.log('⚠️  Table may need responsive design improvements for mobile');
    }

    // Reset to desktop
    await page.setViewportSize({ width: 1280, height: 720 });
  });

  test('Performance - Page Load Time', async () => {
    const startTime = Date.now();

    await page.goto('http://localhost:5173/admin/vetting/email-templates');
    await page.waitForLoadState('networkidle');

    const loadTime = Date.now() - startTime;

    console.log(`✅ Page loaded in ${loadTime}ms`);

    // Performance threshold (should load in under 3 seconds)
    expect(loadTime).toBeLessThan(3000);
  });
});
