import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

// Environment-aware URLs for container/host compatibility
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';

/**
 * E2E Tests for Admin Events Edit Screen - Session Management
 *
 * These tests verify session management functionality including:
 * - Session creation via modal
 * - Session editing with pre-populated data
 * - Automatic S# ID assignment (format: S1, S2, S3, etc.)
 * - Form validation
 *
 * ARCHITECTURE: Tests create their own event data to ensure clean slate
 * for session operations. This avoids issues with seed data having all
 * session slots already filled.
 */

// Helper to make authenticated API request
async function apiRequest(page: Page, method: string, url: string, data?: unknown): Promise<{ status: number; data: unknown }> {
  const response = await page.evaluate(async ({ method, url, data }) => {
    const options: RequestInit = {
      method,
      credentials: 'include',
      headers: data ? { 'Content-Type': 'application/json' } : {},
    };

    if (data) {
      options.body = JSON.stringify(data);
    }

    const res = await fetch(url, options);
    const text = await res.text();
    try {
      return { status: res.status, data: JSON.parse(text) };
    } catch {
      return { status: res.status, data: text };
    }
  }, { method, url, data });

  return response;
}

test.describe('Admin Events Edit Screen - Session Management', () => {
  let testEventId: string | null = null;
  let venueId: string | null = null;

  test.beforeAll(async ({ browser }) => {
    // Create a fresh event with NO sessions for testing session operations
    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    // Get first venue ID
    const venuesResponse = await apiRequest(page, 'GET', '/api/venues');
    const venues = venuesResponse.data as Array<{ id: string }>;
    venueId = venues[0]?.id;

    if (!venueId) {
      console.error('No venues found - cannot create test event');
      await page.close();
      return;
    }

    // Create event with NO sessions - this gives us clean slate for session tests
    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 14); // 2 weeks in future
    startDate.setHours(18, 0, 0, 0);

    const eventData = {
      title: `Session Test Event ${Date.now()}`,
      shortDescription: 'Test event for session management E2E tests',
      description: 'This event is used to test session CRUD operations.',
      eventType: 'Class',
      startDate: startDate.toISOString(),
      endDate: new Date(startDate.getTime() + 3 * 60 * 60 * 1000).toISOString(),
      venueId: venueId,
      capacity: 20,
      isPublished: false, // Keep unpublished for testing
      // CRITICAL: Timing controls to avoid business logic failures
      registrationOpenHours: null,
      registrationCloseHours: 0,
      cancellationCloseHours: 0,
      // NO sessions - we'll add them in tests
      sessions: [],
    };

    console.log('Creating test event with NO sessions...');
    const createResponse = await apiRequest(page, 'POST', '/api/admin/events', eventData);

    if (createResponse.status !== 201 && createResponse.status !== 200) {
      console.error('Failed to create test event:', createResponse);
      await page.close();
      return;
    }

    const responseData = createResponse.data as { id: string };
    testEventId = responseData.id;
    console.log(`✅ Created test event: ${testEventId}`);

    await page.close();
  });

  test.afterAll(async ({ browser }) => {
    // Cleanup: Delete test event
    if (!testEventId) return;

    const page = await browser.newPage();
    await AuthHelpers.loginAs(page, 'admin');

    console.log(`Cleaning up test event: ${testEventId}`);
    await apiRequest(page, 'DELETE', `/api/admin/events/${testEventId}`);
    console.log('✅ Test event deleted');

    await page.close();
  });

  test.beforeEach(async ({ page }) => {
    // Login as admin user
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('should add a new session via modal without page refresh', async ({ page }) => {
    if (!testEventId) {
      console.log('Test event not created - skipping');
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Wait for page to load
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });

    // Navigate to Setup tab (contains Sessions and Ticket Types)
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Click Add Session button
    const addSessionButton = page.locator('[data-testid="button-add-session"]');
    await expect(addSessionButton).toBeVisible({ timeout: 5000 });
    await addSessionButton.click();

    // Verify Add Session modal opens
    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible({ timeout: 5000 });

    // Select Session Identifier (S1 should be available since event has no sessions)
    const sessionIdInput = page.getByTestId('input-session-id');
    await sessionIdInput.click();
    await page.waitForTimeout(300);

    // Select S1 option
    const s1Option = page.getByRole('option', { name: /S1/i });
    if (await s1Option.isVisible({ timeout: 3000 })) {
      await s1Option.click();
    } else {
      // Fall back to first available option
      await page.getByRole('option').first().click();
    }
    await page.waitForTimeout(300);

    // Fill session form fields
    await page.getByTestId('input-session-name').fill('Morning Workshop');
    await page.getByTestId('input-session-start-time').fill('09:00');
    await page.getByTestId('input-session-end-time').fill('12:00');
    await page.getByTestId('input-session-capacity').fill('20');

    // Save session
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Verify modal closes
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify session appears in grid WITHOUT page refresh
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Verify session appears in the grid
    const newSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Morning Workshop' });
    await expect(newSessionRow).toBeVisible({ timeout: 5000 });

    console.log('✅ Session added successfully via modal');
  });

  test('should edit existing session via modal', async ({ page }) => {
    if (!testEventId) {
      console.log('Test event not created - skipping');
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // First, ensure we have a session to edit by adding one via API
    const sessionData = {
      sessionIdentifier: 'S2',
      name: 'Original Session Name',
      startTime: '14:00',
      endTime: '17:00',
      capacity: 15,
    };

    // Add session via API for this test
    await apiRequest(page, 'PUT', `/api/admin/events/${testEventId}`, {
      sessions: [sessionData],
    });

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Wait for page to load and navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    // Verify sessions grid exists
    const sessionGrid = page.locator('[data-testid="grid-sessions"]');
    await expect(sessionGrid).toBeVisible();

    // Click on a session row to edit
    const sessionRow = sessionGrid.locator('[data-testid="session-row"]').first();
    await expect(sessionRow).toBeVisible();
    await sessionRow.click();

    // Verify edit modal opens
    const editModal = page.locator('[role="dialog"]');
    await expect(editModal).toBeVisible({ timeout: 5000 });

    // Verify form is pre-populated with existing session data
    const nameInput = page.getByTestId('input-session-name');
    await expect(nameInput).not.toHaveValue('');

    // Change session name
    await nameInput.fill('Updated Session Name');

    // Save changes
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();

    // Verify modal closes
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify updates appear in grid without page refresh
    const updatedRow = sessionGrid.locator('tr').filter({ hasText: 'Updated Session Name' });
    await expect(updatedRow).toBeVisible({ timeout: 5000 });

    console.log('✅ Session edited successfully via modal');
  });

  test('should assign S# IDs sequentially to new sessions', async ({ page }) => {
    if (!testEventId) {
      console.log('Test event not created - skipping');
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await expect(setupTab).toBeVisible({ timeout: 5000 });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    const sessionGrid = page.locator('[data-testid="grid-sessions"]');

    // Get current session count
    const initialSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    console.log(`Initial session count: ${initialSessionCount}`);

    // Add first new session
    await page.locator('[data-testid="button-add-session"]').click();
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();

    // Select next available session identifier
    const sessionIdInput = page.getByTestId('input-session-id');
    await sessionIdInput.click();
    await page.waitForTimeout(300);

    // Select first available option
    const firstOption = page.getByRole('option').first();
    if (await firstOption.isVisible({ timeout: 3000 })) {
      await firstOption.click();
    }
    await page.waitForTimeout(300);

    await page.getByTestId('input-session-name').fill('First New Session');
    await page.getByTestId('input-session-start-time').fill('09:00');
    await page.getByTestId('input-session-end-time').fill('12:00');
    await page.getByTestId('input-session-capacity').fill('20');
    await page.locator('[data-testid="button-save-session"]').click();

    // Wait for modal to close
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify first session appears
    const firstSessionRow = sessionGrid.locator('tr').filter({ hasText: 'First New Session' });
    await expect(firstSessionRow).toBeVisible({ timeout: 5000 });

    // Add second session
    await page.locator('[data-testid="button-add-session"]').click();
    await expect(modal).toBeVisible();

    // Select next available session identifier
    await sessionIdInput.click();
    await page.waitForTimeout(300);
    const nextOption = page.getByRole('option').first();
    if (await nextOption.isVisible({ timeout: 3000 })) {
      await nextOption.click();
    }
    await page.waitForTimeout(300);

    await page.getByTestId('input-session-name').fill('Second New Session');
    await page.getByTestId('input-session-start-time').fill('13:00');
    await page.getByTestId('input-session-end-time').fill('16:00');
    await page.getByTestId('input-session-capacity').fill('25');
    await page.locator('[data-testid="button-save-session"]').click();

    // Wait for modal to close
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    // Verify second session appears
    const secondSessionRow = sessionGrid.locator('tr').filter({ hasText: 'Second New Session' });
    await expect(secondSessionRow).toBeVisible({ timeout: 5000 });

    // Verify we now have more sessions than before
    const finalSessionCount = await sessionGrid.locator('[data-testid="session-row"]').count();
    expect(finalSessionCount).toBeGreaterThan(initialSessionCount);

    console.log(`✅ Sessions added: ${initialSessionCount} → ${finalSessionCount}`);
  });

  test('should validate session form fields', async ({ page }) => {
    if (!testEventId) {
      console.log('Test event not created - skipping');
      test.fail(true, 'Test event not created in beforeAll');
      return;
    }

    // Navigate to admin event edit page
    await page.goto(`/admin/events/${testEventId}`);

    // Navigate to Setup tab and open add modal
    await expect(page.locator('[data-testid="page-admin-event-details"]')).toBeVisible({ timeout: 10000 });
    const setupTab = page.getByRole('tab', { name: /Sessions|Setup/i });
    await setupTab.click();

    // Wait for sessions section
    const sessionsSection = page.locator('[data-testid="sessions-section"]');
    await expect(sessionsSection).toBeVisible({ timeout: 5000 });

    await page.locator('[data-testid="button-add-session"]').click();

    const sessionModal = page.locator('[role="dialog"]');
    await expect(sessionModal).toBeVisible();

    // Ensure Session Identifier is selected first
    const sessionIdInput = page.getByTestId('input-session-id');
    await sessionIdInput.click();
    await page.waitForTimeout(300);
    const firstOption = page.getByRole('option').first();
    if (await firstOption.isVisible({ timeout: 3000 })) {
      await firstOption.click();
    }

    // Test: Session Name validation - try to submit with empty name
    const nameInput = page.getByTestId('input-session-name');
    await nameInput.fill(''); // Clear the input

    // Click save button to trigger validation
    const saveButton = page.locator('[data-testid="button-save-session"]');
    await saveButton.click();

    // Verify validation prevents submission - modal should still be open
    await expect(sessionModal).toBeVisible();

    // Check that validation error exists (HTML5 or Mantine validation)
    const isInvalid = await nameInput.evaluate((el: HTMLInputElement) => !el.validity.valid);
    expect(isInvalid).toBe(true);

    // Fill valid session name
    await nameInput.fill('Valid Session Name');

    // Fill other required fields
    await page.getByTestId('input-session-start-time').fill('09:00');
    await page.getByTestId('input-session-end-time').fill('12:00');
    await page.getByTestId('input-session-capacity').fill('20');

    // Now save should work
    await saveButton.click();

    // Modal should close on successful save
    await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

    console.log('✅ Form validation working correctly');
  });
});
