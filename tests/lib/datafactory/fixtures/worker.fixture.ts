/**
 * Worker-Scoped DataFactory Fixture
 *
 * Provides shared data that persists across all tests in a worker.
 * Use this fixture when tests in a worker share data (e.g., a common event).
 *
 * IMPORTANT: Worker-scoped fixtures create data once per worker and clean up
 * when the worker is done. Test-scoped fixtures (df) still clean up per-test.
 *
 * @example
 * import { test, expect } from '@tests/lib/datafactory/fixtures/worker.fixture';
 *
 * test('test using shared event', async ({ sharedEventId, df }) => {
 *   // sharedEventId is the same across all tests in this worker
 *   // df is per-test for additional test-specific data
 *   console.log('Using shared event:', sharedEventId);
 * });
 */

import { test as base, request as playwrightRequest } from '@playwright/test';
import { DataFactory } from '../index';
import { createCompleteEvent, type CompleteEventData } from '../scenarios';

/**
 * Worker-scoped fixtures type
 */
type WorkerFixtures = {
  /** Shared event data - same event for all tests in worker */
  sharedEventData: CompleteEventData;
};

/**
 * Test-scoped fixtures type
 */
type TestFixtures = {
  /** Test-scoped DataFactory - automatically cleaned up after each test */
  df: DataFactory;
  /** Convenient access to shared event ID */
  sharedEventId: string;
};

/**
 * Extended test with both worker and test scoped fixtures
 */
export const test = base.extend<TestFixtures, WorkerFixtures>({
  // Worker-scoped shared event (created once per worker)
  sharedEventData: [
    async ({}, use) => {
      // Create a new request context for worker-scoped setup
      const requestContext = await playwrightRequest.newContext({
        baseURL: process.env.API_URL || 'http://localhost:5655',
      });

      try {
        // Create a shared event for this worker
        const eventData = await createCompleteEvent(requestContext, {
          title: `Worker Shared Event ${Date.now()}`,
          sessionCount: 2,
          ticketPrice: 25,
          ticketsPerSession: 100,
        });

        await use(eventData);

        // Cleanup shared event when worker is done
        const df = new DataFactory(requestContext);
        try {
          await df.events.delete(eventData.event.id);
        } catch (error) {
          console.warn('Failed to cleanup shared event:', error);
        }
      } finally {
        await requestContext.dispose();
      }
    },
    { scope: 'worker' },
  ],

  // Test-scoped access to shared event ID
  sharedEventId: async ({ sharedEventData }, use) => {
    await use(sharedEventData.event.id);
  },

  // Test-scoped DataFactory (cleaned up after each test)
  df: async ({ request }, use) => {
    const df = new DataFactory(request);
    await use(df);
    await df.cleanupAll();
  },
});

/**
 * Re-export expect for convenience
 */
export { expect } from '@playwright/test';
