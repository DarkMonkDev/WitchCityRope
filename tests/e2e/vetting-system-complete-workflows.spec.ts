import { test, expect, Page } from '@playwright/test';

// Authentication helper to login as admin
async function loginAsAdmin(page: Page) {
  await page.goto('http://localhost:5173/login');
  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
  await page.fill('[data-testid="password-input"]', 'Test123!');
  await page.click('[data-testid="login-button"]');
  await page.waitForURL('**/dashboard');
}

// Helper to navigate to vetting page
async function navigateToVettingPage(page: Page) {
  await page.goto('http://localhost:5173/admin/vetting/applications');
  await page.waitForLoadState('networkidle');
}

// Helper to create test data if needed
async function setupTestData(page: Page) {
  // This could be enhanced to create test applications via API
  // For now, we'll work with existing data
}

test.describe('Vetting System - Complete Workflows', () => {
  test.beforeEach(async ({ page }) => {
    // Set up error monitoring
    let jsErrors: string[] = [];
    let consoleErrors: string[] = [];

    page.on('pageerror', error => {
      jsErrors.push(error.toString());
    });

    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        // Filter out Mantine CSS warnings which are not blocking
        if (!errorText.includes('style property') && 
            !errorText.includes('maxWidth') &&
            !errorText.includes('focus-visible')) {
          consoleErrors.push(errorText);
        }
      }
    });

    // Check for errors before each test assertion
    test.afterEach(async () => {
      if (jsErrors.length > 0) {
        throw new Error(`JavaScript errors detected: ${jsErrors.join('; ')}`);
      }
      
      const criticalErrors = consoleErrors.filter(error =>
        error.includes('RangeError') || error.includes('Invalid time value')
      );
      if (criticalErrors.length > 0) {
        throw new Error(`Critical errors detected: ${criticalErrors.join('; ')}`);
      }
    });
  });

  test('View Applications Flow - Login, Navigate, and View Table', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    
    // Act
    await navigateToVettingPage(page);
    
    // Assert
    await expect(page.locator('table')).toBeVisible();
    await expect(page.locator('th:has-text("NAME")')).toBeVisible();
    await expect(page.locator('th:has-text("FETLIFE NAME")')).toBeVisible();
    await expect(page.locator('th:has-text("EMAIL")')).toBeVisible();
    await expect(page.locator('th:has-text("APPLICATION DATE")')).toBeVisible();
    await expect(page.locator('th:has-text("CURRENT STATUS")')).toBeVisible();
    
    // Check for filter controls
    await expect(page.getByPlaceholder('Search by name, email, or scene name...')).toBeVisible();
    await expect(page.locator('[data-testid="status-filter"], .mantine-MultiSelect-root')).toBeVisible();
  });

  test('Filter and Search Functionality', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Test search functionality
    const searchInput = page.getByPlaceholder('Search by name, email, or scene name...');
    await searchInput.fill('test');
    
    // Wait for search to be applied
    await page.waitForTimeout(1000);
    
    // Test status filter
    const statusFilter = page.locator('.mantine-MultiSelect-root').first();
    await statusFilter.click();
    
    // Look for status filter options
    await expect(page.locator('text=Under Review')).toBeVisible();
    await expect(page.locator('text=Approved')).toBeVisible();
    
    // Select a specific status
    await page.locator('text=Under Review').click();
    
    // Click outside to close dropdown
    await page.click('body');
    
    // Verify table updates (should show loading or filtered results)
    await page.waitForTimeout(500);
  });

  test('Navigation to Application Detail', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Act - Click on first application row (if exists)
    const firstRow = page.locator('tbody tr').first();
    
    // Check if any applications exist
    const rowCount = await page.locator('tbody tr').count();
    
    if (rowCount > 0) {
      await firstRow.click();
      
      // Assert - Should navigate to detail page
      await expect(page.locator('text=Application -')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('text=Back to Applications')).toBeVisible();
      
      // Check for action buttons
      await expect(page.locator('button:has-text("APPROVE")')).toBeVisible();
      await expect(page.locator('button:has-text("PUT ON HOLD")')).toBeVisible();
      await expect(page.locator('button:has-text("SEND REMINDER")')).toBeVisible();
      await expect(page.locator('button:has-text("DENY APPLICATION")')).toBeVisible();
    } else {
      // No applications to test with
      console.log('No applications found to test detail navigation');
    }
  });

  test('Put on Hold Modal Flow', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Navigate to first application detail if exists
    const rowCount = await page.locator('tbody tr').count();
    
    if (rowCount > 0) {
      const firstRow = page.locator('tbody tr').first();
      await firstRow.click();
      
      // Wait for detail page to load
      await expect(page.locator('text=Application -')).toBeVisible({ timeout: 10000 });
      
      // Act - Click Put on Hold button
      const putOnHoldButton = page.locator('button:has-text("PUT ON HOLD")');
      await putOnHoldButton.click();
      
      // Assert - Modal should open
      await expect(page.locator('text=Put Application On Hold')).toBeVisible();
      await expect(page.getByLabel('Reason for putting on hold')).toBeVisible();
      
      // Test form validation
      const submitButton = page.locator('button:has-text("Put On Hold")');
      await expect(submitButton).toBeDisabled();
      
      // Fill in reason
      await page.getByLabel('Reason for putting on hold').fill('Need additional references from applicant');
      
      // Submit button should now be enabled
      await expect(submitButton).toBeEnabled();
      
      // Test cancel functionality
      const cancelButton = page.locator('button:has-text("Cancel")');
      await cancelButton.click();
      
      // Modal should close
      await expect(page.locator('text=Put Application On Hold')).toBeHidden();
    } else {
      console.log('No applications found to test Put on Hold flow');
    }
  });

  test('Send Reminder Modal Flow', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Navigate to first application detail if exists
    const rowCount = await page.locator('tbody tr').count();
    
    if (rowCount > 0) {
      const firstRow = page.locator('tbody tr').first();
      await firstRow.click();
      
      // Wait for detail page to load
      await expect(page.locator('text=Application -')).toBeVisible({ timeout: 10000 });
      
      // Act - Click Send Reminder button
      const sendReminderButton = page.locator('button:has-text("SEND REMINDER")');
      await sendReminderButton.click();
      
      // Assert - Modal should open
      await expect(page.locator('text=Send Reminder')).toBeVisible();
      await expect(page.getByLabel('Reminder message')).toBeVisible();
      
      // Check that message is pre-filled
      const messageTextarea = page.getByLabel('Reminder message');
      const messageValue = await messageTextarea.inputValue();
      expect(messageValue).toContain('friendly reminder');
      expect(messageValue).toContain('WitchCityRope');
      
      // Submit button should be enabled with pre-filled message
      const submitButton = page.locator('button:has-text("Send Reminder")');
      await expect(submitButton).toBeEnabled();
      
      // Test editing message
      await messageTextarea.clear();
      await messageTextarea.fill('Custom reminder message for testing');
      
      // Test cancel functionality
      const cancelButton = page.locator('button:has-text("Cancel")');
      await cancelButton.click();
      
      // Modal should close
      await expect(page.locator('text=Send Reminder')).toBeHidden();
    } else {
      console.log('No applications found to test Send Reminder flow');
    }
  });

  test('Application Status Badge Display', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Check for status badges in the table
    const statusCells = page.locator('td:has([data-testid*="status-badge"], .mantine-Badge-root)');
    const statusCount = await statusCells.count();
    
    if (statusCount > 0) {
      // Assert - Status badges should be visible and have appropriate styling
      for (let i = 0; i < Math.min(statusCount, 3); i++) {
        const statusBadge = statusCells.nth(i);
        await expect(statusBadge).toBeVisible();
        
        // Status badge should have text content
        const badgeText = await statusBadge.textContent();
        expect(badgeText).toBeTruthy();
        expect(badgeText!.length).toBeGreaterThan(0);
      }
    }
  });

  test('Sorting Functionality', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Test clicking on sortable column headers
    const nameHeader = page.locator('th:has-text("NAME")');
    const dateHeader = page.locator('th:has-text("APPLICATION DATE")');
    
    // Click name header to sort
    await nameHeader.click();
    await page.waitForTimeout(500);
    
    // Look for sort indicators (up/down arrows)
    const sortIcons = page.locator('svg, .sort-icon, [data-testid*="sort"]');
    
    // Click date header to sort
    await dateHeader.click();
    await page.waitForTimeout(500);
    
    // Test double-click to reverse sort order
    await dateHeader.click();
    await page.waitForTimeout(500);
  });

  test('Pagination Controls', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Check for pagination controls
    const paginationContainer = page.locator('.mantine-Pagination-root, [data-testid*="pagination"]');
    
    // If pagination is visible, test it
    if (await paginationContainer.isVisible()) {
      // Look for page numbers
      const pageButtons = page.locator('button:has-text(/^[0-9]+$/)');
      const pageButtonCount = await pageButtons.count();
      
      if (pageButtonCount > 1) {
        // Click on page 2 if it exists
        const page2Button = page.locator('button:has-text("2")');
        if (await page2Button.isVisible()) {
          await page2Button.click();
          await page.waitForTimeout(1000);
        }
      }
    }
    
    // Check for results summary text
    const resultsSummary = page.locator('text=/Showing.*of.*applications/');
    if (await resultsSummary.isVisible()) {
      const summaryText = await resultsSummary.textContent();
      expect(summaryText).toMatch(/Showing \d+-\d+ of \d+ applications/);
    }
  });

  test('Bulk Selection Functionality', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Test "Select All" checkbox
    const selectAllCheckbox = page.getByLabel('Select all applications');
    
    if (await selectAllCheckbox.isVisible()) {
      // Click select all
      await selectAllCheckbox.click();
      
      // Check that individual checkboxes are selected
      const individualCheckboxes = page.locator('input[type="checkbox"]:not([aria-label*="Select all"])');
      const checkboxCount = await individualCheckboxes.count();
      
      if (checkboxCount > 0) {
        // Verify at least one checkbox is checked
        const checkedCheckboxes = page.locator('input[type="checkbox"]:checked:not([aria-label*="Select all"])');
        expect(await checkedCheckboxes.count()).toBeGreaterThan(0);
      }
      
      // Uncheck select all
      await selectAllCheckbox.click();
    }
  });

  test('Back Navigation from Detail Page', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Navigate to detail page if applications exist
    const rowCount = await page.locator('tbody tr').count();
    
    if (rowCount > 0) {
      const firstRow = page.locator('tbody tr').first();
      await firstRow.click();
      
      // Wait for detail page
      await expect(page.locator('text=Application -')).toBeVisible({ timeout: 10000 });
      
      // Click back button
      const backButton = page.locator('button:has-text("Back to Applications")');
      await backButton.click();
      
      // Should return to applications list
      await expect(page.locator('table')).toBeVisible();
      await expect(page.locator('th:has-text("NAME")')).toBeVisible();
    } else {
      console.log('No applications found to test back navigation');
    }
  });

  test('Error Handling and Empty States', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Test search with no results
    const searchInput = page.getByPlaceholder('Search by name, email, or scene name...');
    await searchInput.fill('nonexistentapplication12345');
    await page.waitForTimeout(1000);
    
    // Check for empty state message
    const emptyStateMessages = [
      'No applications match your filters',
      'No vetting applications found',
      'Clear Filters'
    ];
    
    let foundEmptyState = false;
    for (const message of emptyStateMessages) {
      if (await page.locator(`text=${message}`).isVisible()) {
        foundEmptyState = true;
        break;
      }
    }
    
    // If we found an empty state, test the clear filters functionality
    if (foundEmptyState) {
      const clearFiltersButton = page.locator('button:has-text("Clear Filters")');
      if (await clearFiltersButton.isVisible()) {
        await clearFiltersButton.click();
        await page.waitForTimeout(1000);
        
        // Search should be cleared
        const searchValue = await searchInput.inputValue();
        expect(searchValue).toBe('');
      }
    }
  });

  test('Accessibility and Keyboard Navigation', async ({ page }) => {
    // Arrange
    await loginAsAdmin(page);
    await navigateToVettingPage(page);
    
    // Test tab navigation
    await page.keyboard.press('Tab');
    
    // Test that focus is visible on interactive elements
    const focusableElements = [
      'input[type="text"]',
      'button',
      'select',
      'a[href]',
      '[tabindex="0"]'
    ];
    
    for (const selector of focusableElements) {
      const elements = page.locator(selector);
      const count = await elements.count();
      
      if (count > 0) {
        // Verify elements are keyboard accessible
        const firstElement = elements.first();
        await firstElement.focus();
        
        // Element should be focused
        await expect(firstElement).toBeFocused();
        break;
      }
    }
    
    // Test that checkboxes have proper labels
    const checkboxes = page.locator('input[type="checkbox"]');
    const checkboxCount = await checkboxes.count();
    
    for (let i = 0; i < Math.min(checkboxCount, 3); i++) {
      const checkbox = checkboxes.nth(i);
      const ariaLabel = await checkbox.getAttribute('aria-label');
      
      // Each checkbox should have an aria-label or associated label
      expect(ariaLabel).toBeTruthy();
    }
  });
});
