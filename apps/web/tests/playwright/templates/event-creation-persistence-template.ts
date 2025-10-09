/**
 * Event Creation Persistence Test Template
 *
 * Ensures admin event creation persists correctly to database.
 * Verifies all event fields are saved and can be retrieved.
 */

import { Page, expect } from '@playwright/test';
import {
  PersistenceTestTemplate,
  TestConfig,
  PersistenceTestOptions,
  verifySuccessMessage,
  verifyNoErrorMessage,
  verifyApiSuccess,
} from './persistence-test-template';
import { DatabaseHelpers, EventRecord } from '../utils/database-helpers';

// ============================================================================
// EVENT CREATION TEST CONFIG
// ============================================================================

export interface EventCreationTestConfig {
  /** Admin user credentials for login */
  adminEmail: string;
  adminPassword: string;
  /** Event details to create */
  eventData: {
    title: string;
    description: string;
    eventType: 'Workshop' | 'Performance' | 'Social' | 'Class';
    location: string;
    startDate: Date;
    endDate: Date;
    capacity: number;
    isPublished?: boolean;
  };
  /** Optional: Expected success message */
  successMessage?: string;
  /** Optional: Screenshot path */
  screenshotPath?: string;
}

/**
 * Test admin event creation persists to database
 */
export async function testEventCreationPersistence(
  page: Page,
  testConfig: EventCreationTestConfig
): Promise<string> {
  const { adminEmail, adminPassword, eventData, successMessage, screenshotPath } = testConfig;

  let createdEventId: string | null = null;
  let userId: string;

  const config: TestConfig = {
    description: `Event creation persistence for ${eventData.title}`,

    // Setup: Login as admin and navigate to create event page
    setup: async (page: Page) => {
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(adminEmail);
      await page.locator('[data-testid="password-input"]').fill(adminPassword);
      await page.locator('[data-testid="login-button"]').click();

      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      userId = await DatabaseHelpers.getUserIdFromEmail(adminEmail);
      console.log(`✅ Logged in as admin ${adminEmail} (ID: ${userId})`);

      // Navigate to create event page
      // Try multiple possible routes
      const createEventLink = page.locator(
        'a[href*="events/create"], a:has-text("Create Event"), [data-testid="create-event-link"]'
      ).first();

      if (await createEventLink.count() > 0) {
        await createEventLink.click();
        await page.waitForLoadState('networkidle');
      } else {
        // Fallback: Navigate directly
        await page.goto('http://localhost:5173/admin/events/create');
        await page.waitForLoadState('networkidle');
      }

      console.log('✅ Navigated to create event page');
    },

    // Action: Fill event form and submit
    action: async (page: Page) => {
      // Fill event details
      await page.locator('[data-testid="title-input"], input[name="title"]').fill(eventData.title);
      console.log(`✅ Filled title: ${eventData.title}`);

      await page.locator('[data-testid="description-input"], textarea[name="description"]').fill(
        eventData.description
      );
      console.log(`✅ Filled description`);

      // Select event type
      const eventTypeSelect = page.locator('[data-testid="event-type-select"], select[name="eventType"]');
      await eventTypeSelect.selectOption(eventData.eventType);
      console.log(`✅ Selected event type: ${eventData.eventType}`);

      // Fill location
      await page.locator('[data-testid="location-input"], input[name="location"]').fill(
        eventData.location
      );
      console.log(`✅ Filled location: ${eventData.location}`);

      // Fill start date
      const startDateInput = page.locator('[data-testid="start-date-input"], input[name="startDate"]');
      const startDateValue = eventData.startDate.toISOString().slice(0, 16); // YYYY-MM-DDTHH:MM
      await startDateInput.fill(startDateValue);
      console.log(`✅ Filled start date: ${startDateValue}`);

      // Fill end date
      const endDateInput = page.locator('[data-testid="end-date-input"], input[name="endDate"]');
      const endDateValue = eventData.endDate.toISOString().slice(0, 16);
      await endDateInput.fill(endDateValue);
      console.log(`✅ Filled end date: ${endDateValue}`);

      // Fill capacity
      await page.locator('[data-testid="capacity-input"], input[name="capacity"]').fill(
        eventData.capacity.toString()
      );
      console.log(`✅ Filled capacity: ${eventData.capacity}`);

      // Set published status
      if (eventData.isPublished !== undefined) {
        const publishCheckbox = page.locator(
          '[data-testid="is-published-checkbox"], input[name="isPublished"]'
        );

        if (await publishCheckbox.count() > 0) {
          const isChecked = await publishCheckbox.isChecked();
          if (eventData.isPublished !== isChecked) {
            await publishCheckbox.click();
          }
          console.log(`✅ Set published: ${eventData.isPublished}`);
        }
      }

      // Submit form
      const submitButton = page.locator(
        '[data-testid="create-event-button"], button[type="submit"], button:has-text("Create Event")'
      ).first();

      await expect(submitButton).toBeVisible({ timeout: 5000 });
      await submitButton.click();
      console.log('✅ Clicked Create Event button');
    },

    // Verify UI success
    verifyUiSuccess: async (page: Page) => {
      await verifySuccessMessage(page, successMessage);
      await verifyNoErrorMessage(page);

      // Should redirect to event details or events list
      await page.waitForURL(/\/events\/|\/admin\/events/, { timeout: 10000 });
      console.log('✅ Redirected after event creation');

      // Try to extract event ID from URL
      const url = page.url();
      const eventIdMatch = url.match(/\/events\/([a-f0-9-]{36})/i);
      if (eventIdMatch) {
        createdEventId = eventIdMatch[1];
        console.log(`✅ Extracted event ID from URL: ${createdEventId}`);
      }
    },

    // Verify API response
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);

      // Try to extract event ID from response
      const responseData = await response.json();
      if (responseData && responseData.id) {
        createdEventId = responseData.id;
        console.log(`✅ Extracted event ID from API response: ${createdEventId}`);
      } else if (responseData && responseData.data && responseData.data.id) {
        createdEventId = responseData.data.id;
        console.log(`✅ Extracted event ID from API response: ${createdEventId}`);
      }
    },

    // Verify database (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      if (!createdEventId) {
        // Try to find event by title and creator
        const createdEvents = await DatabaseHelpers.getEventsByCreator(userId);
        const matchingEvent = createdEvents.find(e => e.title === eventData.title);

        if (!matchingEvent) {
          throw new Error('Event not found in database');
        }

        createdEventId = matchingEvent.id;
        console.log(`✅ Found event in database: ${createdEventId}`);
      }

      // Verify event exists with correct data
      const dbEvent = await DatabaseHelpers.verifyEventExists(createdEventId);

      // Verify critical fields match
      if (dbEvent.title !== eventData.title) {
        throw new Error(`Title mismatch: expected "${eventData.title}" but got "${dbEvent.title}"`);
      }

      if (dbEvent.eventType !== eventData.eventType) {
        throw new Error(
          `Event type mismatch: expected "${eventData.eventType}" but got "${dbEvent.eventType}"`
        );
      }

      if (dbEvent.capacity !== eventData.capacity) {
        throw new Error(
          `Capacity mismatch: expected ${eventData.capacity} but got ${dbEvent.capacity}`
        );
      }

      console.log('✅ Database contains event with correct data');
      console.log(`   ID: ${dbEvent.id}`);
      console.log(`   Title: ${dbEvent.title}`);
      console.log(`   Type: ${dbEvent.eventType}`);
      console.log(`   Capacity: ${dbEvent.capacity}`);
    },

    // Verify persistence after refresh (CRITICAL)
    verifyPersistence: async (page: Page) => {
      if (!createdEventId) {
        throw new Error('Cannot verify persistence: event ID not found');
      }

      // Navigate to event details page
      await page.goto(`http://localhost:5173/events/${createdEventId}`);
      await page.waitForLoadState('networkidle');

      // Verify event title is displayed
      const heading = page.locator('h1, h2, [data-testid="event-title"]');
      await expect(heading).toContainText(eventData.title, { timeout: 5000 });
      console.log('✅ Event title displayed on page');

      // Verify other key fields are visible
      await expect(page.locator('text=' + eventData.eventType)).toBeVisible();
      console.log('✅ Event type displayed on page');

      await expect(page.locator('text=' + eventData.location)).toBeVisible();
      console.log('✅ Event location displayed on page');

      console.log('✅ Event persisted and displays correctly after navigation');
    },

    // Cleanup: Delete test event
    cleanup: async (page: Page) => {
      if (createdEventId) {
        console.log(`⚠️  Test event created with ID: ${createdEventId}`);
        console.log('   Consider deleting this test event manually if needed');
        // In production, we might delete via API:
        // await DatabaseHelpers.cleanupTestData('Events', [createdEventId]);
      }
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: screenshotPath || '/tmp/event-creation-test',
    apiEndpoint: /\/api\/events$/i,
    apiMethod: 'POST',
    waitAfterAction: 2000, // Event creation might take longer
  };

  await PersistenceTestTemplate.runTest(page, config, options);

  if (!createdEventId) {
    throw new Error('Failed to retrieve created event ID');
  }

  return createdEventId;
}

