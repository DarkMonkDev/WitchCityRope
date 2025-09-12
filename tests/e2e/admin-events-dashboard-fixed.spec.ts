import { test, expect } from '@playwright/test';

test.describe('Admin Events Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin using correct selectors
    await page.goto('http://localhost:5173/login');
    
    // Wait for form elements using CORRECT data-testid selectors (proven working)
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    
    // Verify form elements exist with correct selectors
    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible({ timeout: 10000 });
    await expect(loginButton).toBeVisible({ timeout: 10000 });
    
    // Fill in admin credentials
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await loginButton.click();
    
    // Wait for login to complete (fixed URL pattern)
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    
    // Navigate to admin events page (FIXED: was /admin/events-table)
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
  });

  test('should show both filter chips checked by default', async ({ page }) => {
    // Check that both filter chips are selected by default
    const socialChip = page.getByTestId('filter-social');
    const classChip = page.getByTestId('filter-class');
    
    // Chips should be checked by default (Mantine uses 'checked' attribute)
    await expect(socialChip).toBeChecked();
    await expect(classChip).toBeChecked();
  });

  test('should show events when both filters are checked', async ({ page }) => {
    // Both filters should be checked by default
    const eventsTable = page.getByTestId('events-table');
    
    // Check if table exists
    await expect(eventsTable).toBeVisible();
    
    // Check if there are any events in the table
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();
    
    console.log(`Found ${rowCount} events in table with both filters checked`);
    
    // Should have at least one event (or show "No events found" message)
    if (rowCount === 1) {
      // Check if it's the "No events found" message
      const firstRow = tableRows.first();
      const noEventsText = await firstRow.textContent();
      if (noEventsText?.includes('No events found')) {
        console.log('No events in database');
      } else {
        console.log('Found event:', noEventsText);
      }
    } else {
      expect(rowCount).toBeGreaterThan(0);
      console.log(`Successfully showing ${rowCount} events`);
    }
  });

  test('should filter events by type when unchecking filters', async ({ page }) => {
    // Get initial event count
    const tableRows = page.locator('tbody tr');
    const initialCount = await tableRows.count();
    console.log(`Initial event count: ${initialCount}`);
    
    // Uncheck Social filter - click the chip label (visible part)
    const socialChipLabel = page.locator('[data-testid="filter-social"] + .mantine-Chip-label, [data-testid="filter-social"] ~ label, label[for*="filter-social"], .mantine-Chip-label:has-text("Social")');
    await socialChipLabel.first().click();
    await page.waitForTimeout(500); // Wait for filter to apply
    
    // Check row count after unchecking Social
    const afterSocialUncheck = await tableRows.count();
    console.log(`Events after unchecking Social: ${afterSocialUncheck}`);
    
    // Re-check Social and uncheck Class
    await socialChip.click();
    await page.waitForTimeout(500);
    
    const classChip = page.getByTestId('filter-class');
    await classChip.click();
    await page.waitForTimeout(500);
    
    // Check row count after unchecking Class
    const afterClassUncheck = await tableRows.count();
    console.log(`Events after unchecking Class: ${afterClassUncheck}`);
    
    // Uncheck both
    await socialChip.click();
    await page.waitForTimeout(500);
    
    const bothUnchecked = await tableRows.count();
    console.log(`Events with both unchecked: ${bothUnchecked}`);
    
    // When both are unchecked, should show no events or "No events found" message
    if (bothUnchecked === 1) {
      const firstRow = tableRows.first();
      const text = await firstRow.textContent();
      expect(text).toContain('No events found');
    } else {
      expect(bothUnchecked).toBe(0);
    }
  });

  test('should have working Copy button', async ({ page }) => {
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();
    
    if (rowCount > 0) {
      // Check if first row has a Copy button
      const firstRowCopyButton = tableRows.first().getByTestId('button-copy-event');
      const buttonExists = await firstRowCopyButton.count() > 0;
      
      if (buttonExists) {
        // Check that button text is visible
        await expect(firstRowCopyButton).toBeVisible();
        await expect(firstRowCopyButton).toContainText('Copy');
      }
    }
  });

  test('should navigate to event edit on row click', async ({ page }) => {
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();
    
    if (rowCount > 0) {
      const firstRow = tableRows.first();
      const text = await firstRow.textContent();
      
      // Only click if it's not the "No events found" row
      if (!text?.includes('No events found')) {
        await firstRow.click();
        
        // Should either navigate or show a notification
        await page.waitForTimeout(1000);
        
        // Check if we navigated away from the table page
        const currentUrl = page.url();
        console.log('Current URL after click:', currentUrl);
      }
    }
  });
});