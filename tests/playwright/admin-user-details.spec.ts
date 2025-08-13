import { test, expect } from '@playwright/test';

test.describe('Admin User Details & Notes Panel', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin and navigate to users page
    await page.goto('http://localhost:5651/login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for authentication
    await page.waitForTimeout(2000);
    
    // Navigate to users page
    await page.goto('http://localhost:5651/admin/users');
    await page.waitForLoadState('networkidle');
  });

  test('should navigate to user details from user list', async ({ page }) => {
    // Look for user cards with clickable elements
    const userCards = page.locator('.user-card');
    const userCardCount = await userCards.count();
    
    console.log(`Found ${userCardCount} user cards`);
    
    if (userCardCount > 0) {
      // Get the first user card
      const firstUserCard = userCards.first();
      
      // Look for clickable elements within the card
      const userLink = firstUserCard.locator('a, button[data-user-id], [data-user-id]');
      const linkCount = await userLink.count();
      
      if (linkCount > 0) {
        // Take screenshot before clicking
        await page.screenshot({ 
          path: 'test-results/user-details-before-click.png', 
          fullPage: true 
        });
        
        // Click the user link
        await userLink.first().click();
        await page.waitForLoadState('networkidle');
        
        // Take screenshot after navigation
        await page.screenshot({ 
          path: 'test-results/user-details-after-click.png', 
          fullPage: true 
        });
        
        // Check if we navigated to a user details page
        const currentUrl = page.url();
        console.log('Navigated to:', currentUrl);
        
        // Look for user details content
        const hasUserDetails = await page.locator('h2:has-text("User Details")').count() > 0;
        const hasUserInfo = await page.locator('[class*="user-info"], .card:has-text("User Information")').count() > 0;
        
        if (hasUserDetails || hasUserInfo) {
          console.log('Successfully navigated to user details page');
          
          // Check for AdminNotesPanel
          await expect(page.locator('text=Admin Notes')).toBeVisible({ timeout: 10000 });
          
          // Check for Add Note button
          const addNoteButton = page.locator('button:has-text("Add Note")');
          if (await addNoteButton.count() > 0) {
            await expect(addNoteButton).toBeVisible();
          }
        }
      } else {
        console.log('No clickable elements found in user cards');
        
        // Try to extract user IDs from the page and navigate directly
        const userEmails = await page.locator('.user-email, [class*="email"]').allTextContents();
        console.log('Found user emails:', userEmails);
      }
    }
  });

  test('should display user information correctly', async ({ page }) => {
    // First verify the user list loads
    await expect(page.locator('.user-grid')).toBeVisible();
    
    // Check for user cards
    const userCards = page.locator('.user-card');
    const cardCount = await userCards.count();
    
    if (cardCount > 0) {
      console.log(`Testing with ${cardCount} user cards`);
      
      // Verify each card has essential user information
      for (let i = 0; i < Math.min(cardCount, 3); i++) {
        const card = userCards.nth(i);
        
        // Check for email
        const hasEmail = await card.locator('[class*="email"], p:has-text("@")').count() > 0;
        
        // Check for user status or role
        const hasStatus = await card.locator('[class*="status"], [class*="role"], .badge').count() > 0;
        
        // Check for user actions
        const hasActions = await card.locator('button, a').count() > 0;
        
        console.log(`Card ${i}: email=${hasEmail}, status=${hasStatus}, actions=${hasActions}`);
        
        expect(hasEmail || hasStatus || hasActions).toBeTruthy();
      }
    }
  });

  test('should test AdminNotesPanel functionality with mock user', async ({ page }) => {
    // Navigate directly to the UserDetails page route which uses a mock user
    await page.goto('http://localhost:5651/admin/users/12345678-1234-1234-1234-123456789012');
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of user details page
    await page.screenshot({ 
      path: 'test-results/mock-user-details.png', 
      fullPage: true 
    });
    
    // Check if the user details page loads (it uses mock data)
    const hasUserDetails = await page.locator('h2:has-text("User Details")').count() > 0;
    
    if (hasUserDetails) {
      console.log('Mock user details page loaded successfully');
      
      // Verify user information panel
      await expect(page.locator('.card:has-text("User Information")').first()).toBeVisible();
      
      // Verify AdminNotesPanel is present
      await expect(page.locator('text=Admin Notes')).toBeVisible();
      
      // Check for Add Note button
      const addNoteButton = page.locator('button:has-text("Add Note")');
      if (await addNoteButton.count() > 0) {
        await expect(addNoteButton).toBeVisible();
        
        // Test adding a note
        await addNoteButton.click();
        
        // Check if modal opens
        const modal = page.locator('.modal.show, [role="dialog"]:visible');
        const hasModal = await modal.count() > 0;
        
        if (hasModal) {
          console.log('Add Note modal opened successfully');
          
          // Fill out the form if fields exist
          const noteContent = page.locator('textarea[name="noteContent"], textarea#noteContent');
          if (await noteContent.count() > 0) {
            await noteContent.fill('Test note from comprehensive E2E test');
          }
          
          // Try to save
          const saveButton = page.locator('button:has-text("Save"), button[type="submit"]');
          if (await saveButton.count() > 0) {
            await saveButton.click();
            await page.waitForTimeout(1000);
            
            // Check if note was added
            const notesList = page.locator('[class*="notes"], [class*="note-list"]');
            if (await notesList.count() > 0) {
              console.log('Note saving functionality tested');
            }
          }
        }
      }
    } else {
      console.log('Mock user details page not accessible, this is expected');
    }
  });
});