import { test, expect } from '@playwright/test';

/**
 * E2E Test: Tiptap Editors Rendering on Admin Events Details Page
 *
 * This test verifies that all three Tiptap rich text editors render correctly
 * on the admin event details page after the TinyMCE to Tiptap migration.
 *
 * Critical editors to verify:
 * 1. Full Description editor (Event Details tab)
 * 2. Policies & Procedures editor (Event Details tab)
 * 3. Email Content editor (Email Templates tab)
 */

test.describe('Tiptap Editor Rendering', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto('http://localhost:5173/login');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Wait for navigation to complete
    await page.waitForURL('**/admin/dashboard', { timeout: 10000 });

    // Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
  });

  test('should render Full Description Tiptap editor on Event Details tab', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Verify we're on the Event Details tab (default tab)
    await expect(page.locator('button[data-value="basic-info"]')).toHaveAttribute('data-active', 'true');

    // Verify Full Description editor is present
    const fullDescriptionLabel = page.locator('text=Full Description');
    await expect(fullDescriptionLabel).toBeVisible();

    // Verify Tiptap editor toolbar is present (indicates editor rendered)
    const editorToolbar = page.locator('.mantine-RichTextEditor-toolbar').first();
    await expect(editorToolbar).toBeVisible();

    // Verify editor content area is present
    const editorContent = page.locator('.mantine-RichTextEditor-content').first();
    await expect(editorContent).toBeVisible();

    // Verify toolbar controls are present (Bold, Italic, etc.)
    await expect(editorToolbar.locator('button[aria-label*="Bold"]')).toBeVisible();
    await expect(editorToolbar.locator('button[aria-label*="Italic"]')).toBeVisible();

    console.log('✅ Full Description Tiptap editor rendered successfully');
  });

  test('should render Policies & Procedures Tiptap editor on Event Details tab', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Verify Policies & Procedures editor is present
    const policiesLabel = page.locator('text=Policies & Procedures');
    await expect(policiesLabel).toBeVisible();

    // Count Tiptap editors on this tab (should be 2: Full Description + Policies)
    const editors = page.locator('.mantine-RichTextEditor-root');
    await expect(editors).toHaveCount(2);

    // Verify second editor (Policies) toolbar is present
    const policiesToolbar = page.locator('.mantine-RichTextEditor-toolbar').nth(1);
    await expect(policiesToolbar).toBeVisible();

    // Verify second editor content area is present
    const policiesContent = page.locator('.mantine-RichTextEditor-content').nth(1);
    await expect(policiesContent).toBeVisible();

    console.log('✅ Policies & Procedures Tiptap editor rendered successfully');
  });

  test('should render Email Content Tiptap editor on Email Templates tab', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Navigate to Email Templates tab
    await page.click('button:has-text("Email Templates")');
    await page.waitForTimeout(500); // Wait for tab transition

    // Verify we're on the Email Templates tab
    await expect(page.locator('[data-testid="panel-email-templates"]')).toBeVisible();

    // Verify Email Content editor is present
    const emailContentLabel = page.locator('text=Email Content');
    await expect(emailContentLabel).toBeVisible();

    // Verify Tiptap editor toolbar is present
    const editorToolbar = page.locator('.mantine-RichTextEditor-toolbar');
    await expect(editorToolbar).toBeVisible();

    // Verify editor content area is present
    const editorContent = page.locator('.mantine-RichTextEditor-content');
    await expect(editorContent).toBeVisible();

    // Verify variable insertion hint is present
    await expect(page.locator('text=Available variables:')).toBeVisible();

    console.log('✅ Email Content Tiptap editor rendered successfully');
  });

  test('should allow typing in Full Description editor', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Click into the first editor (Full Description)
    const editorContent = page.locator('.mantine-RichTextEditor-content').first();
    await editorContent.click();

    // Type test content
    const testContent = 'This is a test event description with rich text formatting.';
    await page.keyboard.type(testContent);

    // Verify content was entered
    await expect(editorContent.locator('.tiptap')).toContainText(testContent);

    console.log('✅ Full Description editor accepts text input');
  });

  test('should allow formatting text with toolbar controls', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Click into the first editor
    const editorContent = page.locator('.mantine-RichTextEditor-content').first();
    await editorContent.click();

    // Type test content
    await page.keyboard.type('Bold text');

    // Select all text
    await page.keyboard.press('Control+a');

    // Click Bold button
    const toolbar = page.locator('.mantine-RichTextEditor-toolbar').first();
    await toolbar.locator('button[aria-label*="Bold"]').click();

    // Verify bold formatting was applied (check for <strong> or <b> tag)
    const boldText = editorContent.locator('strong, b');
    await expect(boldText).toHaveCount(1);

    console.log('✅ Toolbar controls work correctly');
  });

  test('comprehensive: all three editors render on their respective tabs', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Tab 1: Event Details - Check for 2 editors (Full Description + Policies)
    await expect(page.locator('button[data-value="basic-info"]')).toHaveAttribute('data-active', 'true');
    let editors = page.locator('.mantine-RichTextEditor-root');
    await expect(editors).toHaveCount(2);
    console.log('✅ Tab 1 (Event Details): 2 Tiptap editors found');

    // Tab 2: Email Templates - Check for 1 editor (Email Content)
    await page.click('button:has-text("Email Templates")');
    await page.waitForTimeout(500);
    await expect(page.locator('[data-testid="panel-email-templates"]')).toBeVisible();
    editors = page.locator('.mantine-RichTextEditor-root');
    await expect(editors).toHaveCount(1);
    console.log('✅ Tab 2 (Email Templates): 1 Tiptap editor found');

    // Summary
    console.log('✅ COMPREHENSIVE TEST PASSED: All 3 Tiptap editors render correctly');
  });

  test('should NOT show TinyMCE editors (migration verification)', async ({ page }) => {
    // Click "Create Event" button
    await page.click('button:has-text("Create Event")');

    // Wait for event form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Verify NO TinyMCE elements exist
    const tinyMCEElements = page.locator('.tox-tinymce, .mce-tinymce');
    await expect(tinyMCEElements).toHaveCount(0);

    // Verify Tiptap elements DO exist
    const tiptapElements = page.locator('.mantine-RichTextEditor-root');
    await expect(tiptapElements.first()).toBeVisible();

    console.log('✅ TinyMCE successfully replaced with Tiptap');
  });
});
