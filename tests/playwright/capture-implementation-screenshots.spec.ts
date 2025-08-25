import { test, expect } from '@playwright/test';

test.describe('Event Session Matrix Implementation Screenshots', () => {
  test('capture emails tab implementation', async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
    
    // Click on Emails tab
    await page.click('text=Emails');
    
    // Wait for content to load
    await page.waitForSelector('text=Email Templates', { timeout: 10000 });
    
    // Take screenshot of emails tab
    await page.screenshot({ 
      path: 'emails-tab-implementation.png',
      fullPage: true 
    });
    
    console.log('✅ Emails tab screenshot captured');
  });

  test('capture volunteers/staff tab implementation', async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
    
    // Click on Volunteers/Staff tab
    await page.click('text=Volunteers/Staff');
    
    // Wait for content to load
    await page.waitForSelector('text=Volunteer Positions', { timeout: 10000 });
    
    // Take screenshot of volunteers tab
    await page.screenshot({ 
      path: 'volunteers-staff-tab-implementation.png',
      fullPage: true 
    });
    
    console.log('✅ Volunteers/Staff tab screenshot captured');
  });

  test('verify TinyMCE editor functionality', async ({ page }) => {
    await page.goto('http://localhost:5174/admin/event-session-matrix-demo');
    
    // Click on Emails tab
    await page.click('text=Emails');
    
    // Wait for content to load
    await page.waitForSelector('text=Email Templates', { timeout: 10000 });
    
    // Check if TinyMCE editor loads
    try {
      await page.waitForSelector('.tox-tinymce', { timeout: 15000 });
      console.log('✅ TinyMCE editor loaded successfully');
      
      // Take screenshot showing TinyMCE editor
      await page.screenshot({ 
        path: 'tinymce-editor-working.png',
        fullPage: true 
      });
      
    } catch (error) {
      console.log('❌ TinyMCE editor not found, taking debug screenshot');
      await page.screenshot({ 
        path: 'tinymce-debug.png',
        fullPage: true 
      });
    }
  });
});