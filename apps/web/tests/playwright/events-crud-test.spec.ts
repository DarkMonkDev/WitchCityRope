import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Event CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin using correct React authentication patterns
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
  });

  test('Phase 2: Admin Events Page - Create Event button opens modal', async ({ page }) => {
    console.log('🧪 Testing Phase 2: Event CRUD operations...');
    
    // Check that the admin events page loads
    await expect(page.locator('text=Event Management')).toBeVisible();
    
    // Check that Create Event button exists
    const createButton = page.locator('button:has-text("Create Event")');
    await expect(createButton).toBeVisible();
    
    // Click Create Event button
    await createButton.click();
    
    // Check that modal opens
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });
    
    // Check that EventForm is displayed in the modal
    const eventForm = page.locator('[data-testid="event-form"]');
    await expect(eventForm).toBeVisible();
    
    // Check form has required tabs
    await expect(page.locator('button[role="tab"]:has-text("Basic Info")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Tickets/Orders")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Emails")')).toBeVisible();
    await expect(page.locator('button[role="tab"]:has-text("Volunteers")')).toBeVisible();
    
    console.log('✅ Phase 2 Test Passed: Create Event modal opens with EventForm');
  });

  test('Phase 2: Admin Events Page - Edit and Delete buttons are present', async ({ page }) => {
    console.log('🧪 Testing Phase 2: Edit and Delete functionality...');
    
    // Check that the admin events page loads
    await expect(page.locator('text=Event Management')).toBeVisible();
    
    // Look for any admin event cards
    const eventCards = page.locator('[data-testid="admin-event"]');
    const count = await eventCards.count();
    
    if (count > 0) {
      console.log(`Found ${count} event(s) on the page`);
      
      // Check first event card has action buttons
      const firstCard = eventCards.first();
      
      // Check for View button
      const viewButton = firstCard.locator('[title="View Event"]');
      await expect(viewButton).toBeVisible();
      
      // Check for Edit button
      const editButton = firstCard.locator('[title="Edit Event"]');
      await expect(editButton).toBeVisible();
      
      // Check for Delete button  
      const deleteButton = firstCard.locator('[title="Delete Event"]');
      await expect(deleteButton).toBeVisible();
      
      // Test Edit button opens modal
      await editButton.click();
      const modal = page.locator('[role="dialog"]');
      await expect(modal).toBeVisible({ timeout: 5000 });
      
      // Close modal
      const closeButton = page.locator('[aria-label="Close modal"]');
      if (await closeButton.isVisible()) {
        await closeButton.click();
      } else {
        // Press escape to close
        await page.keyboard.press('Escape');
      }
      
      await page.waitForTimeout(500);
      
      // Test Delete button opens confirmation modal
      await deleteButton.click();
      const deleteModal = page.locator('[role="dialog"]:has-text("Confirm Delete")');
      await expect(deleteModal).toBeVisible({ timeout: 5000 });
      
      console.log('✅ Edit and Delete buttons work correctly');
    } else {
      console.log('No events found - checking empty state');
      
      // Check for empty state message
      await expect(page.locator('text=No Events Created Yet')).toBeVisible();
      await expect(page.locator('button:has-text("Create Your First Event")')).toBeVisible();
      
      console.log('✅ Empty state displays correctly');
    }
    
    console.log('✅ Phase 2 Test Passed: CRUD buttons are functional');
  });
});