/**
 * Test-Scoped DataFactory Fixture
 *
 * Provides a DataFactory instance that automatically cleans up after each test.
 * Use this fixture when tests need isolated data that doesn't persist between tests.
 *
 * @example
 * import { test } from '@tests/lib/datafactory/fixtures/test.fixture';
 *
 * test('my test', async ({ df }) => {
 *   const user = await df.users.createVerified({ email: 'test@example.com' });
 *   const event = await df.events.createDefault();
 *   // Data is automatically cleaned up after test
 * });
 */

import { test as base } from '@playwright/test';
import { DataFactory } from '../index';

/**
 * Extended test type with DataFactory fixture
 */
export const test = base.extend<{
  /** DataFactory instance - automatically cleaned up after each test */
  df: DataFactory;
}>({
  df: async ({ request }, use) => {
    // Create DataFactory instance
    const df = new DataFactory(request);

    // Verify test helpers are available
    const isHealthy = await df.healthCheck();
    if (!isHealthy) {
      console.warn(
        'Warning: Test helper endpoints may not be available. ' +
          'Ensure API is running in Development or Test environment.'
      );
    }

    // Provide the fixture to the test
    await use(df);

    // Cleanup all created data after test
    await df.cleanupAll();
  },
});

/**
 * Re-export expect for convenience
 */
export { expect } from '@playwright/test';

/**
 * Test type for use in other files
 */
export type TestFixture = typeof test;
