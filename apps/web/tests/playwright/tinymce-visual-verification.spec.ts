import { test, expect } from '@playwright/test';

test.describe('TinyMCE Visual Verification', () => {
  test('should show TinyMCE editors with toolbars (not plain textareas)', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    await page.waitForLoadState('networkidle');

    // Scroll down to see the rich text editors
    await page.locator('text=Full Event Description').scrollIntoViewIfNeeded();
    
    // Wait a moment for TinyMCE to fully initialize
    await page.waitForTimeout(3000);
    
    // Take a screenshot of the editors area
    await page.screenshot({ 
      path: 'test-results/tinymce-editors-visible.png',
      fullPage: true 
    });

    // Check for TinyMCE toolbars - these should be visible
    const toolbars = page.locator('.tox-toolbar');
    const toolbarCount = await toolbars.count();
    console.log(`Found ${toolbarCount} TinyMCE toolbars`);
    
    expect(toolbarCount).toBeGreaterThan(0);
    
    // Verify specific toolbar buttons exist
    const boldButton = page.locator('button[title="Bold"]').first();
    await expect(boldButton).toBeVisible();
    console.log('Bold button is visible');
    
    const italicButton = page.locator('button[title="Italic"]').first();
    await expect(italicButton).toBeVisible();
    console.log('Italic button is visible');

    // Check that we have actual TinyMCE editor containers, not plain textareas
    const editorContainers = page.locator('.tox-editor-container');
    const containerCount = await editorContainers.count();
    console.log(`Found ${containerCount} TinyMCE editor containers`);
    
    expect(containerCount).toBeGreaterThan(0);

    // Verify we have iframes (TinyMCE uses iframes for the content area)
    const editorIframes = page.locator('iframe[id*="tiny"]');
    const iframeCount = await editorIframes.count();
    console.log(`Found ${iframeCount} TinyMCE content iframes`);
    
    expect(iframeCount).toBeGreaterThan(0);

    // Make sure we don't see any bare textareas that would indicate fallback mode
    const visibleTextareas = page.locator('textarea:visible');
    const visibleTextareaCount = await visibleTextareas.count();
    console.log(`Found ${visibleTextareaCount} visible textareas (should be 0 or very few)`);
    
    // TinyMCE editors should not show visible textareas - they use hidden ones internally
    // We might see 1-2 for other form fields, but not many
    expect(visibleTextareaCount).toBeLessThan(3);
  });

  test('should allow interaction with TinyMCE editor', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/event-session-matrix-demo');
    await page.waitForLoadState('networkidle');

    // Scroll to editors
    await page.locator('text=Full Event Description').scrollIntoViewIfNeeded();
    await page.waitForTimeout(3000);

    // Find the first TinyMCE editor iframe
    const editorFrame = page.frameLocator('iframe[id*="tiny"]').first();
    
    // Clear and add content to verify the editor works
    await editorFrame.locator('body[contenteditable="true"]').clear();
    await editorFrame.locator('body[contenteditable="true"]').fill('Test content for TinyMCE verification');
    
    // Select text and make it bold
    await editorFrame.locator('body[contenteditable="true"]').selectText();
    await page.locator('button[title="Bold"]').first().click();
    
    // Check that the content is now bold
    const boldContent = await editorFrame.locator('strong').textContent();
    expect(boldContent).toBe('Test content for TinyMCE verification');
    console.log('Successfully made text bold in TinyMCE editor');

    // Take a final screenshot showing the working editor
    await page.screenshot({ 
      path: 'test-results/tinymce-working-with-content.png',
      fullPage: true 
    });
  });
});