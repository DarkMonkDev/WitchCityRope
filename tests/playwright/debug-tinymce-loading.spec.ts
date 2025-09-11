import { test, expect } from '@playwright/test';

test('Debug TinyMCE Loading Issues', async ({ page }) => {
  // Navigate to the demo page
  await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
  
  // Wait for page to load
  await page.waitForTimeout(5000);
  
  // Take screenshot before checking
  await page.screenshot({ path: 'debug-before-scroll.png', fullPage: true });
  
  // Scroll down to see the full description editor
  await page.locator('text=Full Event Description').scrollIntoViewIfNeeded();
  await page.waitForTimeout(2000);
  
  // Take another screenshot
  await page.screenshot({ path: 'debug-after-scroll.png', fullPage: true });
  
  // Check for any console errors
  page.on('console', msg => {
    console.log(`PAGE CONSOLE [${msg.type()}]: ${msg.text()}`);
  });
  
  // Check for TinyMCE elements more specifically
  const tinyMCEContainers = await page.locator('.tox-tinymce').count();
  const tinyMCEToolbars = await page.locator('.tox-toolbar').count();
  const tinyMCEIframes = await page.locator('iframe[id*="tinymce"]').count();
  const editorDivs = await page.locator('[data-editor]').count();
  
  console.log('TinyMCE containers (.tox-tinymce):', tinyMCEContainers);
  console.log('TinyMCE toolbars (.tox-toolbar):', tinyMCEToolbars);
  console.log('TinyMCE iframes:', tinyMCEIframes);
  console.log('Editor divs [data-editor]:', editorDivs);
  
  // Check if TinyMCE script is loaded
  const tinyMCEScriptLoaded = await page.evaluate(() => {
    return typeof window.tinymce !== 'undefined';
  });
  console.log('TinyMCE script loaded:', tinyMCEScriptLoaded);
  
  // Check for React Editor components
  const reactEditors = await page.locator('[data-testid*="editor"], .tinymce-editor').count();
  console.log('React Editor components:', reactEditors);
  
  // Check for any elements that might indicate the editors are trying to load
  const anyEditorElements = await page.locator('div:has(iframe), div:has([contenteditable]), textarea').count();
  console.log('Any potential editor elements:', anyEditorElements);
  
  // List all visible text content to see what's actually on the page
  const pageText = await page.locator('body').textContent();
  const hasFullDescriptionText = pageText?.includes('Full Event Description');
  const hasPoliciesText = pageText?.includes('Policies & Procedures');
  
  console.log('Page contains "Full Event Description":', hasFullDescriptionText);
  console.log('Page contains "Policies & Procedures":', hasPoliciesText);
  
  // Check for loading states or error messages
  const hasLoadingText = pageText?.includes('Loading') || pageText?.includes('loading');
  const hasErrorText = pageText?.includes('Error') || pageText?.includes('error');
  
  console.log('Has loading text:', hasLoadingText);
  console.log('Has error text:', hasErrorText);
});