/**
 * Test event update persists to database
 */
export async function testEventUpdatePersistence(
  page: Page,
  adminEmail: string,
  adminPassword: string,
  eventId: string,
  updatedFields: Partial<EventCreationTestConfig['eventData']>
): Promise<void> {
  let userId: string;

  const config: TestConfig = {
    description: `Event update persistence for ${eventId}`,

    // Setup: Login and navigate to event edit page
    setup: async (page: Page) => {
      await page.goto('http://localhost:5173/login');
      await page.waitForLoadState('networkidle');

      await page.locator('[data-testid="email-input"]').fill(adminEmail);
      await page.locator('[data-testid="password-input"]').fill(adminPassword);
      await page.locator('[data-testid="login-button"]').click();

      await page.waitForURL('**/dashboard', { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      userId = await DatabaseHelpers.getUserIdFromEmail(adminEmail);

      // Navigate to event edit page
      await page.goto(`http://localhost:5173/admin/events/${eventId}/edit`);
      await page.waitForLoadState('networkidle');
      console.log('✅ Navigated to event edit page');
    },

    // Action: Update fields and save
    action: async (page: Page) => {
      // Update title if provided
      if (updatedFields.title) {
        const titleInput = page.locator('[data-testid="title-input"], input[name="title"]');
        await titleInput.clear();
        await titleInput.fill(updatedFields.title);
        console.log(`✅ Updated title: ${updatedFields.title}`);
      }

      // Update description if provided
      if (updatedFields.description) {
        const descInput = page.locator('[data-testid="description-input"], textarea[name="description"]');
        await descInput.clear();
        await descInput.fill(updatedFields.description);
        console.log(`✅ Updated description`);
      }

      // Update capacity if provided
      if (updatedFields.capacity) {
        const capacityInput = page.locator('[data-testid="capacity-input"], input[name="capacity"]');
        await capacityInput.clear();
        await capacityInput.fill(updatedFields.capacity.toString());
        console.log(`✅ Updated capacity: ${updatedFields.capacity}`);
      }

      // Submit form
      const submitButton = page.locator(
        '[data-testid="save-event-button"], button[type="submit"], button:has-text("Save")'
      ).first();

      await submitButton.click();
      console.log('✅ Clicked Save button');
    },

    // Verify UI success
    verifyUiSuccess: async (page: Page) => {
      await verifySuccessMessage(page);
      await verifyNoErrorMessage(page);
    },

    // Verify API response
    verifyApiResponse: async (response) => {
      await verifyApiSuccess(response);
    },

    // Verify database (CRITICAL)
    verifyDatabaseState: async (page: Page) => {
      const dbEvent = await DatabaseHelpers.verifyEventExists(eventId);

      // Verify updated fields
      if (updatedFields.title && dbEvent.title !== updatedFields.title) {
        throw new Error(`Title not updated in database`);
      }

      if (updatedFields.capacity && dbEvent.capacity !== updatedFields.capacity) {
        throw new Error(`Capacity not updated in database`);
      }

      console.log('✅ Database shows updated values');
    },

    // Verify persistence
    verifyPersistence: async (page: Page) => {
      await page.goto(`http://localhost:5173/events/${eventId}`);
      await page.waitForLoadState('networkidle');

      if (updatedFields.title) {
        await expect(page.locator('h1, h2')).toContainText(updatedFields.title);
        console.log('✅ Updated title persisted');
      }
    },
  };

  const options: PersistenceTestOptions = {
    screenshotPath: '/tmp/event-update-test',
    apiEndpoint: new RegExp(`/api/events/${eventId}`),
    apiMethod: 'PUT',
    waitAfterAction: 1500,
  };

  await PersistenceTestTemplate.runTest(page, config, options);
}

// Export all functions
export default {
  testEventCreationPersistence,
  testEventUpdatePersistence,
};
