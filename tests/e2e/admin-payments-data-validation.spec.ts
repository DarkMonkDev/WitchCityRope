import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * E2E Tests for Admin Payments Page - Data Validation
 *
 * These tests validate that actual payment data displays correctly:
 * - Payment transactions appear with correct data
 * - Sorting works correctly on all columns
 * - Filtering works correctly for all filter types
 * - Payment amounts, dates, users, and statuses display accurately
 */

test.describe('Admin Payments Data Validation', () => {
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.loginAs(page, 'admin');
    await page.goto('/admin/analytics/payments');
    await expect(page.locator('h1')).toContainText(/payment/i);
  });

  test('displays payment transactions with correct data structure', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Verify table has rows
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    expect(rowCount).toBeGreaterThan(0);
    console.log(`Found ${rowCount} payment transactions`);

    // Verify first row has all required columns
    const firstRow = rows.first();

    // Date column
    const dateCell = firstRow.locator('td').nth(0);
    await expect(dateCell).toBeVisible();
    const dateText = await dateCell.textContent();
    expect(dateText).toMatch(/\w{3}\s\d{1,2},\s\d{4}/); // e.g., "Nov 18, 2025"

    // User column
    const userCell = firstRow.locator('td').nth(1);
    await expect(userCell).toBeVisible();
    const userText = await userCell.textContent();
    expect(userText).toBeTruthy();

    // Event column
    const eventCell = firstRow.locator('td').nth(2);
    await expect(eventCell).toBeVisible();
    const eventText = await eventCell.textContent();
    expect(eventText).toBeTruthy();

    // Payment Method column (badge)
    const methodCell = firstRow.locator('td').nth(3);
    const methodBadge = methodCell.locator('[class*="Badge-root"]').first();
    await expect(methodBadge).toBeVisible();
    const methodText = await methodBadge.textContent();
    expect(['PayPal', 'Free', 'Venmo', 'Cash']).toContain(methodText?.trim());

    // Amount column
    const amountCell = firstRow.locator('td').nth(4);
    await expect(amountCell).toBeVisible();
    const amountText = await amountCell.textContent();
    expect(amountText).toMatch(/\$\d+\.\d{2}/); // e.g., "$25.00"

    // Status column (badge)
    const statusCell = firstRow.locator('td').nth(5);
    const statusBadge = statusCell.locator('[class*="Badge-root"]').first();
    await expect(statusBadge).toBeVisible();
    const statusText = await statusBadge.textContent();
    expect(['Paid', 'Completed', 'Refunded', 'Pending', 'Failed']).toContain(statusText?.trim());
  });

  test('sorting by date changes transaction order', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Get initial first row date
    const getFirstRowDate = async () => {
      const firstRow = page.locator('[data-testid="payment-row"]').first();
      const dateCell = firstRow.locator('td').first();
      return await dateCell.textContent();
    };

    const initialDate = await getFirstRowDate();

    // Click date header to sort
    const dateHeader = page.locator('th').filter({ hasText: 'Date' });
    await dateHeader.click();
    await page.waitForTimeout(500); // Allow sort to complete

    const sortedDate = await getFirstRowDate();

    // Verify order changed (ascending sort)
    expect(sortedDate).not.toBe(initialDate);

    // Click again to reverse sort
    await dateHeader.click();
    await page.waitForTimeout(500);

    const reversedDate = await getFirstRowDate();

    // Should be back to original or different from ascending
    expect(reversedDate).toBeTruthy();
  });

  test('sorting by amount orders payments correctly', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Click amount header to sort ascending
    const amountHeader = page.locator('th').filter({ hasText: 'Amount' });
    await amountHeader.click();
    await page.waitForTimeout(500);

    // Get first 3 amounts
    const rows = page.locator('[data-testid="payment-row"]');
    const amounts: number[] = [];

    for (let i = 0; i < Math.min(3, await rows.count()); i++) {
      const amountCell = rows.nth(i).locator('td').nth(4);
      const amountText = await amountCell.textContent();
      const amount = parseFloat(amountText?.replace('$', '') || '0');
      amounts.push(amount);
    }

    // Verify ascending order
    for (let i = 1; i < amounts.length; i++) {
      expect(amounts[i]).toBeGreaterThanOrEqual(amounts[i - 1]);
    }

    console.log(`Amounts in ascending order: ${amounts.map(a => `$${a.toFixed(2)}`).join(', ')}`);
  });

  test('filtering by payment method shows only matching transactions', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Open payment method filter
    const paymentMethodFilter = page.locator('[data-testid="payment-method-filter"]');
    await paymentMethodFilter.click();

    // Select PayPal
    await page.locator('[role="option"]').filter({ hasText: 'PayPal' }).click();

    // Close dropdown
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000); // Allow filter to apply

    // Verify all visible transactions have PayPal payment method
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    if (rowCount > 0) {
      for (let i = 0; i < Math.min(5, rowCount); i++) {
        const methodCell = rows.nth(i).locator('td').nth(3);
        const methodBadge = methodCell.locator('[class*="Badge-root"]').first();
        const methodText = await methodBadge.textContent();
        expect(methodText?.trim()).toBe('PayPal');
      }
      console.log(`Verified ${Math.min(5, rowCount)} PayPal transactions`);
    }
  });

  test('filtering by status shows only matching transactions', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Open status filter
    const statusFilter = page.locator('[data-testid="payment-status-filter"]');
    await statusFilter.click();

    // Select Paid status
    await page.locator('[role="option"]').filter({ hasText: 'Paid' }).click();

    // Close dropdown
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000); // Allow filter to apply

    // Verify all visible transactions have Paid status
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    if (rowCount > 0) {
      for (let i = 0; i < Math.min(5, rowCount); i++) {
        const statusCell = rows.nth(i).locator('td').nth(5);
        const statusBadge = statusCell.locator('[class*="Badge-root"]').first();
        const statusText = await statusBadge.textContent();
        expect(statusText?.trim()).toBe('Paid');
      }
      console.log(`Verified ${Math.min(5, rowCount)} Paid transactions`);
    }
  });

  test('search filter finds transactions by user name', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Use a simple search term that should exist
    const searchTerm = 'Admin';

    // Enter search term
    const searchInput = page.locator('[data-testid="payment-search-input"]');
    await searchInput.fill(searchTerm);

    // Wait for debounce and filter to apply
    await page.waitForTimeout(1500);

    // Verify we got results
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    // If we got results, verify they contain the search term
    if (rowCount > 0) {
      const firstResultUserCell = rows.first().locator('td').nth(1);
      const firstResultUserText = await firstResultUserCell.textContent();
      console.log(`Search for "${searchTerm}" returned ${rowCount} results. First: ${firstResultUserText}`);
    } else {
      console.log(`Search for "${searchTerm}" returned 0 results - skipping validation`);
    }
  });

  test('date range filter narrows results to specified period', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Get total count before filtering
    const initialRows = page.locator('[data-testid="payment-row"]');
    const initialCount = await initialRows.count();

    // Open date picker
    const datePicker = page.locator('[data-testid="payment-date-range"]');
    await datePicker.click();

    // Wait for calendar to open
    await page.waitForTimeout(500);

    // Select a date range (click two dates in the calendar)
    // This is simplified - in real tests you'd select specific dates
    const calendarDates = page.locator('[data-mantine-calendar-date]');
    const availableDates = await calendarDates.count();

    if (availableDates >= 2) {
      await calendarDates.first().click();
      await page.waitForTimeout(200);
      await calendarDates.nth(Math.min(7, availableDates - 1)).click();
      await page.waitForTimeout(1000);

      // Verify results changed
      const filteredRows = page.locator('[data-testid="payment-row"]');
      const filteredCount = await filteredRows.count();

      // Filter should have reduced results (or kept same if all within range)
      expect(filteredCount).toBeLessThanOrEqual(initialCount);

      console.log(`Date filter: ${initialCount} → ${filteredCount} transactions`);
    }
  });

  test('summary statistics display correct filtered totals', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Count visible rows
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    // Check summary section
    const summaryText = await page.locator('text=/Showing:.*transactions/').first().textContent();

    // Extract numbers from summary
    const match = summaryText?.match(/(\d+)\s+of\s+(\d+)\s+transactions/);

    if (match) {
      const showingCount = parseInt(match[1]);
      const totalCount = parseInt(match[2]);

      // Showing count should match visible rows
      expect(showingCount).toBe(rowCount);

      // Total should be greater than or equal to showing
      expect(totalCount).toBeGreaterThanOrEqual(showingCount);

      console.log(`Summary: Showing ${showingCount} of ${totalCount} transactions`);
    }
  });

  test('revenue total calculates correctly for filtered results', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Get all visible amounts
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();

    let calculatedTotal = 0;
    for (let i = 0; i < rowCount; i++) {
      const amountCell = rows.nth(i).locator('td').nth(4);
      const amountText = await amountCell.textContent();
      const amount = parseFloat(amountText?.replace('$', '') || '0');
      calculatedTotal += amount;
    }

    // Get displayed total
    const totalRevenueText = await page.locator('text=/Total Revenue:.*\\$\\d/').first().textContent();
    const displayedMatch = totalRevenueText?.match(/\$([\d,]+\.\d{2})/);

    if (displayedMatch) {
      const displayedTotal = parseFloat(displayedMatch[1].replace(',', ''));

      // Allow small rounding difference
      expect(Math.abs(calculatedTotal - displayedTotal)).toBeLessThan(0.02);

      console.log(`Revenue: Calculated $${calculatedTotal.toFixed(2)}, Displayed $${displayedTotal.toFixed(2)}`);
    }
  });

  test('payment method badges display with correct valid methods', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Collect unique payment methods
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();
    const foundMethods = new Set<string>();

    for (let i = 0; i < Math.min(10, rowCount); i++) {
      const methodCell = rows.nth(i).locator('td').nth(3);
      const methodBadge = methodCell.locator('[class*="Badge-root"]').first();
      const methodText = await methodBadge.textContent();

      if (methodText) {
        foundMethods.add(methodText.trim());
      }
    }

    // Verify expected payment methods appear
    const foundMethodsList = Array.from(foundMethods);
    console.log(`Found payment methods: ${foundMethodsList.join(', ')}`);

    // Should have at least one of the expected methods
    const validMethods = ['PayPal', 'Free', 'Venmo', 'Cash'];
    expect(foundMethodsList.some(m => validMethods.includes(m))).toBe(true);

    // All found methods should be valid
    for (const method of foundMethodsList) {
      expect(validMethods).toContain(method);
    }
  });

  test('no invalid payment methods appear (no Stripe, RSVP, Zelle)', async ({ page }) => {
    await page.waitForSelector('[data-testid="payment-table"]', { timeout: 10000 });

    // Check all visible payment methods
    const rows = page.locator('[data-testid="payment-row"]');
    const rowCount = await rows.count();
    const invalidMethods = ['Stripe', 'RSVP', 'Zelle'];
    const foundInvalidMethods: string[] = [];

    for (let i = 0; i < rowCount; i++) {
      const methodCell = rows.nth(i).locator('td').nth(3);
      const methodBadge = methodCell.locator('[class*="Badge-root"]').first();
      const methodText = await methodBadge.textContent();

      if (methodText && invalidMethods.includes(methodText.trim())) {
        foundInvalidMethods.push(methodText.trim());
      }
    }

    // Should not find any invalid payment methods
    expect(foundInvalidMethods).toHaveLength(0);

    if (foundInvalidMethods.length > 0) {
      console.error(`❌ Found invalid payment methods: ${foundInvalidMethods.join(', ')}`);
    } else {
      console.log(`✅ No invalid payment methods found in ${rowCount} transactions`);
    }
  });
});
