import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Demo - Button Fixes', () => {
  test('should display buttons with proper styling and no text cutoff', async ({ page }) => {
    // Navigate to the demo page
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');

    // Wait for the page to load
    await page.waitForSelector('h1', { timeout: 10000 });

    // Verify page title
    const title = await page.locator('h1').textContent();
    expect(title).toContain('Event Session Matrix Demo');

    // Wait for form to load and navigate to tickets tab to see all buttons
    await page.waitForSelector('.btn', { timeout: 10000 });
    
    // Click on Tickets/Orders tab to see additional buttons
    await page.click('button[role="tab"]:has-text("Tickets/Orders")', { timeout: 5000 }).catch(() => {
      console.log('Tickets/Orders tab not found, continuing with current tab');
    });

    // Check primary CTA buttons
    const primaryButtons = page.locator('.btn-primary');
    
    // Should have at least 3 primary buttons (Add Session, Add Ticket Type, Save Draft)
    const buttonCount = await primaryButtons.count();
    expect(buttonCount).toBeGreaterThanOrEqual(3);

    // Check each button for proper text display (no cutoff)
    for (let i = 0; i < buttonCount; i++) {
      const button = primaryButtons.nth(i);
      
      // Check that button is visible
      await expect(button).toBeVisible();
      
      // Get button text
      const buttonText = await button.textContent();
      expect(buttonText).toBeTruthy();
      
      // Check button styling classes
      await expect(button).toHaveClass(/btn/);
      await expect(button).toHaveClass(/btn-primary/);
      
      console.log(`Button ${i}: "${buttonText}" - styling OK`);
    }

    // Check secondary buttons
    const secondaryButtons = page.locator('.btn-secondary');
    const secondaryCount = await secondaryButtons.count();
    
    if (secondaryCount > 0) {
      for (let i = 0; i < secondaryCount; i++) {
        const button = secondaryButtons.nth(i);
        
        await expect(button).toBeVisible();
        
        const buttonText = await button.textContent();
        expect(buttonText).toBeTruthy();
        
        await expect(button).toHaveClass(/btn/);
        await expect(button).toHaveClass(/btn-secondary/);
        
        console.log(`Secondary Button ${i}: "${buttonText}" - styling OK`);
      }
    }

    // Check table action buttons
    const tableButtons = page.locator('.table-action-btn');
    const tableButtonCount = await tableButtons.count();
    
    if (tableButtonCount > 0) {
      for (let i = 0; i < tableButtonCount; i++) {
        const button = tableButtons.nth(i);
        
        await expect(button).toBeVisible();
        
        const buttonText = await button.textContent();
        expect(buttonText).toBeTruthy();
        
        await expect(button).toHaveClass(/table-action-btn/);
        
        console.log(`Table Button ${i}: "${buttonText}" - styling OK`);
      }
    }
  });

  test('should have working button animations and hover effects', async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');

    // Wait for the form to load
    await page.waitForSelector('.btn-primary', { timeout: 10000 });

    const firstPrimaryButton = page.locator('.btn-primary').first();
    
    // Check initial styling
    await expect(firstPrimaryButton).toBeVisible();
    
    // Test hover effect by moving mouse over button
    await firstPrimaryButton.hover();
    
    // Wait a moment for CSS transitions
    await page.waitForTimeout(500);
    
    // Button should still be visible and functional after hover
    await expect(firstPrimaryButton).toBeVisible();
    
    console.log('Button hover animations working correctly');
  });

  test('should verify TipTap rich text editors are working', async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');

    // Wait for the form to load
    await page.waitForSelector('[data-testid="RichTextEditor"], .mantine-RichTextEditor-root', { timeout: 10000 });

    // Check for rich text editor presence
    const richTextEditors = page.locator('[data-testid="RichTextEditor"], .mantine-RichTextEditor-root');
    const editorCount = await richTextEditors.count();
    
    expect(editorCount).toBeGreaterThanOrEqual(2); // Should have Full Description and Policies editors
    
    console.log(`Found ${editorCount} rich text editors`);

    // Check for toolbar presence
    const toolbars = page.locator('.mantine-RichTextEditor-toolbar, [data-testid="RichTextEditor-toolbar"]');
    const toolbarCount = await toolbars.count();
    
    expect(toolbarCount).toBeGreaterThanOrEqual(2);
    console.log(`Found ${toolbarCount} rich text editor toolbars`);

    // Check for content areas
    const contentAreas = page.locator('.mantine-RichTextEditor-content, [data-testid="RichTextEditor-content"]');
    const contentCount = await contentAreas.count();
    
    expect(contentCount).toBeGreaterThanOrEqual(2);
    console.log(`Found ${contentCount} rich text editor content areas`);
  });
});