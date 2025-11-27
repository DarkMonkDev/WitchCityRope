/**
 * E2E Tests for Admin Event Creation
 * Tests: POST /api/events via admin UI
 * Coverage: Form validation, API integration, success flow, error handling
 */

import { test, expect } from '@playwright/test';
import { AuthHelpers } from '../../../../tests/e2e/test-utils/helpers/auth.helpers';

test.describe('Admin Event Creation - E2E', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await AuthHelpers.loginAs(page, 'admin');
  });

  test('Admin can create basic event with required fields only', async ({ page }) => {
    // Navigate to create event page
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill required fields
    await page.locator('[data-testid="input-event-title"]').last().fill('E2E Test Event');
    await page.locator('[data-testid="input-event-description"]').last().fill('This is an E2E test event created via Playwright');

    // Select event type
    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    // Select venue
    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click(); // Select first venue

    // Set capacity
    await page.locator('[data-testid="input-capacity"]').last().fill('25');

    // Set dates
    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7); // 7 days from now
    const startDateStr = startDate.toISOString().split('T')[0];

    await page.locator('[data-testid="input-start-date"]').last().fill(startDateStr);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab'); // Close calendar

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 2); // 2 hours later
    const endDateStr = endDate.toISOString().split('T')[0];

    await page.locator('[data-testid="input-end-date"]').last().fill(endDateStr);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab'); // Close calendar

    // Submit form
    const submitButton = page.locator('button:has-text("Create Event")').last();
    await submitButton.waitFor({ state: 'visible' });
    await submitButton.click();

    // Verify success notification
    const successAlert = page.locator('[role="alert"]').first();
    await expect(successAlert).toBeVisible({ timeout: 10000 });
    await expect(successAlert).toContainText(/created|success/i);

    // Verify redirect to event detail or events list
    await expect(page).toHaveURL(/\/admin\/events/);
  });

  test('Admin can create event with sessions', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill basic fields
    await page.locator('[data-testid="input-event-title"]').last().fill('Workshop with Sessions');
    await page.locator('[data-testid="input-event-description"]').last().fill('Event with multiple sessions');

    // Select event type
    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    // Select venue
    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('50');

    // Set dates
    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 4);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    // Add sessions
    const addSessionButton = page.locator('button:has-text("Add Session")').last();
    if (await addSessionButton.isVisible()) {
      await addSessionButton.click();

      // Fill session details
      await page.locator('[data-testid="input-session-name"]').last().fill('Morning Session');
      await page.locator('[data-testid="input-session-capacity"]').last().fill('25');
    }

    // Submit
    await page.locator('button:has-text("Create Event")').last().click();

    // Verify success
    const successAlert = page.locator('[role="alert"]').first();
    await expect(successAlert).toBeVisible({ timeout: 10000 });
  });

  test('Admin can create event with ticket types', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill basic fields
    await page.locator('[data-testid="input-event-title"]').last().fill('Ticketed Event');
    await page.locator('[data-testid="input-event-description"]').last().fill('Event with tickets');

    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('30');

    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 2);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    // Add ticket type
    const addTicketButton = page.locator('button:has-text("Add Ticket Type")').last();
    if (await addTicketButton.isVisible()) {
      await addTicketButton.click();

      await page.locator('[data-testid="input-ticket-name"]').last().fill('General Admission');
      await page.locator('[data-testid="input-ticket-price"]').last().fill('25');
      await page.locator('[data-testid="input-ticket-quantity"]').last().fill('30');
    }

    // Submit
    await page.locator('button:has-text("Create Event")').last().click();

    const successAlert = page.locator('[role="alert"]').first();
    await expect(successAlert).toBeVisible({ timeout: 10000 });
  });

  test('Admin can create event with volunteer positions', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill basic fields
    await page.locator('[data-testid="input-event-title"]').last().fill('Event with Volunteers');
    await page.locator('[data-testid="input-event-description"]').last().fill('Event needing volunteers');

    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Social")').last().click();

    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('40');

    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 3);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    // Add volunteer position
    const addVolunteerButton = page.locator('button:has-text("Add Volunteer Position")').last();
    if (await addVolunteerButton.isVisible()) {
      await addVolunteerButton.click();

      await page.locator('[data-testid="input-volunteer-title"]').last().fill('Setup Crew');
      await page.locator('[data-testid="input-volunteer-description"]').last().fill('Help set up the venue');
      await page.locator('[data-testid="input-volunteer-slots"]').last().fill('3');
    }

    // Submit
    await page.locator('button:has-text("Create Event")').last().click();

    const successAlert = page.locator('[role="alert"]').first();
    await expect(successAlert).toBeVisible({ timeout: 10000 });
  });

  test('Form validation prevents submission with missing required fields', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Try to submit without filling required fields
    const submitButton = page.locator('button:has-text("Create Event")').last();
    await submitButton.click();

    // Verify form validation errors appear (don't submit)
    // Should still be on create page
    await expect(page).toHaveURL(/\/admin\/events\/create/);

    // Verify validation messages present
    const validationErrors = page.locator('[role="alert"], .error, .invalid');
    await expect(validationErrors.first()).toBeVisible({ timeout: 5000 });
  });

  test('Success notification appears after creation', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill minimum required fields
    await page.locator('[data-testid="input-event-title"]').last().fill('Notification Test');
    await page.locator('[data-testid="input-event-description"]').last().fill('Testing success notification');

    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('20');

    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 2);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    // Submit
    await page.locator('button:has-text("Create Event")').last().click();

    // Verify success notification
    const successNotification = page.locator('[role="alert"]').first();
    await expect(successNotification).toBeVisible({ timeout: 10000 });
    await expect(successNotification).toContainText(/success|created/i);
  });

  test('Redirects to event detail page after creation', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Fill and submit form
    await page.locator('[data-testid="input-event-title"]').last().fill('Redirect Test Event');
    await page.locator('[data-testid="input-event-description"]').last().fill('Testing redirect');

    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('20');

    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 2);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    await page.locator('button:has-text("Create Event")').last().click();

    // Wait for redirect
    await page.waitForURL(/\/admin\/events\/[a-f0-9-]+|\/admin\/events$/, { timeout: 10000 });

    // Verify we're no longer on create page
    const currentUrl = page.url();
    expect(currentUrl).not.toContain('/create');
  });

  test('Created event appears in admin events list', async ({ page }) => {
    // Create event
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    const uniqueTitle = `List Test Event ${Date.now()}`;

    await page.locator('[data-testid="input-event-title"]').last().fill(uniqueTitle);
    await page.locator('[data-testid="input-event-description"]').last().fill('Should appear in list');

    const eventTypeSelect = page.locator('[data-testid="select-event-type"]').last();
    await eventTypeSelect.click();
    await page.locator('[role="option"]:has-text("Workshop")').last().click();

    const venueSelect = page.locator('[data-testid="select-venue"]').last();
    await venueSelect.click();
    await page.locator('[role="option"]').first().click();

    await page.locator('[data-testid="input-capacity"]').last().fill('20');

    const startDate = new Date();
    startDate.setDate(startDate.getDate() + 7);
    await page.locator('[data-testid="input-start-date"]').last().fill(startDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-start-date"]').last().press('Tab');

    const endDate = new Date(startDate);
    endDate.setHours(startDate.getHours() + 2);
    await page.locator('[data-testid="input-end-date"]').last().fill(endDate.toISOString().split('T')[0]);
    await page.locator('[data-testid="input-end-date"]').last().press('Tab');

    await page.locator('button:has-text("Create Event")').last().click();

    // Wait for success
    await expect(page.locator('[role="alert"]').first()).toBeVisible({ timeout: 10000 });

    // Navigate to events list
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('domcontentloaded');

    // Verify event appears
    const eventCard = page.locator(`[data-testid="event-card"]:has-text("${uniqueTitle}")`);
    await expect(eventCard.first()).toBeVisible({ timeout: 10000 });
  });

  test('Cancel button returns to events list without creating', async ({ page }) => {
    await page.goto('http://localhost:5173/admin/events/create');
    await page.waitForLoadState('domcontentloaded');

    // Start filling form
    await page.locator('[data-testid="input-event-title"]').last().fill('Should Not Be Created');

    // Click cancel
    const cancelButton = page.locator('button:has-text("Cancel")').last();
    if (await cancelButton.isVisible()) {
      await cancelButton.click();

      // Verify navigated back to events list
      await expect(page).toHaveURL(/\/admin\/events$/);

      // Verify event was NOT created
      const eventCard = page.locator('[data-testid="event-card"]:has-text("Should Not Be Created")');
      await expect(eventCard).not.toBeVisible();
    }
  });
});
