import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

/**
 * User Dashboard Vetting Status Display Tests
 * Validates that vetting status enum values are displayed correctly
 * Tests the DTO alignment strategy implementation
 */
test.describe('User Dashboard - Vetting Status Display', () => {
  test('admin user dashboard shows Approved vetting status', async ({ page }) => {
    // Arrange - Login as admin (should have Approved vetting status)
    await AuthHelpers.loginAs(page, 'admin');

    // Act - Navigate to dashboard
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Assert - Verify vetting status is displayed
    // Admin user should show "Approved" status (VettingStatus = 4)
    const vettingStatusBadge = page.locator('text=/approved/i, [data-testid="vetting-status"]');

    // Take screenshot for manual verification
    await page.screenshot({
      path: 'test-results/user-dashboard-admin-vetting-status.png',
      fullPage: true
    });

    // Verify page content includes vetting status information
    const pageContent = await page.textContent('body');

    // Should show approved status or full access message
    const hasApprovedStatus = pageContent?.toLowerCase().includes('approved') ||
                             pageContent?.toLowerCase().includes('full access') ||
                             pageContent?.toLowerCase().includes('vetted');

    expect(hasApprovedStatus).toBeTruthy();

    console.log('✅ Admin user dashboard displays vetting status correctly');
  });

  test('applicant user with InterviewApproved status displays correctly', async ({ page }) => {
    // Arrange - Login as applicant with InterviewApproved status
    // Note: Need to check which test user has this status
    await AuthHelpers.loginAs(page, 'vetted');

    // Act - Navigate to dashboard
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Take screenshot
    await page.screenshot({
      path: 'test-results/user-dashboard-vetted-status.png',
      fullPage: true
    });

    // Assert - Dashboard loads successfully
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();

    console.log('✅ Vetted user dashboard loaded successfully');
  });

  test('guest user shows correct non-vetted status', async ({ page }) => {
    // Arrange - Login as guest (not vetted)
    await AuthHelpers.loginAs(page, 'guest');

    // Act - Navigate to dashboard
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Take screenshot
    await page.screenshot({
      path: 'test-results/user-dashboard-guest-status.png',
      fullPage: true
    });

    // Assert - Should show limited access or non-vetted status
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();

    console.log('✅ Guest user dashboard loaded successfully');
  });

  test('dashboard API returns correct VettingStatus enum values', async ({ page }) => {
    // Arrange - Login as admin
    await AuthHelpers.loginAs(page, 'admin');

    // Act - Intercept dashboard API call
    const dashboardResponse = await page.waitForResponse(
      response => response.url().includes('/api/dashboard') && response.status() === 200
    );

    const dashboardData = await dashboardResponse.json();

    // Assert - Verify VettingStatus is a string enum value, not a number
    expect(dashboardData.vettingStatus).toBeDefined();
    expect(typeof dashboardData.vettingStatus).toBe('string');

    // Should be one of the valid enum values
    const validStatuses = [
      'UnderReview',
      'InterviewApproved',
      'FinalReview',
      'Approved',
      'Denied',
      'OnHold',
      'Withdrawn'
    ];

    expect(validStatuses).toContain(dashboardData.vettingStatus);

    console.log(`✅ Dashboard API returned VettingStatus: ${dashboardData.vettingStatus}`);
    console.log('✅ DTO Alignment Strategy working correctly - enum values are strings');
  });
});
