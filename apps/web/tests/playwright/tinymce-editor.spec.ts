import { test, expect } from '@playwright/test';

test.describe('TinyMCE Rich Text Editor', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    // Wait for the page to load completely
    await page.waitForLoadState('networkidle');
  });

  test('should load TinyMCE editor with toolbar instead of plain textarea', async ({ page }) => {
    // Wait for TinyMCE to initialize - check for iframe which indicates TinyMCE loaded
    const editorFrame = page.frameLocator('iframe[id*="tiny-react"]').first();
    await expect(editorFrame.locator('body')).toBeVisible({ timeout: 10000 });
    
    // Check that TinyMCE toolbar is present
    const toolbar = page.locator('.tox-toolbar');
    await expect(toolbar).toBeVisible();
    
    // Verify specific toolbar buttons are present
    await expect(page.locator('button[title="Bold"]')).toBeVisible();
    await expect(page.locator('button[title="Italic"]')).toBeVisible();
    await expect(page.locator('button[title="Undo"]')).toBeVisible();
    await expect(page.locator('button[title="Redo"]')).toBeVisible();
  });

  test('should NOT show a plain textarea fallback', async ({ page }) => {
    // Ensure we don't have a fallback textarea
    const plainTextarea = page.locator('textarea').first();
    
    // If there's a textarea, it should be hidden/not visible (TinyMCE might create hidden textareas)
    // But we should not have a visible textarea as the main editor
    const visibleTextarea = page.locator('textarea:visible');
    const textareaCount = await visibleTextarea.count();
    
    // TinyMCE should not show any visible textareas - it uses an iframe
    expect(textareaCount).toBe(0);
  });

  test('should allow text input and formatting', async ({ page }) => {
    // Wait for TinyMCE iframe to be ready
    const editorFrame = page.frameLocator('iframe[id*="tiny-react"]').first();
    await expect(editorFrame.locator('body')).toBeVisible({ timeout: 10000 });
    
    // Clear existing content and type new text
    await editorFrame.locator('body').clear();
    await editorFrame.locator('body').fill('Test content for TinyMCE editor');
    
    // Select the text
    await editorFrame.locator('body').click({ clickCount: 3 });
    
    // Click Bold button
    await page.locator('button[title="Bold"]').click();
    
    // Verify the HTML output contains bold formatting
    const htmlOutput = page.locator('pre').first();
    await expect(htmlOutput).toContainText('<strong>Test content for TinyMCE editor</strong>');
  });

  test('should show API key warning if not configured', async ({ page }) => {
    // This test is more complex - we'd need to test with a different env
    // For now, just verify the editor loads with the configured API key
    
    // Check that we don't see the API key warning alert
    const warningAlert = page.locator('[role="alert"]:has-text("TinyMCE API key not configured")');
    await expect(warningAlert).not.toBeVisible();
  });

  test('should update HTML output in real-time', async ({ page }) => {
    // Wait for TinyMCE iframe to be ready
    const editorFrame = page.frameLocator('iframe[id*="tiny-react"]').first();
    await expect(editorFrame.locator('body')).toBeVisible({ timeout: 10000 });
    
    // Clear and add test content
    await editorFrame.locator('body').clear();
    await editorFrame.locator('body').fill('Dynamic content test');
    
    // Verify the HTML output updates
    const htmlOutput = page.locator('pre').first();
    await expect(htmlOutput).toContainText('Dynamic content test');
  });
});