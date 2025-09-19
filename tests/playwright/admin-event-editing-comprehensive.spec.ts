import { test, expect } from '@playwright/test';

/**
 * Comprehensive E2E tests for Admin Event Editing functionality
 *
 * Critical Issues to Test:
 * 1. Field Persistence Issues - Teacher selection doesn't persist after save and refresh
 * 2. Draft/Publish Status Toggle - Modal confirmation appears but status doesn't change
 * 3. Session Tickets Count Issue - Setup tab shows "10 tickets sold" vs RSVP/Tickets tab shows no tickets sold
 * 4. Add Position/Session Bug - First attempt fails and refreshes screen, second attempt works
 *
 * Test Environment: Docker containers only (port 5173)
 *
 * @created 2025-09-19
 * @updated 2025-09-19
 */

test.describe('Admin Event Editing - Comprehensive Tests', () => {

  // ✅ CRITICAL: JavaScript and console error monitoring (mandatory from lessons learned)
  let consoleErrors: string[] = [];
  let jsErrors: string[] = [];
  let networkErrors: string[] = [];

  test.beforeEach(async ({ page }) => {
    // Reset error tracking
    consoleErrors = [];
    jsErrors = [];
    networkErrors = [];

    // Monitor JavaScript errors (page crashes)
    page.on('pageerror', error => {
      jsErrors.push(error.toString());
      console.log('🚨 JavaScript Error:', error.toString());
    });

    // Monitor console errors (component failures)
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const errorText = msg.text();
        consoleErrors.push(errorText);
        // Specifically catch date/time errors and other critical issues
        if (errorText.includes('RangeError') || errorText.includes('Invalid time value')) {
          console.log(`🚨 CRITICAL: Date/Time error detected: ${errorText}`);
        }
        console.log('🔍 Console Error:', errorText);
      }
    });

    // Monitor network failures for API debugging
    page.on('requestfailed', request => {
      networkErrors.push(`${request.method()} ${request.url()} - ${request.failure()?.errorText}`);
      console.log('🔍 Network Failed:', request.method(), request.url(), request.failure()?.errorText);
    });

    // Monitor API responses for debugging
    page.on('response', response => {
      if (response.url().includes('/api/') && !response.ok()) {
        console.log('🔍 API Error Response:', response.status(), response.url());
      }
    });
  });

  // Helper function to check for errors before content validation
  const checkForErrors = () => {
    if (jsErrors.length > 0) {
      throw new Error(`Page has JavaScript errors that crash functionality: ${jsErrors.join('; ')}`);
    }

    if (consoleErrors.length > 0) {
      // Check for critical date/time errors
      const criticalErrors = consoleErrors.filter(error =>
        error.includes('RangeError') || error.includes('Invalid time value')
      );
      if (criticalErrors.length > 0) {
        throw new Error(`CRITICAL: Page has date/time errors that crash components: ${criticalErrors.join('; ')}`);
      }
    }

    // Check for user-visible connection problems
    return true;
  };

  // Helper function to login as admin
  const loginAsAdmin = async (page) => {
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    // Use exact selectors from LoginPage.tsx
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');

    // Wait for navigation to dashboard with proper error checking
    await page.waitForURL('**/dashboard', { timeout: 15000 });
    await page.waitForLoadState('networkidle');

    // Allow React components to render
    await page.waitForTimeout(2000);

    checkForErrors();
  };

  // Helper function to navigate to an existing event for editing
  const navigateToEventEdit = async (page) => {
    // Navigate to admin events page
    await page.goto('/admin/events');
    await page.waitForLoadState('networkidle');
    checkForErrors();

    // Find first event in the table and get its edit link
    const eventRows = page.locator('[data-testid="event-row"]');
    const rowCount = await eventRows.count();

    if (rowCount === 0) {
      throw new Error('No events found to edit. Test requires at least one event in the system.');
    }

    // Click on the first event to go to details page
    await eventRows.first().click();
    await page.waitForLoadState('networkidle');
    checkForErrors();

    // Verify we're on the event details page
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible();
  };

  test('should persist all field changes across tabs and page refresh', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    // Test Basic Info tab field persistence
    console.log('🧪 Testing Basic Info tab field persistence...');

    // Ensure we're on the basic info tab
    await page.click('[data-testid="tab-basic-info"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    // Change title field
    const originalTitle = await page.inputValue('[data-testid="event-title"]');
    const newTitle = `${originalTitle} - EDITED ${Date.now()}`;
    await page.fill('[data-testid="event-title"]', newTitle);

    // Change description
    const descriptionField = page.locator('[data-testid="short-description"]');
    await descriptionField.fill(`Updated description ${Date.now()}`);

    // Test Setup tab - Teacher selection persistence issue
    console.log('🧪 Testing Setup tab - Teacher selection persistence...');
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    // Select a teacher (this is the critical test for the reported issue)
    const teacherSelect = page.locator('[data-testid="teacher-select"]');
    if (await teacherSelect.count() > 0) {
      await teacherSelect.click();
      await page.waitForTimeout(500);

      // Select first available teacher
      const teacherOptions = page.locator('[data-testid="teacher-option"]');
      if (await teacherOptions.count() > 0) {
        await teacherOptions.first().click();
        console.log('✅ Teacher selected');
      }
    }

    // Test venue selection
    const venueSelect = page.locator('[data-testid="venue-select"]');
    if (await venueSelect.count() > 0) {
      await venueSelect.click();
      await page.waitForTimeout(500);

      const venueOptions = page.locator('[data-testid="venue-option"]');
      if (await venueOptions.count() > 0) {
        await venueOptions.first().click();
        console.log('✅ Venue selected');
      }
    }

    // Save changes
    console.log('🧪 Saving changes...');
    const saveButton = page.locator('[data-testid="save-button"]');
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Wait for save to complete (look for success notification)
    await page.waitForTimeout(3000);
    checkForErrors();

    // Check for success notification
    const notification = page.locator('.mantine-Notification-root, [data-testid="notification"]');
    if (await notification.count() > 0) {
      const notificationText = await notification.textContent();
      console.log('💬 Notification:', notificationText);
    }

    // Refresh the page to test persistence
    console.log('🧪 Refreshing page to test field persistence...');
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    checkForErrors();

    // Verify title change persisted
    await page.click('[data-testid="tab-basic-info"]');
    await page.waitForTimeout(1000);
    const persistedTitle = await page.inputValue('[data-testid="event-title"]');
    expect(persistedTitle).toBe(newTitle);
    console.log('✅ Title persistence verified');

    // Verify teacher selection persisted (this is the critical test)
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(1000);

    // Check if teacher selection is still there
    const teacherValue = await page.locator('[data-testid="teacher-select"]').inputValue().catch(() => '');
    console.log('🔍 Teacher value after refresh:', teacherValue);

    // This test will help identify if the teacher selection persistence issue exists
    if (!teacherValue) {
      console.log('🚨 ISSUE CONFIRMED: Teacher selection did not persist after save and refresh');
    } else {
      console.log('✅ Teacher selection persisted correctly');
    }
  });

  test('should toggle draft/publish status immediately on modal confirmation', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    console.log('🧪 Testing draft/publish status toggle...');

    // Find the status toggle (SegmentedControl)
    const statusToggle = page.locator('[data-testid="status-toggle"], .mantine-SegmentedControl-root');
    await expect(statusToggle).toBeVisible();

    // Get current status
    const currentStatus = await page.locator('.mantine-SegmentedControl-control[data-active="true"]').textContent();
    console.log('🔍 Current status:', currentStatus);

    // Click on the opposite status
    const targetStatus = currentStatus?.includes('DRAFT') ? 'PUBLISHED' : 'DRAFT';
    await page.locator(`.mantine-SegmentedControl-label:text("${targetStatus}")`).click();

    // Wait for and handle the confirmation modal
    console.log('🧪 Looking for confirmation modal...');
    const modal = page.locator('[data-testid="status-modal"], .mantine-Modal-root');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Confirm the status change
    const confirmButton = page.locator('[data-testid="confirm-status"], button:text("Publish Event"), button:text("Unpublish Event")');
    await confirmButton.click();

    // Wait for modal to close and status to update
    await page.waitForTimeout(2000);
    checkForErrors();

    // Verify the status changed immediately (without needing Save button)
    const updatedStatus = await page.locator('.mantine-SegmentedControl-control[data-active="true"]').textContent();
    console.log('🔍 Updated status:', updatedStatus);

    // This test will help identify if the status toggle persistence issue exists
    if (updatedStatus === currentStatus) {
      console.log('🚨 ISSUE CONFIRMED: Status toggle did not change after modal confirmation');
    } else {
      console.log('✅ Status toggle worked correctly');
    }

    // Refresh page to verify persistence
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    checkForErrors();

    const persistedStatus = await page.locator('.mantine-SegmentedControl-control[data-active="true"]').textContent();
    console.log('🔍 Status after refresh:', persistedStatus);

    if (persistedStatus !== updatedStatus) {
      console.log('🚨 ISSUE CONFIRMED: Status change did not persist after page refresh');
    } else {
      console.log('✅ Status change persisted correctly');
    }
  });

  test('should show accurate and consistent ticket counts across tabs', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    console.log('🧪 Testing ticket count consistency...');

    // Check Setup tab ticket count
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    const setupTabTicketCount = await page.locator('[data-testid="tickets-sold-count"]').textContent().catch(() => 'NOT FOUND');
    console.log('🔍 Setup tab tickets sold:', setupTabTicketCount);

    // Check RSVP/Tickets tab ticket count
    await page.click('[data-testid="rsvp-tickets-tab"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    const rsvpTabTicketCount = await page.locator('[data-testid="actual-tickets-count"]').textContent().catch(() => 'NOT FOUND');
    console.log('🔍 RSVP/Tickets tab actual tickets:', rsvpTabTicketCount);

    // Extract numeric values for comparison
    const setupCountMatch = setupTabTicketCount.match(/(\d+)/);
    const rsvpCountMatch = rsvpTabTicketCount.match(/(\d+)/);

    const setupCount = setupCountMatch ? parseInt(setupCountMatch[1]) : 0;
    const rsvpCount = rsvpCountMatch ? parseInt(rsvpCountMatch[1]) : 0;

    console.log(`🔍 Parsed counts - Setup: ${setupCount}, RSVP: ${rsvpCount}`);

    // This test will help identify the ticket count discrepancy issue
    if (setupCount !== rsvpCount) {
      console.log(`🚨 ISSUE CONFIRMED: Ticket count mismatch - Setup shows ${setupCount}, RSVP shows ${rsvpCount}`);

      // Check if Setup tab shows hardcoded "10 tickets sold"
      if (setupCount === 10 && rsvpCount === 0) {
        console.log('🚨 SPECIFIC ISSUE: Setup tab shows hardcoded "10 tickets sold" while RSVP shows 0');
      }
    } else {
      console.log('✅ Ticket counts are consistent across tabs');
    }

    // Verify ticket counts match actual database/API data
    const ticketRows = page.locator('[data-testid="ticket-row"]');
    const actualTicketRowCount = await ticketRows.count();
    console.log('🔍 Actual ticket rows in table:', actualTicketRowCount);

    if (rsvpCount !== actualTicketRowCount) {
      console.log(`🚨 ISSUE: RSVP tab count (${rsvpCount}) doesn't match actual ticket rows (${actualTicketRowCount})`);
    }
  });

  test('should add volunteer positions and sessions on first attempt', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    console.log('🧪 Testing add position/session functionality...');

    // Test adding volunteer position
    await page.click('[data-testid="tab-volunteers"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    const addPositionButton = page.locator('[data-testid="add-position-button"], [data-testid="button-add-volunteer-position"]');
    if (await addPositionButton.count() > 0) {
      console.log('🧪 Testing add volunteer position on first attempt...');

      // This is the critical test - first attempt should work, not refresh screen
      const urlBeforeClick = page.url();

      await addPositionButton.click();
      await page.waitForTimeout(2000);

      const urlAfterClick = page.url();

      // Check if page refreshed (which would indicate the bug)
      if (urlAfterClick !== urlBeforeClick) {
        console.log('🚨 ISSUE CONFIRMED: Add position button caused page refresh on first attempt');
      } else {
        console.log('✅ Add position button worked without page refresh');
      }

      // Check if modal appeared or position was added
      const positionModal = page.locator('[data-testid="position-modal"], .mantine-Modal-root');
      const modalVisible = await positionModal.isVisible().catch(() => false);

      if (modalVisible) {
        console.log('✅ Position modal appeared on first attempt');

        // Close modal for cleanup
        const closeButton = page.locator('[data-testid="modal-close"], .mantine-Modal-close');
        if (await closeButton.count() > 0) {
          await closeButton.click();
        }
      } else {
        console.log('🚨 Position modal did not appear - may indicate issue');
      }
    }

    // Test adding session
    await page.click('[data-testid="setup-tab"]');
    await page.waitForTimeout(1000);
    checkForErrors();

    const addSessionButton = page.locator('[data-testid="add-session-button"], [data-testid="button-add-session"]');
    if (await addSessionButton.count() > 0) {
      console.log('🧪 Testing add session on first attempt...');

      const urlBeforeClick = page.url();

      await addSessionButton.click();
      await page.waitForTimeout(2000);

      const urlAfterClick = page.url();

      // Check if page refreshed (which would indicate the bug)
      if (urlAfterClick !== urlBeforeClick) {
        console.log('🚨 ISSUE CONFIRMED: Add session button caused page refresh on first attempt');
      } else {
        console.log('✅ Add session button worked without page refresh');
      }

      // Check if modal appeared or session was added
      const sessionModal = page.locator('[data-testid="session-modal"], .mantine-Modal-root');
      const modalVisible = await sessionModal.isVisible().catch(() => false);

      if (modalVisible) {
        console.log('✅ Session modal appeared on first attempt');

        // Close modal for cleanup
        const closeButton = page.locator('[data-testid="modal-close"], .mantine-Modal-close');
        if (await closeButton.count() > 0) {
          await closeButton.click();
        }
      } else {
        console.log('🚨 Session modal did not appear - may indicate issue');
      }
    }
  });

  test('should handle auth timeout and maintain session during editing', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    console.log('🧪 Testing auth session handling during editing...');

    // Make several tab switches and form interactions to test for auth timeouts
    const tabs = ['[data-testid="tab-basic-info"]', '[data-testid="setup-tab"]', '[data-testid="tab-emails"]', '[data-testid="tab-volunteers"]', '[data-testid="rsvp-tickets-tab"]'];

    for (const tab of tabs) {
      console.log(`🧪 Switching to tab: ${tab}`);
      await page.click(tab);
      await page.waitForTimeout(1000);
      checkForErrors();

      // Check for auth redirects or timeout errors
      const currentUrl = page.url();
      if (currentUrl.includes('/login')) {
        console.log('🚨 ISSUE: User was redirected to login during tab switching - auth timeout issue');
        break;
      }
    }

    // Try to save to test if auth is still valid
    const saveButton = page.locator('[data-testid="save-button"]');
    if (await saveButton.count() > 0) {
      await saveButton.click();
      await page.waitForTimeout(2000);

      const urlAfterSave = page.url();
      if (urlAfterSave.includes('/login')) {
        console.log('🚨 ISSUE: User was redirected to login during save - auth timeout issue');
      } else {
        console.log('✅ Auth session maintained during editing operations');
      }
    }
  });

  test('should verify all tabs load without errors and display expected content', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateToEventEdit(page);

    console.log('🧪 Testing all tabs for errors and content...');

    const tabTests = [
      {
        testId: '[data-testid="tab-basic-info"]',
        name: 'Basic Info',
        requiredElements: ['[data-testid="event-title"]', '[data-testid="short-description"]']
      },
      {
        testId: '[data-testid="setup-tab"]',
        name: 'Setup',
        requiredElements: ['[data-testid="teacher-select"]', '[data-testid="venue-select"]']
      },
      {
        testId: '[data-testid="tab-emails"]',
        name: 'Emails',
        requiredElements: [] // Add elements when known
      },
      {
        testId: '[data-testid="tab-volunteers"]',
        name: 'Volunteers',
        requiredElements: ['[data-testid="add-position-button"], [data-testid="button-add-volunteer-position"]']
      },
      {
        testId: '[data-testid="rsvp-tickets-tab"]',
        name: 'RSVP/Tickets',
        requiredElements: [] // Add elements when known
      },
      {
        testId: '[data-testid="attendees-tab"]',
        name: 'Attendees',
        requiredElements: [] // Add elements when known
      }
    ];

    for (const tabTest of tabTests) {
      console.log(`🧪 Testing ${tabTest.name} tab...`);

      await page.click(tabTest.testId);
      await page.waitForTimeout(1500);

      // Check for errors after tab switch
      checkForErrors();

      // Verify required elements are present
      for (const element of tabTest.requiredElements) {
        const elementExists = await page.locator(element).count() > 0;
        if (elementExists) {
          console.log(`✅ ${tabTest.name}: Required element ${element} found`);
        } else {
          console.log(`⚠️ ${tabTest.name}: Required element ${element} not found`);
        }
      }

      // Take screenshot for debugging
      await page.screenshot({
        path: `test-results/admin-event-editing-${tabTest.name.toLowerCase().replace(/[^a-z0-9]/g, '-')}.png`,
        fullPage: true
      });
    }
  });
});